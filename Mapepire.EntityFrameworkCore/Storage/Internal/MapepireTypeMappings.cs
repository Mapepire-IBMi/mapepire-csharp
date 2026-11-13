using System.Globalization;

namespace Mapepire.EntityFrameworkCore.Storage.Internal;

/// <summary>TIMESTAMP mapping with Db2 for i literal syntax.</summary>
public class MapepireDateTimeTypeMapping : DateTimeTypeMapping
{
    public MapepireDateTimeTypeMapping(string storeType = "TIMESTAMP", DbType? dbType = System.Data.DbType.DateTime2)
        : base(storeType, dbType)
    {
    }

    protected MapepireDateTimeTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new MapepireDateTimeTypeMapping(parameters);

    protected override string GenerateNonNullSqlLiteral(object value)
        => "CAST('" + ((DateTime)value).ToString("yyyy-MM-dd-HH.mm.ss.ffffff", CultureInfo.InvariantCulture) + "' AS TIMESTAMP)";
}

/// <summary>DATE mapping for <see cref="DateOnly"/>.</summary>
public class MapepireDateOnlyTypeMapping : DateOnlyTypeMapping
{
    public MapepireDateOnlyTypeMapping(string storeType = "DATE")
        : base(storeType, System.Data.DbType.Date)
    {
    }

    protected MapepireDateOnlyTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new MapepireDateOnlyTypeMapping(parameters);

    protected override string GenerateNonNullSqlLiteral(object value)
        => "CAST('" + ((DateOnly)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "' AS DATE)";
}

/// <summary>DATE mapping for <see cref="DateTime"/> properties configured with column type DATE.</summary>
public class MapepireDateTypeMapping : DateTimeTypeMapping
{
    public MapepireDateTypeMapping(string storeType = "DATE")
        : base(storeType, System.Data.DbType.Date)
    {
    }

    protected MapepireDateTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new MapepireDateTypeMapping(parameters);

    protected override string GenerateNonNullSqlLiteral(object value)
        => "CAST('" + ((DateTime)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "' AS DATE)";
}

/// <summary>TIME mapping for <see cref="TimeOnly"/>.</summary>
public class MapepireTimeOnlyTypeMapping : TimeOnlyTypeMapping
{
    public MapepireTimeOnlyTypeMapping(string storeType = "TIME")
        : base(storeType, System.Data.DbType.Time)
    {
    }

    protected MapepireTimeOnlyTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new MapepireTimeOnlyTypeMapping(parameters);

    protected override string GenerateNonNullSqlLiteral(object value)
        => "CAST('" + ((TimeOnly)value).ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "' AS TIME)";
}

/// <summary>TIME mapping for <see cref="TimeSpan"/> (time of day; Db2 for i TIME has whole-second precision).</summary>
public class MapepireTimeSpanTypeMapping : TimeSpanTypeMapping
{
    public MapepireTimeSpanTypeMapping(string storeType = "TIME")
        : base(storeType, System.Data.DbType.Time)
    {
    }

    protected MapepireTimeSpanTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new MapepireTimeSpanTypeMapping(parameters);

    protected override string GenerateNonNullSqlLiteral(object value)
        => "CAST('" + ((TimeSpan)value).ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture) + "' AS TIME)";
}

/// <summary>Binary mapping using Db2 for i binary string constants (<c>BX'...'</c>).</summary>
public class MapepireByteArrayTypeMapping : ByteArrayTypeMapping
{
    public MapepireByteArrayTypeMapping(string storeType, int? size = null)
        : base(storeType, System.Data.DbType.Binary, size)
    {
    }

    protected MapepireByteArrayTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new MapepireByteArrayTypeMapping(parameters);

    protected override string GenerateNonNullSqlLiteral(object value)
        => "BX'" + Convert.ToHexString((byte[])value) + "'";
}

/// <summary>Native BOOLEAN mapping (IBM i 7.5 and later), used when a column type of BOOLEAN is configured.</summary>
public class MapepireBooleanTypeMapping : BoolTypeMapping
{
    public MapepireBooleanTypeMapping()
        : base("BOOLEAN", System.Data.DbType.Boolean)
    {
    }

    protected MapepireBooleanTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new MapepireBooleanTypeMapping(parameters);

    protected override string GenerateNonNullSqlLiteral(object value) => (bool)value ? "TRUE" : "FALSE";
}
