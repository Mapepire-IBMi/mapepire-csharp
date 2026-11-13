using Mapepire.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>Extension methods to configure a <see cref="DbContext"/> for Db2 for i through Mapepire.</summary>
public static class MapepireDbContextOptionsBuilderExtensions
{
    /// <summary>Configures the context to use Db2 for i; the connection or connection string is set later.</summary>
    public static DbContextOptionsBuilder UseMapepire(
        this DbContextOptionsBuilder optionsBuilder,
        Action<MapepireDbContextOptionsBuilder>? mapepireOptionsAction = null)
    {
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(GetOrCreateExtension(optionsBuilder));
        mapepireOptionsAction?.Invoke(new MapepireDbContextOptionsBuilder(optionsBuilder));
        return optionsBuilder;
    }

    /// <summary>
    /// Configures the context to connect to Db2 for i through a Mapepire server.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    /// <param name="connectionString">
    /// For example <c>Server=myibmi;Port=8076;User ID=ME;Password=secret;Default Schema=MYLIB</c>.
    /// See <see cref="MapepireConnectionStringBuilder"/> for all keywords.
    /// </param>
    /// <param name="mapepireOptionsAction">Optional provider-specific configuration.</param>
    public static DbContextOptionsBuilder UseMapepire(
        this DbContextOptionsBuilder optionsBuilder,
        string? connectionString,
        Action<MapepireDbContextOptionsBuilder>? mapepireOptionsAction = null)
    {
        var extension = (MapepireOptionsExtension)GetOrCreateExtension(optionsBuilder).WithConnectionString(connectionString);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        mapepireOptionsAction?.Invoke(new MapepireDbContextOptionsBuilder(optionsBuilder));
        return optionsBuilder;
    }

    /// <summary>Configures the context to use an existing <see cref="MapepireConnection"/>.</summary>
    public static DbContextOptionsBuilder UseMapepire(
        this DbContextOptionsBuilder optionsBuilder,
        DbConnection connection,
        Action<MapepireDbContextOptionsBuilder>? mapepireOptionsAction = null)
        => UseMapepire(optionsBuilder, connection, contextOwnsConnection: false, mapepireOptionsAction);

    /// <summary>Configures the context to use an existing <see cref="MapepireConnection"/>.</summary>
    public static DbContextOptionsBuilder UseMapepire(
        this DbContextOptionsBuilder optionsBuilder,
        DbConnection connection,
        bool contextOwnsConnection,
        Action<MapepireDbContextOptionsBuilder>? mapepireOptionsAction = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        var extension = (MapepireOptionsExtension)GetOrCreateExtension(optionsBuilder).WithConnection(connection, contextOwnsConnection);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        mapepireOptionsAction?.Invoke(new MapepireDbContextOptionsBuilder(optionsBuilder));
        return optionsBuilder;
    }

    /// <inheritdoc cref="UseMapepire(DbContextOptionsBuilder, Action{MapepireDbContextOptionsBuilder}?)" />
    public static DbContextOptionsBuilder<TContext> UseMapepire<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        Action<MapepireDbContextOptionsBuilder>? mapepireOptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseMapepire((DbContextOptionsBuilder)optionsBuilder, mapepireOptionsAction);

    /// <inheritdoc cref="UseMapepire(DbContextOptionsBuilder, string?, Action{MapepireDbContextOptionsBuilder}?)" />
    public static DbContextOptionsBuilder<TContext> UseMapepire<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        string? connectionString,
        Action<MapepireDbContextOptionsBuilder>? mapepireOptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseMapepire((DbContextOptionsBuilder)optionsBuilder, connectionString, mapepireOptionsAction);

    /// <inheritdoc cref="UseMapepire(DbContextOptionsBuilder, DbConnection, bool, Action{MapepireDbContextOptionsBuilder}?)" />
    public static DbContextOptionsBuilder<TContext> UseMapepire<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        DbConnection connection,
        bool contextOwnsConnection = false,
        Action<MapepireDbContextOptionsBuilder>? mapepireOptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseMapepire(
            (DbContextOptionsBuilder)optionsBuilder, connection, contextOwnsConnection, mapepireOptionsAction);

    private static MapepireOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder options)
        => options.Options.FindExtension<MapepireOptionsExtension>() ?? new MapepireOptionsExtension();
}
