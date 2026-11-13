using System.Data;

namespace Mapepire.Data.Internal;

internal enum Db2TypeKind
{
    String,
    Int16,
    Int32,
    Int64,
    Decimal,
    Single,
    Double,
    Boolean,
    Date,
    Time,
    Timestamp,
    Binary,
}

/// <summary>Maps Db2 for i type names (as reported in result metadata) to CLR types.</summary>
internal sealed record Db2Type(string Name, Db2TypeKind Kind)
{
    public Type ClrType => Kind switch
    {
        Db2TypeKind.String => typeof(string),
        Db2TypeKind.Int16 => typeof(short),
        Db2TypeKind.Int32 => typeof(int),
        Db2TypeKind.Int64 => typeof(long),
        Db2TypeKind.Decimal => typeof(decimal),
        Db2TypeKind.Single => typeof(float),
        Db2TypeKind.Double => typeof(double),
        Db2TypeKind.Boolean => typeof(bool),
        Db2TypeKind.Date => typeof(DateTime),
        Db2TypeKind.Time => typeof(TimeSpan),
        Db2TypeKind.Timestamp => typeof(DateTime),
        Db2TypeKind.Binary => typeof(byte[]),
        _ => typeof(string),
    };

    public DbType DbType => Kind switch
    {
        Db2TypeKind.String => DbType.String,
        Db2TypeKind.Int16 => DbType.Int16,
        Db2TypeKind.Int32 => DbType.Int32,
        Db2TypeKind.Int64 => DbType.Int64,
        Db2TypeKind.Decimal => DbType.Decimal,
        Db2TypeKind.Single => DbType.Single,
        Db2TypeKind.Double => DbType.Double,
        Db2TypeKind.Boolean => DbType.Boolean,
        Db2TypeKind.Date => DbType.Date,
        Db2TypeKind.Time => DbType.Time,
        Db2TypeKind.Timestamp => DbType.DateTime2,
        Db2TypeKind.Binary => DbType.Binary,
        _ => DbType.String,
    };

    /// <summary>
    /// Classifies a Db2 type name such as <c>VARCHAR</c>, <c>DECIMAL(10,2)</c> or <c>CHAR () FOR BIT DATA</c>.
    /// </summary>
    /// <param name="typeName">The type name from result metadata; null is treated as VARCHAR.</param>
    /// <returns>The type; unknown names are treated as strings.</returns>
    public static Db2Type FromName(string? typeName)
    {
        var name = (typeName ?? "VARCHAR").Trim().ToUpperInvariant();
        if (name.Contains("FOR BIT DATA", StringComparison.Ordinal))
        {
            return new Db2Type(name, Db2TypeKind.Binary);
        }

        var baseName = name;
        var paren = baseName.IndexOf('(');
        if (paren > 0)
        {
            baseName = baseName[..paren].Trim();
        }

        var kind = baseName switch
        {
            "SMALLINT" => Db2TypeKind.Int16,
            "INTEGER" or "INT" => Db2TypeKind.Int32,
            "BIGINT" => Db2TypeKind.Int64,
            "DECIMAL" or "NUMERIC" or "DEC" or "DECFLOAT" => Db2TypeKind.Decimal,
            "REAL" => Db2TypeKind.Single,
            "DOUBLE" or "FLOAT" or "DOUBLE PRECISION" => Db2TypeKind.Double,
            "BOOLEAN" => Db2TypeKind.Boolean,
            "DATE" => Db2TypeKind.Date,
            "TIME" => Db2TypeKind.Time,
            "TIMESTAMP" or "TIMESTMP" => Db2TypeKind.Timestamp,
            "BINARY" or "VARBINARY" or "BLOB" or "ROWID" => Db2TypeKind.Binary,
            _ => Db2TypeKind.String,
        };

        return new Db2Type(name, kind);
    }
}
