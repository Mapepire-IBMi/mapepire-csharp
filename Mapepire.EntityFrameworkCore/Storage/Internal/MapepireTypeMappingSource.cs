namespace Mapepire.EntityFrameworkCore.Storage.Internal;

/// <summary>
/// Maps CLR types to Db2 for i column types.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>bool</c> maps to SMALLINT (0/1) so it works on every IBM i release; configure a column type of BOOLEAN for IBM i 7.5+.</item>
/// <item>Unicode strings map to NVARCHAR(n) (UTF-16), non-Unicode strings to VARCHAR(n). Strings without a maximum
/// length map to NVARCHAR(4000), or NVARCHAR(450) for keys and indexes. Db2 for i limits a row to 32,766 bytes, so set a
/// maximum length on wide tables.</item>
/// <item><c>Guid</c> maps to CHAR(36); <c>byte[]</c> to VARBINARY(n).</item>
/// </list>
/// </remarks>
public class MapepireTypeMappingSource : RelationalTypeMappingSource
{
    internal const int DefaultStringLength = 4000;
    internal const int DefaultKeyStringLength = 450;
    internal const int DefaultBinaryLength = 8000;
    internal const int DefaultKeyBinaryLength = 900;

    private static readonly IntTypeMapping Integer = new("INTEGER", DbType.Int32);
    private static readonly LongTypeMapping BigInt = new("BIGINT", DbType.Int64);
    private static readonly ShortTypeMapping SmallInt = new("SMALLINT", DbType.Int16);
    private static readonly ByteTypeMapping ByteSmallInt = new("SMALLINT", DbType.Int16);
    private static readonly BoolTypeMapping BoolSmallInt = new("SMALLINT", DbType.Int16);
    private static readonly MapepireBooleanTypeMapping Boolean = new();
    private static readonly DoubleTypeMapping Double = new("DOUBLE", DbType.Double);
    private static readonly FloatTypeMapping Real = new("REAL", DbType.Single);
    private static readonly DecimalTypeMapping DecFloat = new("DECFLOAT", DbType.Decimal);
    private static readonly MapepireDateTimeTypeMapping Timestamp = new();
    private static readonly MapepireDateTypeMapping DateTimeDate = new();
    private static readonly MapepireDateOnlyTypeMapping DateOnly = new();
    private static readonly MapepireTimeOnlyTypeMapping TimeOnly = new();
    private static readonly MapepireTimeSpanTypeMapping TimeSpan = new();
    private static readonly GuidTypeMapping Guid = new("CHAR(36)", DbType.String);

    public MapepireTypeMappingSource(
        TypeMappingSourceDependencies dependencies,
        RelationalTypeMappingSourceDependencies relationalDependencies)
        : base(dependencies, relationalDependencies)
    {
    }

    /// <inheritdoc />
    protected override RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
        => base.FindMapping(mappingInfo) ?? FindRawMapping(mappingInfo);

    /// <summary>
    /// Finds a mapping for a property: by its configured column type if it has one, otherwise by its CLR type.
    /// </summary>
    /// <param name="mappingInfo">The CLR type, column type and facets requested.</param>
    /// <returns>The mapping, or null if the type is not supported.</returns>
    private static RelationalTypeMapping? FindRawMapping(in RelationalTypeMappingInfo mappingInfo)
    {
        var clrType = mappingInfo.ClrType;
        var storeTypeName = mappingInfo.StoreTypeName;

        if (storeTypeName is not null)
        {
            var mapping = FindByStoreType(storeTypeName, clrType, mappingInfo);
            if (mapping is not null && (clrType is null || mapping.ClrType == clrType))
            {
                return mapping;
            }

            // Unknown store type: fall through to the CLR type if there is one.
            if (clrType is null)
            {
                return null;
            }
        }

        return clrType is null ? null : FindByClrType(clrType, mappingInfo);
    }

    /// <summary>Chooses the default Db2 for i column type for a CLR type, applying size, precision and Unicode facets.</summary>
    /// <param name="clrType">The CLR type.</param>
    /// <param name="info">The facets requested.</param>
    /// <returns>The mapping, or null if the CLR type is not supported.</returns>
    private static RelationalTypeMapping? FindByClrType(Type clrType, in RelationalTypeMappingInfo info)
    {
        if (clrType == typeof(int))
        {
            return Integer;
        }

        if (clrType == typeof(long))
        {
            return BigInt;
        }

        if (clrType == typeof(short))
        {
            return SmallInt;
        }

        if (clrType == typeof(byte))
        {
            return ByteSmallInt;
        }

        if (clrType == typeof(bool))
        {
            return BoolSmallInt;
        }

        if (clrType == typeof(decimal))
        {
            var precision = info.Precision ?? 18;
            var scale = info.Scale ?? 2;
            return new DecimalTypeMapping($"DECIMAL({precision},{scale})", DbType.Decimal, precision, scale);
        }

        if (clrType == typeof(double))
        {
            return Double;
        }

        if (clrType == typeof(float))
        {
            return Real;
        }

        if (clrType == typeof(DateTime))
        {
            return Timestamp;
        }

        if (clrType == typeof(DateOnly))
        {
            return DateOnly;
        }

        if (clrType == typeof(TimeOnly))
        {
            return TimeOnly;
        }

        if (clrType == typeof(TimeSpan))
        {
            return TimeSpan;
        }

        if (clrType == typeof(Guid))
        {
            return Guid;
        }

        if (clrType == typeof(string))
        {
            var unicode = info.IsUnicode ?? true;
            var fixedLength = info.IsFixedLength ?? false;
            var size = info.Size ?? (info.IsKeyOrIndex ? DefaultKeyStringLength : DefaultStringLength);
            var name = (fixedLength, unicode) switch
            {
                (true, true) => "NCHAR",
                (true, false) => "CHAR",
                (false, true) => "NVARCHAR",
                (false, false) => "VARCHAR",
            };

            return new StringTypeMapping($"{name}({size})", unicode ? DbType.String : DbType.AnsiString, unicode, size);
        }

        if (clrType == typeof(byte[]))
        {
            if (info.IsRowVersion == true)
            {
                return null;
            }

            var size = info.Size ?? (info.IsKeyOrIndex ? DefaultKeyBinaryLength : DefaultBinaryLength);
            return new MapepireByteArrayTypeMapping(info.IsFixedLength == true ? $"BINARY({size})" : $"VARBINARY({size})", size);
        }

        return null;
    }

    /// <summary>Maps an explicitly configured Db2 for i column type.</summary>
    /// <param name="storeTypeName">The column type, for example <c>VARCHAR(50)</c> or <c>CHAR(10) FOR BIT DATA</c>.</param>
    /// <param name="clrType">The CLR type of the property, if known; it selects among mappings for the same column type.</param>
    /// <param name="info">The facets parsed from the column type.</param>
    /// <returns>The mapping, or null if the column type is not recognized.</returns>
    private static RelationalTypeMapping? FindByStoreType(string storeTypeName, Type? clrType, in RelationalTypeMappingInfo info)
    {
        var upper = storeTypeName.Trim().ToUpperInvariant();
        var baseName = (info.StoreTypeNameBase ?? storeTypeName).Trim().ToUpperInvariant();
        var paren = baseName.IndexOf('(');
        if (paren > 0)
        {
            baseName = baseName[..paren].Trim();
        }

        if (upper.Contains("FOR BIT DATA", StringComparison.Ordinal))
        {
            return new MapepireByteArrayTypeMapping(storeTypeName, info.Size);
        }

        switch (baseName)
        {
            case "BOOLEAN":
                return Boolean;

            case "SMALLINT":
                return clrType == typeof(bool)
                    ? BoolSmallInt
                    : clrType == typeof(byte)
                        ? ByteSmallInt
                        : SmallInt;

            case "INTEGER":
            case "INT":
                return Integer;

            case "BIGINT":
                return BigInt;

            case "DECIMAL":
            case "DEC":
            case "NUMERIC":
            {
                var precision = info.Precision ?? 5;
                var scale = info.Scale ?? 0;
                return new DecimalTypeMapping(storeTypeName, DbType.Decimal, precision, scale);
            }

            case "DECFLOAT":
                return DecFloat;

            case "REAL":
                return Real;

            case "DOUBLE":
            case "FLOAT":
            case "DOUBLE PRECISION":
                return Double;

            case "DATE":
                return clrType == typeof(DateTime) ? DateTimeDate : DateOnly;

            case "TIME":
                return clrType == typeof(TimeSpan) ? TimeSpan : TimeOnly;

            case "TIMESTAMP":
                return Timestamp;

            case "BINARY":
            case "VARBINARY":
            case "BLOB":
                return new MapepireByteArrayTypeMapping(storeTypeName, info.Size);

            case "CHAR":
            case "CHARACTER":
            case "VARCHAR":
            case "CLOB":
            case "NCHAR":
            case "NVARCHAR":
            case "NCLOB":
            case "GRAPHIC":
            case "VARGRAPHIC":
            case "DBCLOB":
                if (clrType == typeof(Guid) && baseName is "CHAR" or "CHARACTER" or "NCHAR")
                {
                    return new GuidTypeMapping(storeTypeName, DbType.String);
                }

                var unicode = baseName.StartsWith('N') || baseName is "GRAPHIC" or "VARGRAPHIC" or "DBCLOB"
                    || upper.Contains("CCSID 1200", StringComparison.Ordinal)
                    || upper.Contains("CCSID 1208", StringComparison.Ordinal)
                    || upper.Contains("CCSID 13488", StringComparison.Ordinal);
                return new StringTypeMapping(storeTypeName, unicode ? DbType.String : DbType.AnsiString, unicode, info.Size);

            default:
                return null;
        }
    }
}
