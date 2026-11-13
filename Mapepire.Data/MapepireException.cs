using System.Data.Common;

namespace Mapepire.Data;

/// <summary>
/// An error returned by Db2 for i through the Mapepire server.
/// </summary>
public sealed class MapepireException : DbException
{
    private readonly string? _sqlState;

    /// <summary>Creates an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="sqlCode">The SQLCODE reported by Db2, or 0 if the error did not come from Db2.</param>
    /// <param name="sqlState">The SQLSTATE reported by Db2, if any.</param>
    /// <param name="innerException">The underlying error, if any.</param>
    public MapepireException(string message, int sqlCode = 0, string? sqlState = null, Exception? innerException = null)
        : base(message, innerException)
    {
        SqlCode = sqlCode;
        _sqlState = sqlState;
        HResult = sqlCode;
    }

    /// <summary>The SQLCODE reported by Db2.</summary>
    public int SqlCode { get; }

    /// <inheritdoc />
    public override string? SqlState => _sqlState;

    /// <inheritdoc />
    public override int ErrorCode => SqlCode;

    /// <inheritdoc />
    public override bool IsTransient
        => _sqlState is "57033" /* deadlock or timeout without rollback */
            or "40001" /* deadlock or timeout with rollback */
            or "57014" /* processing cancelled */;
}
