using System.Data.Common;

namespace Mapepire.Data;

/// <summary>
/// An error returned by Db2 for i through the Mapepire server.
/// </summary>
public sealed class MapepireException : DbException
{
    public MapepireException(string message, int sqlCode = 0, string? sqlState = null, Exception? innerException = null)
        : base(message, innerException)
    {
        SqlCode = sqlCode;
        SqlStateValue = sqlState;
        HResult = sqlCode;
    }

    /// <summary>The SQLCODE reported by Db2.</summary>
    public int SqlCode { get; }

    private string? SqlStateValue { get; }

    /// <inheritdoc />
    public override string? SqlState => SqlStateValue;

    /// <inheritdoc />
    public override int ErrorCode => SqlCode;

    /// <inheritdoc />
    public override bool IsTransient
        => SqlStateValue is "57033" /* deadlock or timeout without rollback */
            or "40001" /* deadlock or timeout with rollback */
            or "57014" /* processing cancelled */;
}
