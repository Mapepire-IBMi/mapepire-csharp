using Microsoft.EntityFrameworkCore.Storage.Internal;

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
        if (schema == null)
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
        var builder = new MapepireConnectionStringBuilder(Dependencies.Connection.ConnectionString) { DefaultSchema = null, Pooling = false };
        using var connection = new MapepireConnection(builder.ConnectionString);
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

        ClearPool();
        var builder = new MapepireConnectionStringBuilder(Dependencies.Connection.ConnectionString) { DefaultSchema = null, Pooling = false };
        using var connection = new MapepireConnection(builder.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DROP SCHEMA " + Dependencies.SqlGenerationHelper.DelimitIdentifier(schema);
        command.ExecuteNonQuery();
    }

    /// <inheritdoc />
    public override bool HasTables()
    {
        var schema = GetConfiguredSchema();
        var predicate = schema == null ? "CURRENT SCHEMA" : Literal(schema);
        var count = ExecuteScalar(
            $"SELECT COUNT(*) FROM QSYS2.SYSTABLES WHERE TABLE_SCHEMA = {predicate} AND TABLE_TYPE IN ('T', 'P')");
        return Convert.ToInt64(count) > 0;
    }

    private object? ExecuteScalar(string sql)
        => rawSqlCommandBuilder.Build(sql).ExecuteScalar(
            new RelationalCommandParameterObject(
                Dependencies.Connection,
                null,
                null,
                Dependencies.CurrentContext.Context,
                Dependencies.CommandLogger,
                CommandSource.Migrations));

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
}
