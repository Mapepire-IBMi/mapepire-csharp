using System.Data;
using System.Data.Common;

namespace Mapepire.Data;

/// <summary>
/// A Db2 for i unit of work, ended with COMMIT or ROLLBACK.
/// </summary>
public sealed class MapepireTransaction : DbTransaction
{
    private MapepireConnection? _connection;

    internal MapepireTransaction(MapepireConnection connection, IsolationLevel isolationLevel)
    {
        _connection = connection;
        IsolationLevel = isolationLevel;
    }

    /// <inheritdoc />
    public override IsolationLevel IsolationLevel { get; }

    /// <summary>
    /// Always false. Transactions can only be started when the connection has auto-commit disabled.
    /// </summary>
    public bool IsAutoCommit => false;

    /// <inheritdoc />
    protected override DbConnection? DbConnection => _connection;

    /// <summary>The connection this transaction belongs to, or null once it has completed.</summary>
    public new MapepireConnection? Connection => _connection;

    /// <inheritdoc />
    public override void Commit() => End(commit: true);

    /// <inheritdoc />
    public override void Rollback() => End(commit: false);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && _connection is not null)
        {
            try
            {
                End(commit: false);
            }
            catch
            {
                // Dispose must not throw.
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>Ends the transaction through its connection. A transaction can only be ended once.</summary>
    private void End(bool commit)
    {
        var connection = _connection ?? throw new InvalidOperationException("The transaction has already completed.");
        _connection = null;
        connection.EndTransaction(this, commit);
    }
}
