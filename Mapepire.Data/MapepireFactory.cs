using System.Data.Common;

namespace Mapepire.Data;

/// <summary>Creates Mapepire ADO.NET objects. Register with <c>DbProviderFactories.RegisterFactory("Mapepire.Data", MapepireFactory.Instance)</c>.</summary>
public sealed class MapepireFactory : DbProviderFactory
{
    /// <summary>The singleton instance.</summary>
    public static readonly MapepireFactory Instance = new();

    private MapepireFactory()
    {
    }

    /// <inheritdoc />
    public override DbCommand CreateCommand() => new MapepireCommand();

    /// <inheritdoc />
    public override DbConnection CreateConnection() => new MapepireConnection();

    /// <inheritdoc />
    public override DbConnectionStringBuilder CreateConnectionStringBuilder() => new MapepireConnectionStringBuilder();

    /// <inheritdoc />
    public override DbParameter CreateParameter() => new MapepireParameter();
}
