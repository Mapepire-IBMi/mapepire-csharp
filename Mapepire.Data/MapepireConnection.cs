using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Mapepire.Data.Internal;

namespace Mapepire.Data;

/// <summary>
/// An ADO.NET connection to Db2 for IBM i through the Mapepire server.
/// </summary>
/// <remarks>
/// Server connections (a websocket plus a server job) are pooled per connection string unless
/// <c>Pooling=false</c>. Only one request is in flight per connection; concurrent use of the same
/// connection from multiple threads is serialized.
/// </remarks>
public sealed class MapepireConnection : DbConnection
{
    private string _connectionString = string.Empty;
    private MapepireConnectionStringBuilder _settings = new();
    private ServerSession? _session;
    private ConnectionState _state = ConnectionState.Closed;
    private string? _currentSchema;

    /// <summary>Creates a connection with no connection string.</summary>
    public MapepireConnection()
    {
    }

    /// <summary>Creates a connection.</summary>
    /// <param name="connectionString">The connection string; see <see cref="MapepireConnectionStringBuilder"/>.</param>
    public MapepireConnection(string? connectionString)
    {
        ConnectionString = connectionString;
    }

    /// <inheritdoc />
    [AllowNull]
    public override string ConnectionString
    {
        get => _connectionString;
        set
        {
            if (_state != ConnectionState.Closed)
            {
                throw new InvalidOperationException("The connection string cannot be changed while the connection is open.");
            }

            _connectionString = value ?? string.Empty;
            _settings = new MapepireConnectionStringBuilder(_connectionString);
        }
    }

    /// <summary>A snapshot of the parsed connection settings. To configure the connection, set <see cref="ConnectionString"/>.</summary>
    public MapepireConnectionStringBuilder Settings => new(_settings.ConnectionString);

    /// <summary>The default schema, as given by <c>Default Schema</c> in the connection string.</summary>
    public override string Database => _currentSchema ?? _settings.DefaultSchema ?? string.Empty;

    /// <inheritdoc />
    public override string DataSource => _settings.Server ?? string.Empty;

    /// <inheritdoc />
    public override string ServerVersion
        => _session?.ServerVersion ?? throw new InvalidOperationException("The connection is not open.");

    /// <summary>The qualified name of the server job that services this connection.</summary>
    public string? JobName => _session?.JobName;

    /// <inheritdoc />
    public override ConnectionState State => _state;

    /// <summary>The transaction currently active on this connection, if any.</summary>
    internal MapepireTransaction? Transaction { get; private set; }

    internal int FetchSize => _settings.FetchSize;

    internal ServerSession Session
        => _session ?? throw new InvalidOperationException("The connection is not open.");

    /// <inheritdoc />
    protected override DbProviderFactory DbProviderFactory => MapepireFactory.Instance;

    /// <summary>Closes all idle pooled connections for every connection string.</summary>
    public static void ClearAllPools() => SessionPool.ClearAll();

    /// <summary>Closes the idle pooled connections of every connection string matching <paramref name="predicate"/>.</summary>
    /// <param name="predicate">Selects the connection strings whose pools are cleared.</param>
    public static void ClearPools(Func<MapepireConnectionStringBuilder, bool> predicate) => SessionPool.Clear(predicate);

    /// <summary>Closes all idle pooled connections for the connection string of <paramref name="connection"/>.</summary>
    /// <param name="connection">The connection whose pool is cleared.</param>
    public static void ClearPool(MapepireConnection connection) => SessionPool.Clear(connection.ConnectionString);

    /// <inheritdoc />
    public override void Open()
    {
        if (_state == ConnectionState.Open)
        {
            throw new InvalidOperationException("The connection is already open.");
        }

        _state = ConnectionState.Connecting;
        try
        {
            _session = SessionPool.Rent(_connectionString, _settings);
            _state = ConnectionState.Open;
        }
        catch
        {
            _state = ConnectionState.Closed;
            throw;
        }

        OnStateChange(new StateChangeEventArgs(ConnectionState.Closed, ConnectionState.Open));
    }

    /// <inheritdoc />
    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(Open, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// An active transaction is rolled back, the default schema is restored, and the server connection is
    /// returned to the pool.
    /// </remarks>
    public override void Close()
    {
        if (_state == ConnectionState.Closed || _session is null)
        {
            return;
        }

        var session = _session;
        try
        {
            if (Transaction is not null)
            {
                try
                {
                    EndTransaction(Transaction, commit: false, disposing: true);
                }
                catch
                {
                    // A failed rollback marks the session broken, and the pool then discards it.
                }
            }

            if (_currentSchema is not null && !session.IsBroken)
            {
                // Restore the default schema before the session goes back to the pool.
                try
                {
                    ExecuteInternal(_settings.DefaultSchema is { Length: > 0 } defaultSchema
                        ? "SET SCHEMA " + QuoteIdentifierIfNeeded(defaultSchema)
                        : "SET SCHEMA USER");
                }
                catch
                {
                    // A session left on another schema must not be reused, so it is closed instead.
                    session.Dispose();
                }
            }
        }
        finally
        {
            _currentSchema = null;
            _session = null;
            Transaction = null;
            _state = ConnectionState.Closed;
            SessionPool.Return(_connectionString, _settings, session);
        }

        OnStateChange(new StateChangeEventArgs(ConnectionState.Open, ConnectionState.Closed));
    }

    /// <inheritdoc />
    public override void ChangeDatabase(string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ExecuteInternal("SET SCHEMA " + QuoteIdentifierIfNeeded(databaseName));
        _currentSchema = databaseName;
    }

    /// <summary>
    /// Starts a transaction. With <c>Auto Commit=false</c> this is a real unit of work ended by COMMIT or
    /// ROLLBACK. In auto-commit mode (the default) statements are committed as they run: <c>Commit()</c> does
    /// nothing and <c>Rollback()</c> throws.
    /// </summary>
    public new MapepireTransaction BeginTransaction() => (MapepireTransaction)BeginDbTransaction(IsolationLevel.Unspecified);

    /// <inheritdoc cref="BeginTransaction()" />
    public new MapepireTransaction BeginTransaction(IsolationLevel isolationLevel)
        => (MapepireTransaction)BeginDbTransaction(isolationLevel);

    /// <summary>Creates a command on this connection, enlisted in the active transaction if there is one.</summary>
    public new MapepireCommand CreateCommand() => (MapepireCommand)CreateDbCommand();

    /// <summary>Quotes an identifier unless it is an ordinary upper-case Db2 identifier.</summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>The identifier, as it should appear in SQL.</returns>
    internal static string QuoteIdentifierIfNeeded(string identifier)
    {
        if (identifier.Length > 0
            && identifier.All(character => char.IsAsciiLetterUpper(character) || char.IsAsciiDigit(character) || character is '_' or '#' or '@' or '$')
            && !char.IsAsciiDigit(identifier[0]))
        {
            return identifier;
        }

        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>Ends the active transaction with COMMIT or ROLLBACK.</summary>
    /// <param name="transaction">The transaction to end; it must be the active one.</param>
    /// <param name="commit">True to commit, false to roll back.</param>
    /// <param name="disposing">
    /// True when called from <c>Dispose</c> or <c>Close</c>; in auto-commit mode the implicit rollback is then not an error.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The transaction is not the active one, or an explicit rollback was requested in auto-commit mode.
    /// </exception>
    internal void EndTransaction(MapepireTransaction transaction, bool commit, bool disposing = false)
    {
        if (!ReferenceEquals(Transaction, transaction))
        {
            throw new InvalidOperationException("The transaction has already completed.");
        }

        if (transaction.IsAutoCommit)
        {
            Transaction = null;
            if (!commit && !disposing)
            {
                throw new InvalidOperationException(
                    "Cannot roll back: the connection is in auto-commit mode, so every statement has already been committed. "
                    + "Use 'Auto Commit=false' in the connection string for transactions that can be rolled back "
                    + "(the tables involved must be journaled).");
            }

            return;
        }

        try
        {
            ExecuteInternal(commit ? "COMMIT" : "ROLLBACK");
        }
        finally
        {
            Transaction = null;
        }
    }

    /// <summary>
    /// Called after each command completes. When the connection is not in auto-commit mode and no
    /// transaction is active, the work is committed so that commands behave as they do in auto-commit mode.
    /// </summary>
    internal void OnCommandCompleted()
    {
        if (!_settings.AutoCommit && Transaction is null && _session is { IsBroken: false })
        {
            ExecuteInternal("COMMIT");
        }
    }

    /// <inheritdoc />
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        if (_state != ConnectionState.Open)
        {
            throw new InvalidOperationException("The connection is not open.");
        }

        if (Transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active on this connection. Nested transactions are not supported.");
        }

        if (_settings.AutoCommit)
        {
            // Like JDBC in auto-commit mode: every statement is committed as it runs, so the transaction
            // object only delimits the work. Commit is a no-op; an explicit Rollback throws.
            Transaction = new MapepireTransaction(this, isolationLevel, isAutoCommit: true);
            return Transaction;
        }

        var level = isolationLevel switch
        {
            IsolationLevel.Unspecified => null,
            IsolationLevel.ReadUncommitted => "READ UNCOMMITTED",
            IsolationLevel.ReadCommitted => "READ COMMITTED",
            IsolationLevel.RepeatableRead => "REPEATABLE READ",
            IsolationLevel.Serializable => "SERIALIZABLE",
            _ => throw new NotSupportedException($"Isolation level {isolationLevel} is not supported by Db2 for i."),
        };

        if (level is not null)
        {
            ExecuteInternal("SET TRANSACTION ISOLATION LEVEL " + level);
        }

        Transaction = new MapepireTransaction(this, isolationLevel);
        return Transaction;
    }

    /// <inheritdoc />
    protected override DbCommand CreateDbCommand() => new MapepireCommand { Connection = this, Transaction = Transaction };

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Close();
        }

        base.Dispose(disposing);
    }

    private void ExecuteInternal(string sql) => Session.Execute(sql, null, 1);
}
