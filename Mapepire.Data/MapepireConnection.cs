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

    public MapepireConnection()
    {
    }

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

    /// <summary>The parsed connection settings.</summary>
    public MapepireConnectionStringBuilder Settings => _settings;

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

    /// <inheritdoc />
    protected override DbProviderFactory DbProviderFactory => MapepireFactory.Instance;

    /// <summary>The transaction currently active on this connection, if any.</summary>
    internal MapepireTransaction? Transaction { get; private set; }

    internal int FetchSize => _settings.FetchSize;

    internal ServerSession Session
        => _session ?? throw new InvalidOperationException("The connection is not open.");

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
    public override void Close()
    {
        if (_state == ConnectionState.Closed || _session == null)
        {
            return;
        }

        var session = _session;
        try
        {
            if (Transaction != null)
            {
                try
                {
                    EndTransaction(Transaction, commit: false, disposing: true);
                }
                catch
                {
                    // Session will be discarded below if it is broken.
                }
            }

            if (_currentSchema != null && !session.IsBroken)
            {
                // Restore the default schema before the session goes back to the pool.
                try
                {
                    ExecuteInternal(_settings.DefaultSchema is { Length: > 0 } s
                        ? "SET SCHEMA " + QuoteIdentifierIfNeeded(s)
                        : "SET SCHEMA USER");
                }
                catch
                {
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

    /// <inheritdoc />
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        if (_state != ConnectionState.Open)
        {
            throw new InvalidOperationException("The connection is not open.");
        }

        if (Transaction != null)
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

        if (level != null)
        {
            ExecuteInternal("SET TRANSACTION ISOLATION LEVEL " + level);
        }

        Transaction = new MapepireTransaction(this, isolationLevel);
        return Transaction;
    }

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
        if (!_settings.AutoCommit && Transaction == null && _session is { IsBroken: false })
        {
            ExecuteInternal("COMMIT");
        }
    }

    private void ExecuteInternal(string sql) => Session.Execute(sql, null, 1);

    /// <inheritdoc />
    public new MapepireCommand CreateCommand() => (MapepireCommand)CreateDbCommand();

    /// <inheritdoc />
    protected override DbCommand CreateDbCommand() => new MapepireCommand { Connection = this, Transaction = Transaction };

    /// <summary>Closes all idle pooled connections for every connection string.</summary>
    public static void ClearAllPools() => SessionPool.ClearAll();

    /// <summary>Closes all idle pooled connections for the connection string of <paramref name="connection"/>.</summary>
    public static void ClearPool(MapepireConnection connection) => SessionPool.Clear(connection.ConnectionString);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Close();
        }

        base.Dispose(disposing);
    }

    internal static string QuoteIdentifierIfNeeded(string identifier)
    {
        if (identifier.Length > 0 && identifier.All(c => char.IsAsciiLetterUpper(c) || char.IsAsciiDigit(c) || c is '_' or '#' or '@' or '$')
            && !char.IsAsciiDigit(identifier[0]))
        {
            return identifier;
        }

        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }
}
