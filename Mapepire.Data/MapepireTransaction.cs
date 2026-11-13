using System.Data;
using System.Data.Common;

namespace Mapepire.Data;

/// <summary>
/// A Db2 for i unit of work, ended with COMMIT or ROLLBACK.
/// </summary>
/// <remarks>
/// When the connection is in auto-commit mode (<c>Auto Commit=true</c>, the default) every statement is
/// committed as it runs; see <see cref="IsAutoCommit"/>.
/// </remarks>
public sealed class MapepireTransaction : DbTransaction
{
    private MapepireConnection? _connection;

    internal MapepireTransaction(MapepireConnection connection, IsolationLevel isolationLevel, bool isAutoCommit = false)
    {
        _connection = connection;
        IsolationLevel = isolationLevel;
        IsAutoCommit = isAutoCommit;
    }

    /// <inheritdoc />
    public override IsolationLevel IsolationLevel { get; }

    /// <summary>
    /// True when the connection is in auto-commit mode. Statements are then committed as they run:
    /// <see cref="Commit"/> does nothing and <see cref="Rollback"/> throws <see cref="InvalidOperationException"/>.
    /// </summary>
    public bool IsAutoCommit { get; }

    /// <inheritdoc />
    protected override DbConnection? DbConnection => _connection;

    /// <summary>The connection this transaction belongs to, or null once it has completed.</summary>
    public new MapepireConnection? Connection => _connection;

    /// <inheritdoc />
    public override void Commit() => End(commit: true, disposing: false);

    /// <inheritdoc />
    public override void Rollback() => End(commit: false, disposing: false);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && _connection is not null)
        {
            try
            {
                End(commit: false, disposing: true);
            }
            catch
            {
                // Dispose must not throw.
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>Ends the transaction through its connection. A transaction can only be ended once.</summary>
    private void End(bool commit, bool disposing)
    {
        var connection = _connection ?? throw new InvalidOperationException("The transaction has already completed.");
        _connection = null;
        connection.EndTransaction(this, commit, disposing);
    }
}
