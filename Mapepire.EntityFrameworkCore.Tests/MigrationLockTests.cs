using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Mapepire.Data;

namespace Mapepire.EntityFrameworkCore.Tests;

/// <summary>Migration lock cleanup after the operation's token is canceled, against the Mapepire mock server.</summary>
[TestClass]
public sealed class MigrationLockTests
{
    private static bool IsLockInsert(string sql) => sql.StartsWith("INSERT INTO \"__EFMigrationsLock\"", StringComparison.Ordinal);

    private static bool IsLockDelete(string sql) => sql.StartsWith("DELETE FROM \"__EFMigrationsLock\"", StringComparison.Ordinal);

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task AsyncDisposalReleasesLockRegardlessOfCallerCancellation(bool cancel)
    {
        await using var server = await MockMapepireServer.StartAsync(
            // The lock table already exists.
            new
            {
                pattern = @"select count\(\*\) from qsys2\.systables .*",
                columns = new[] { new { name = "COUNT", type = "INTEGER" } },
                rows = new[] { new[] { 1 } },
            },
            // The lock is free, so inserting the lock row succeeds.
            new { pattern = "insert into \"__EFMigrationsLock\" .*", update_count = 1 });
        var builder = new MapepireConnectionStringBuilder(server.ConnectionString) { Pooling = false, AutoCommit = true };
        using var context = BloggingContext.Create(builder.ConnectionString);
        await context.Database.OpenConnectionAsync();
        var history = context.GetService<IHistoryRepository>();
        using var cancellation = new CancellationTokenSource();
        var migrationLock = await history.AcquireDatabaseLockAsync(cancellation.Token);
        Assert.AreEqual(1, await server.WaitForSqlAsync(IsLockInsert, 1));
        if (cancel)
        {
            cancellation.Cancel();
        }

        await migrationLock.DisposeAsync();
        Assert.AreEqual(1, await server.WaitForSqlAsync(IsLockDelete, 1), "Cleanup must delete the lock row even after cancellation.");

        await using (await history.AcquireDatabaseLockAsync())
        {
            Assert.AreEqual(2, await server.WaitForSqlAsync(IsLockInsert, 2), "The next migration must be able to acquire the lock.");
        }

        Assert.AreEqual(2, await server.WaitForSqlAsync(IsLockDelete, 2));
    }
}
