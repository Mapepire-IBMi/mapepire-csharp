using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Mapepire.Data;

namespace Mapepire.EntityFrameworkCore.Tests;

/// <summary>Schema deletion through the provider's database creator.</summary>
[TestClass]
public sealed class DatabaseCreatorTests
{
    /// <summary>Deletion must explicitly use CASCADE and preserve the configured schema's identifier quoting.</summary>
    [TestMethod]
    [DataRow("eftest", "\"EFTEST\"")]
    [DataRow("\"Ef\"\"Test\"", "\"Ef\"\"Test\"")]
    public async Task DeleteUsesNoninteractiveCascade(string schema, string delimitedSchema)
    {
        var expectedSql = "DROP SCHEMA " + delimitedSchema + " CASCADE";
        await using var server = await MockMapepireServer.StartAsync(
            new { pattern = Regex.Escape(expectedSql), update_count = 0 },
            new
            {
                pattern = "drop schema .*",
                error = new { message = "Schema deletion must not wait for an operator inquiry.", sql_state = "58004", sql_rc = -901 },
            });
        var settings = new MapepireConnectionStringBuilder(server.ConnectionString) { DefaultSchema = schema };
        using var context = BloggingContext.Create(settings.ConnectionString);

        context.GetService<IRelationalDatabaseCreator>().Delete();

        Assert.AreEqual(1, await server.WaitForSqlAsync(sql => sql.Equals(expectedSql, StringComparison.Ordinal), 1));
    }
}
