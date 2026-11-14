using System.ComponentModel;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Mapepire.EntityFrameworkCore.Diagnostics.Internal;
using Mapepire.EntityFrameworkCore.Infrastructure;
using Mapepire.EntityFrameworkCore.Metadata.Internal;
using Mapepire.EntityFrameworkCore.Migrations.Internal;
using Mapepire.EntityFrameworkCore.Query.Internal;
using Mapepire.EntityFrameworkCore.Storage.Internal;
using Mapepire.EntityFrameworkCore.Update.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers the Mapepire (Db2 for i) EF Core provider services.</summary>
public static class MapepireServiceCollectionExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TContext"/> configured to use Db2 for i through Mapepire.
    /// </summary>
    public static IServiceCollection AddMapepire<TContext>(
        this IServiceCollection serviceCollection,
        string? connectionString,
        Action<MapepireDbContextOptionsBuilder>? mapepireOptionsAction = null,
        Action<DbContextOptionsBuilder>? optionsAction = null)
        where TContext : DbContext
        => serviceCollection.AddDbContext<TContext>(
            (_, options) =>
            {
                optionsAction?.Invoke(options);
                options.UseMapepire(connectionString, mapepireOptionsAction);
            });

    /// <summary>
    /// Adds the provider's internal services. Only needed together with
    /// <see cref="DbContextOptionsBuilder.UseInternalServiceProvider"/>; use <c>UseMapepire</c> otherwise.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection AddEntityFrameworkMapepire(this IServiceCollection serviceCollection)
    {
        new EntityFrameworkRelationalServicesBuilder(serviceCollection)
            .TryAdd<LoggingDefinitions, MapepireLoggingDefinitions>()
            .TryAdd<IDatabaseProvider, DatabaseProvider<MapepireOptionsExtension>>()
            .TryAdd<IRelationalTypeMappingSource, MapepireTypeMappingSource>()
            .TryAdd<ISqlGenerationHelper, MapepireSqlGenerationHelper>()
            .TryAdd<IRelationalAnnotationProvider, MapepireAnnotationProvider>()
            .TryAdd<IProviderConventionSetBuilder, MapepireConventionSetBuilder>()
            .TryAdd<IModificationCommandBatchFactory, MapepireModificationCommandBatchFactory>()
            .TryAdd<IRelationalConnection, MapepireRelationalConnection>()
            .TryAdd<IMigrationsSqlGenerator, MapepireMigrationsSqlGenerator>()
            .TryAdd<IRelationalDatabaseCreator, MapepireDatabaseCreator>()
            .TryAdd<IHistoryRepository, MapepireHistoryRepository>()
            .TryAdd<IMethodCallTranslatorProvider, MapepireMethodCallTranslatorProvider>()
            .TryAdd<IMemberTranslatorProvider, MapepireMemberTranslatorProvider>()
            .TryAdd<IQuerySqlGeneratorFactory, MapepireQuerySqlGeneratorFactory>()
            .TryAdd<IRelationalParameterBasedSqlProcessorFactory, MapepireParameterBasedSqlProcessorFactory>()
            .TryAdd<IUpdateSqlGenerator, MapepireUpdateSqlGenerator>()
            .TryAddCoreServices();

        return serviceCollection;
    }
}
