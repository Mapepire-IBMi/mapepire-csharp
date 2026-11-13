namespace Mapepire.EntityFrameworkCore.Infrastructure;

/// <summary>Mapepire-specific configuration, used from <c>UseMapepire(..., o => ...)</c>.</summary>
public class MapepireDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
    : RelationalDbContextOptionsBuilder<MapepireDbContextOptionsBuilder, MapepireOptionsExtension>(optionsBuilder);
