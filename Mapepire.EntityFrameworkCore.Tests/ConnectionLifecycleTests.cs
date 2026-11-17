using System.Data;
using Mapepire.Data;

namespace Mapepire.EntityFrameworkCore.Tests;

/// <summary>Pool reuse after transaction completion, against the Mapepire mock server.</summary>
[TestClass]
public sealed class ConnectionLifecycleTests
{
    /// <summary>An implicit COMMIT failure must discard the session for every command completion path.</summary>
    [TestMethod]
    [DataRow("nonquery")]
    [DataRow("scalar")]
    [DataRow("reader")]
    public async Task FailedImplicitCommitDiscardsSession(string execution)
    {
        await using var server = await MockMapepireServer.StartAsync(
            new
            {
                pattern = "commit",
                error = new { message = "Simulated transaction failure", sql_state = "58004", sql_rc = -901 },
            },
            new { pattern = "update test_table set n = 1", update_count = 1 },
            new
            {
                pattern = "select n from test_table",
                columns = new[] { new { name = "N", type = "INTEGER" } },
                rows = new[] { new[] { 1 } },
            });
        using var connection = new MapepireConnection(server.ConnectionString);
        try
        {
            connection.Open();
            var originalJob = connection.JobName;
            using var command = connection.CreateCommand();
            command.CommandText = execution == "nonquery" ? "UPDATE TEST_TABLE SET N = 1" : "SELECT N FROM TEST_TABLE";
            var error = Assert.ThrowsException<MapepireException>(() =>
            {
                switch (execution)
                {
                    case "nonquery":
                        command.ExecuteNonQuery();
                        break;
                    case "scalar":
                        command.ExecuteScalar();
                        break;
                    case "reader":
                        using (var reader = command.ExecuteReader())
                        {
                            Assert.IsTrue(reader.Read());
                        }
                        break;
                }
            });
            Assert.AreEqual(-901, error.SqlCode);
            Assert.AreEqual("58004", error.SqlState);

            connection.Close();
            connection.Open();
            Assert.AreNotEqual(originalJob, connection.JobName, "A session whose implicit COMMIT failed must not be pooled.");
        }
        finally
        {
            connection.Close();
            MapepireConnection.ClearPool(connection);
        }
    }

    /// <summary>A successful implicit COMMIT preserves normal pool reuse.</summary>
    [TestMethod]
    public async Task SuccessfulImplicitCommitKeepsSessionReusable()
    {
        await using var server = await MockMapepireServer.StartAsync(
            new { pattern = "update test_table set n = 1", update_count = 1 });
        using var connection = new MapepireConnection(server.ConnectionString);
        try
        {
            connection.Open();
            var originalJob = connection.JobName;
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE TEST_TABLE SET N = 1";
            Assert.AreEqual(1, command.ExecuteNonQuery());

            connection.Close();
            connection.Open();
            Assert.AreEqual(originalJob, connection.JobName);
        }
        finally
        {
            connection.Close();
            MapepireConnection.ClearPool(connection);
        }
    }

    [TestMethod]
    [DataRow("close")]
    [DataRow("dispose")]
    [DataRow("rollback")]
    [DataRow("commit")]
    public async Task FailedTransactionCompletionDiscardsSession(string completion)
    {
        await using var server = await MockMapepireServer.StartAsync(new
        {
            pattern = "commit|rollback",
            error = new { message = "Simulated transaction failure", sql_state = "58004", sql_rc = -901 },
        });
        using var connection = new MapepireConnection(server.ConnectionString);
        try
        {
            connection.Open();
            var originalJob = connection.JobName;
            using var transaction = connection.BeginTransaction();
            switch (completion)
            {
                case "close":
                    connection.Close();
                    break;
                case "dispose":
                    transaction.Dispose();
                    break;
                case "rollback":
                    Assert.ThrowsException<MapepireException>(transaction.Rollback);
                    break;
                case "commit":
                    Assert.ThrowsException<MapepireException>(transaction.Commit);
                    break;
            }

            connection.Close();
            connection.Open();
            Assert.AreNotEqual(originalJob, connection.JobName, "A session with uncertain transaction state must not be pooled.");
        }
        finally
        {
            connection.Close();
            MapepireConnection.ClearPool(connection);
        }
    }

    [TestMethod]
    public async Task SuccessfulRollbackKeepsSessionReusable()
    {
        // The mock server's built-in rules let SET, COMMIT and ROLLBACK succeed.
        await using var server = await MockMapepireServer.StartAsync();
        using var connection = new MapepireConnection(server.ConnectionString);
        try
        {
            connection.Open();
            var originalJob = connection.JobName;
            using var transaction = connection.BeginTransaction();
            connection.Close();
            Assert.AreEqual(ConnectionState.Closed, connection.State);

            connection.Open();
            Assert.AreEqual(originalJob, connection.JobName);
        }
        finally
        {
            connection.Close();
            MapepireConnection.ClearPool(connection);
        }
    }
}
