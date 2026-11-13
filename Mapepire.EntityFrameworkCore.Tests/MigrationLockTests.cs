using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Mapepire.Data;

namespace Mapepire.EntityFrameworkCore.Tests;

/// <summary>Migration lock cleanup after the operation's token is canceled.</summary>
[TestClass]
public sealed class MigrationLockTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task AsyncDisposalReleasesLockRegardlessOfCallerCancellation(bool cancel)
    {
        var locked = 0;
        await using var server = new OfflineMapepireServer(sql =>
        {
            if (sql.StartsWith("SELECT COUNT(*)", StringComparison.Ordinal))
            {
                // The lock table already exists.
                return new
                {
                    success = true,
                    is_done = true,
                    has_results = true,
                    metadata = new { columns = new[] { new { name = "COUNT", type = "INTEGER" } } },
                    data = new[] { new[] { 1 } },
                };
            }

            if (sql.StartsWith("INSERT INTO", StringComparison.Ordinal))
            {
                return new { success = true, is_done = true, update_count = Interlocked.Exchange(ref locked, 1) == 0 ? 1 : 0 };
            }

            if (sql.StartsWith("DELETE FROM", StringComparison.Ordinal))
            {
                Interlocked.Exchange(ref locked, 0);
                return OfflineMapepireServer.Success();
            }

            throw new InvalidOperationException("Unexpected migration lock SQL: " + sql);
        });
        var builder = new MapepireConnectionStringBuilder(server.ConnectionString) { Pooling = false, AutoCommit = true };
        using var context = BloggingContext.Create(builder.ConnectionString);
        await context.Database.OpenConnectionAsync();
        var history = context.GetService<IHistoryRepository>();
        using var cancellation = new CancellationTokenSource();
        var migrationLock = await history.AcquireDatabaseLockAsync(cancellation.Token);
        Assert.AreEqual(1, Volatile.Read(ref locked));
        if (cancel)
        {
            cancellation.Cancel();
        }

        await migrationLock.DisposeAsync();
        Assert.AreEqual(0, Volatile.Read(ref locked), "Cleanup must delete the lock row even after cancellation.");

        await using var nextLock = await history.AcquireDatabaseLockAsync();
        Assert.AreEqual(1, Volatile.Read(ref locked), "The next migration must be able to acquire the lock.");
    }
}
