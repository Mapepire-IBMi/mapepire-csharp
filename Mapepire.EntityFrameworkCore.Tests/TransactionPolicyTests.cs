using System.Data;
using System.Data.Common;
using Mapepire.Data;
using Microsoft.EntityFrameworkCore;

namespace Mapepire.EntityFrameworkCore.Tests;

/// <summary>Verifies the transaction contract using a Mapepire server that records SQL requests.</summary>
[TestClass]
public sealed class TransactionPolicyTests
{
    private const string MarkerSql = "VALUES 1";

    /// <summary>Rejects transactions in the default auto-commit mode without sending SQL or enlisting commands.</summary>
    /// <param name="useAsync">Whether to use the asynchronous ADO.NET entry point.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task BeginTransactionRejectsDefaultAutoCommitAsync(bool useAsync)
    {
        await using var server = await StartMockAsync();
        var builder = new MapepireConnectionStringBuilder(server.ConnectionString) { Pooling = false };
        builder.Remove("Auto Commit");
        Assert.IsTrue(builder.AutoCommit);
        using var connection = new MapepireConnection(builder.ConnectionString);
        connection.Open();

        var error = useAsync
            ? await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
            {
                await using var unexpected = await ((DbConnection)connection).BeginTransactionAsync(IsolationLevel.Serializable);
            })
            : Assert.ThrowsException<InvalidOperationException>(() =>
            {
                using var unexpected = connection.BeginTransaction(IsolationLevel.Serializable);
            });

        StringAssert.Contains(error.Message, "Auto Commit=false");
        StringAssert.Contains(error.Message, "journaled");
        await AssertOnlyMarkerWasSentAsync(server, connection);
    }

    /// <summary>Requires an explicit opt-in before a multi-statement EF save can run in auto-commit mode.</summary>
    /// <param name="useAsync">Whether to use asynchronous SaveChanges.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task MultiStatementSaveRejectsAutoCommitBeforeSendingSqlAsync(bool useAsync)
    {
        await using var server = await StartMockAsync();
        var builder = new MapepireConnectionStringBuilder(server.ConnectionString) { AutoCommit = true, Pooling = false };
        using var context = BloggingContext.Create(builder.ConnectionString);
        await context.Database.OpenConnectionAsync();
        context.AddRange(
            new Tag { Id = Guid.NewGuid(), Label = "First" },
            new Tag { Id = Guid.NewGuid(), Label = "Second" });

        var error = useAsync
            ? await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => context.SaveChangesAsync())
            : Assert.ThrowsException<InvalidOperationException>(() => context.SaveChanges());

        StringAssert.Contains(error.Message, "Auto Commit=false");
        await AssertOnlyMarkerWasSentAsync(server, context.Database.GetDbConnection());
    }

    /// <summary>Checks that accepted transactions send their requested completion to Db2.</summary>
    /// <param name="commit">Whether to commit or roll back the transaction.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RealTransactionSendsRequestedCompletionAsync(bool commit)
    {
        await using var server = await StartMockAsync();
        var builder = new MapepireConnectionStringBuilder(server.ConnectionString) { Pooling = false };
        using var connection = new MapepireConnection(builder.ConnectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        Assert.IsFalse(transaction.IsAutoCommit);

        using var command = connection.CreateCommand();
        Assert.AreSame(transaction, command.Transaction);
        command.CommandText = MarkerSql;
        command.ExecuteScalar();

        if (commit)
        {
            transaction.Commit();
        }
        else
        {
            transaction.Rollback();
        }

        string expected = commit ? "COMMIT" : "ROLLBACK";
        Assert.AreEqual(1, await server.WaitForSqlAsync(sql => sql == expected, 1));
        Assert.AreEqual(2, await server.WaitForSqlAsync(_ => true, 2), "Expected the command and one transaction completion.");
        using var nextCommand = connection.CreateCommand();
        Assert.IsNull(nextCommand.Transaction);
    }

    private static Task<MockMapepireServer> StartMockAsync() => MockMapepireServer.StartAsync(new
    {
        pattern = MarkerSql,
        columns = new[] { new { name = "N", type = "INTEGER" } },
        rows = new[] { new[] { 1 } },
    });

    /// <summary>Uses a valid query to prove the connection still works and to flush earlier mock-server output.</summary>
    /// <param name="server">The mock server recording SQL.</param>
    /// <param name="connection">The connection whose transaction attempt failed.</param>
    private static async Task AssertOnlyMarkerWasSentAsync(MockMapepireServer server, DbConnection connection)
    {
        using var marker = connection.CreateCommand();
        Assert.IsNull(marker.Transaction);
        marker.CommandText = MarkerSql;
        Assert.IsNotNull(marker.ExecuteScalar());
        Assert.AreEqual(1, await server.WaitForSqlAsync(sql => sql == marker.CommandText, 1));
        Assert.AreEqual(1, await server.WaitForSqlAsync(_ => true, 1), "Only the marker query should have been sent.");
    }
}
