namespace Mapepire.EntityFrameworkCore.Migrations.Internal;

/// <summary>
/// Manages the <c>__EFMigrationsHistory</c> table and the migration lock for Db2 for i.
/// </summary>
/// <remarks>
/// The lock is a row in a <c>__EFMigrationsLock</c> table: inserting the row acquires it, deleting it releases it.
/// </remarks>
public class MapepireHistoryRepository(HistoryRepositoryDependencies dependencies) : HistoryRepository(dependencies)
{
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(1);

    /// <inheritdoc />
    public override LockReleaseBehavior LockReleaseBehavior => LockReleaseBehavior.Explicit;

    /// <summary>Name of the table used as the migration lock.</summary>
    protected virtual string LockTableName => "__EFMigrationsLock";

    /// <inheritdoc />
    protected override string ExistsSql => CreateExistsSql(TableName);

    private string LockTable => SqlGenerationHelper.DelimitIdentifier(LockTableName, TableSchema);

    /// <inheritdoc />
    public override string GetCreateIfNotExistsScript()
        => WrapIgnoringAlreadyExists(GetCreateScript());

    /// <inheritdoc />
    public override string GetBeginIfNotExistsScript(string migrationId)
        => $"""
            BEGIN
              IF NOT EXISTS (SELECT 1 FROM {SqlGenerationHelper.DelimitIdentifier(TableName, TableSchema)} WHERE {SqlGenerationHelper.DelimitIdentifier(MigrationIdColumnName)} = {Literal(migrationId)}) THEN
            """;

    /// <inheritdoc />
    public override string GetBeginIfExistsScript(string migrationId)
        => $"""
            BEGIN
              IF EXISTS (SELECT 1 FROM {SqlGenerationHelper.DelimitIdentifier(TableName, TableSchema)} WHERE {SqlGenerationHelper.DelimitIdentifier(MigrationIdColumnName)} = {Literal(migrationId)}) THEN
            """;

    /// <inheritdoc />
    public override string GetEndIfScript()
        => """
              END IF;
            END
            """ + Environment.NewLine;

    /// <inheritdoc />
    public override IMigrationsDatabaseLock AcquireDatabaseLock()
    {
        Dependencies.MigrationsLogger.AcquiringMigrationLock();

        if (!InterpretExistsResult(Command(CreateExistsSql(LockTableName)).ExecuteScalar(Parameters())))
        {
            Command(WrapIgnoringAlreadyExists(CreateLockTableSql())).ExecuteNonQuery(Parameters());
        }

        var retryDelay = InitialRetryDelay;
        while (true)
        {
            if (Convert.ToInt32(Command(InsertLockSql()).ExecuteNonQuery(Parameters())) == 1)
            {
                return new MapepireMigrationDatabaseLock(Command(DeleteLockSql()), Parameters(), this);
            }

            Thread.Sleep(retryDelay);
            if (retryDelay < TimeSpan.FromMinutes(1))
            {
                retryDelay += retryDelay;
            }
        }
    }

    /// <inheritdoc />
    public override async Task<IMigrationsDatabaseLock> AcquireDatabaseLockAsync(CancellationToken cancellationToken = default)
    {
        Dependencies.MigrationsLogger.AcquiringMigrationLock();

        if (!InterpretExistsResult(
                await Command(CreateExistsSql(LockTableName)).ExecuteScalarAsync(Parameters(), cancellationToken).ConfigureAwait(false)))
        {
            await Command(WrapIgnoringAlreadyExists(CreateLockTableSql()))
                .ExecuteNonQueryAsync(Parameters(), cancellationToken).ConfigureAwait(false);
        }

        var retryDelay = InitialRetryDelay;
        while (true)
        {
            var inserted = await Command(InsertLockSql()).ExecuteNonQueryAsync(Parameters(), cancellationToken).ConfigureAwait(false);
            if (inserted == 1)
            {
                return new MapepireMigrationDatabaseLock(Command(DeleteLockSql()), Parameters(), this, cancellationToken);
            }

            await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
            if (retryDelay < TimeSpan.FromMinutes(1))
            {
                retryDelay += retryDelay;
            }
        }
    }

    /// <inheritdoc />
    protected override bool InterpretExistsResult(object? value) => Convert.ToInt64(value) != 0;

    /// <summary>Wraps a CREATE statement so it succeeds when the object already exists (SQLSTATE 42710).</summary>
    private static string WrapIgnoringAlreadyExists(string createSql)
    {
        var sql = createSql.Trim().TrimEnd(';');
        return $"""
            BEGIN
              DECLARE CONTINUE HANDLER FOR SQLSTATE '42710' BEGIN END;
              EXECUTE IMMEDIATE {Literal(sql)};
            END
            """;
    }

    private static string Literal(string value) => "'" + value.Replace("'", "''") + "'";

    private string CreateExistsSql(string tableName)
    {
        var schemaPredicate = TableSchema is null ? "CURRENT SCHEMA" : Literal(TableSchema);
        return $"SELECT COUNT(*) FROM QSYS2.SYSTABLES WHERE TABLE_SCHEMA = {schemaPredicate} AND TABLE_NAME = {Literal(tableName)}";
    }

    private string CreateLockTableSql()
        => $"""CREATE TABLE {LockTable} ("Id" INTEGER NOT NULL CONSTRAINT {SqlGenerationHelper.DelimitIdentifier("PK_" + LockTableName)} PRIMARY KEY, "Timestamp" TIMESTAMP NOT NULL)""";

    private string InsertLockSql()
        => $"""INSERT INTO {LockTable} ("Id", "Timestamp") SELECT 1, CURRENT TIMESTAMP FROM SYSIBM.SYSDUMMY1 WHERE NOT EXISTS (SELECT 1 FROM {LockTable} WHERE "Id" = 1)""";

    private string DeleteLockSql() => $"""DELETE FROM {LockTable} WHERE "Id" = 1""";

    private IRelationalCommand Command(string sql) => Dependencies.RawSqlCommandBuilder.Build(sql);

    private RelationalCommandParameterObject Parameters()
        => new(
            Dependencies.Connection,
            null,
            null,
            Dependencies.CurrentContext.Context,
            Dependencies.CommandLogger,
            CommandSource.Migrations);
}

/// <summary>Releases the migration lock by deleting the lock row.</summary>
public sealed class MapepireMigrationDatabaseLock(
    IRelationalCommand releaseLockCommand,
    RelationalCommandParameterObject relationalCommandParameters,
    IHistoryRepository historyRepository,
    CancellationToken cancellationToken = default) : IMigrationsDatabaseLock
{
    /// <inheritdoc />
    public IHistoryRepository HistoryRepository => historyRepository;

    /// <inheritdoc />
    public void Dispose() => releaseLockCommand.ExecuteNonQuery(relationalCommandParameters);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Keep the constructor's optional token for compatibility, but never cancel lock release.
        _ = cancellationToken;
        await releaseLockCommand.ExecuteNonQueryAsync(relationalCommandParameters, CancellationToken.None).ConfigureAwait(false);
    }
}
