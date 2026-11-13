namespace Mapepire.EntityFrameworkCore.Infrastructure;

/// <summary>Options for the Mapepire (Db2 for i) EF Core provider.</summary>
public class MapepireOptionsExtension : RelationalOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;

    public MapepireOptionsExtension()
    {
    }

    protected MapepireOptionsExtension(MapepireOptionsExtension copyFrom)
        : base(copyFrom)
    {
    }

    /// <inheritdoc />
    public override DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    /// <inheritdoc />
    protected override RelationalOptionsExtension Clone() => new MapepireOptionsExtension(this);

    /// <inheritdoc />
    public override void ApplyServices(IServiceCollection services) => services.AddEntityFrameworkMapepire();

    private sealed class ExtensionInfo(IDbContextOptionsExtension extension) : RelationalExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => true;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => other is ExtensionInfo;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) => debugInfo["Mapepire"] = "1";
    }
}
