namespace Mapepire.EntityFrameworkCore.Storage.Internal;

/// <summary>SQL generation helpers for Db2 for i (double-quoted identifiers, <c>@name</c> parameters).</summary>
public class MapepireSqlGenerationHelper(RelationalSqlGenerationHelperDependencies dependencies)
    : RelationalSqlGenerationHelper(dependencies)
{
    /// <summary>Db2 for i starts a unit of work implicitly with the first statement; there is no BEGIN statement.</summary>
    public override string StartTransactionStatement => "-- Db2 for i starts the unit of work implicitly";
}

/// <summary>EF Core connection wrapper creating <see cref="MapepireConnection"/> instances.</summary>
public class MapepireRelationalConnection(RelationalConnectionDependencies dependencies)
    : RelationalConnection(dependencies)
{
    /// <inheritdoc />
    protected override DbConnection CreateDbConnection() => new MapepireConnection(GetValidatedConnectionString());
}

/// <summary>
/// Db2 for i has a single database per system, so EF Core "databases" are mapped onto the default schema
/// (library) of the connection: <c>EnsureCreated</c> creates the schema and <c>EnsureDeleted</c> drops it.
/// </summary>
public class MapepireDatabaseCreator(
    RelationalDatabaseCreatorDependencies dependencies,
    IRawSqlCommandBuilder rawSqlCommandBuilder)
    : RelationalDatabaseCreator(dependencies)
{
    /// <inheritdoc />
    public override bool Exists()
    {
        var schema = GetConfiguredSchema();
        if (schema is null)
        {
            // Without a configured schema the user profile's default schema is used; it exists if we can connect.
            return CanConnect();
        }

        try
        {
            Dependencies.Connection.Open(errorsExpected: true);
        }
        catch (MapepireException)
        {
            return false;
        }

        try
        {
            var count = ExecuteScalar(
                "SELECT COUNT(*) FROM QSYS2.SYSSCHEMAS WHERE SCHEMA_NAME = " + Literal(schema));
            return Convert.ToInt64(count) > 0;
        }
        finally
        {
            Dependencies.Connection.Close();
        }
    }

    /// <inheritdoc />
    public override void Create()
    {
        var schema = GetConfiguredSchema()
            ?? throw new InvalidOperationException(
                "Creating a database requires 'Default Schema' in the connection string. On Db2 for i the schema (library) is created.");

        // Connect without the library list: it would fail validation while the schema does not exist yet.
        using var connection = new MapepireConnection(CreateAdminConnectionString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE SCHEMA " + Dependencies.SqlGenerationHelper.DelimitIdentifier(schema);
        command.ExecuteNonQuery();

        // Pooled sessions opened while the schema did not exist are discarded.
        ClearPool();
    }

    /// <inheritdoc />
    public override void Delete()
    {
        var schema = GetConfiguredSchema()
            ?? throw new InvalidOperationException(
                "Deleting a database requires 'Default Schema' in the connection string. On Db2 for i the schema (library) is dropped.");

        // Pooled server jobs that have the library in their library list keep it in use (SQL0913).
        MapepireConnection.ClearPools(builder => builder.DefaultSchema is not null && NormalizeSchemaName(builder.DefaultSchema) == schema);

        using var connection = new MapepireConnection(CreateAdminConnectionString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DROP SCHEMA " + Dependencies.SqlGenerationHelper.DelimitIdentifier(schema);

        // Closed server jobs can take a moment to end and release the library; retry for a while.
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            try
            {
                command.ExecuteNonQuery();
                return;
            }
            catch (MapepireException ex) when (ex.SqlCode == -913 || ex.Message.Contains("SQL0913", StringComparison.Ordinal))
            {
                if (DateTime.UtcNow > deadline)
                {
                    throw;
                }

                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
        }
    }

    /// <inheritdoc />
    public override bool HasTables()
    {
        var schema = GetConfiguredSchema();
        var predicate = schema is null ? "CURRENT SCHEMA" : Literal(schema);
        var count = ExecuteScalar(
            $"SELECT COUNT(*) FROM QSYS2.SYSTABLES WHERE TABLE_SCHEMA = {predicate} AND TABLE_TYPE IN ('T', 'P')");
        return Convert.ToInt64(count) > 0;
    }

    /// <summary>
    /// Unquoted schema names are folded to upper case, as the IBM i library list does; quoted names keep their case.
    /// </summary>
    internal static string NormalizeSchemaName(string schema)
    {
        schema = schema.Trim();
        return schema.Length > 1 && schema[0] == '"' && schema[^1] == '"'
            ? schema[1..^1].Replace("\"\"", "\"")
            : schema.ToUpperInvariant();
    }

    private static string Literal(string value) => "'" + value.Replace("'", "''") + "'";

    private object? ExecuteScalar(string sql)
        => rawSqlCommandBuilder.Build(sql).ExecuteScalar(
            new RelationalCommandParameterObject(
                Dependencies.Connection,
                null,
                null,
                Dependencies.CurrentContext.Context,
                Dependencies.CommandLogger,
                CommandSource.Migrations));

    /// <summary>
    /// A connection for CREATE/DROP SCHEMA: without the library list (the schema may not exist yet) and outside
    /// commitment control, because DROP SCHEMA fails with SQL5003 under commitment control.
    /// </summary>
    private string CreateAdminConnectionString()
        => new MapepireConnectionStringBuilder(Dependencies.Connection.ConnectionString)
        {
            DefaultSchema = null,
            Pooling = false,
            AutoCommit = true,
            TransactionIsolation = "none",
        }.ConnectionString;

    private void ClearPool()
    {
        if (Dependencies.Connection.DbConnection is MapepireConnection connection)
        {
            MapepireConnection.ClearPool(connection);
        }
    }

    private string? GetConfiguredSchema()
    {
        var schema = new MapepireConnectionStringBuilder(Dependencies.Connection.ConnectionString).DefaultSchema;
        return string.IsNullOrWhiteSpace(schema) ? null : NormalizeSchemaName(schema);
    }
}
