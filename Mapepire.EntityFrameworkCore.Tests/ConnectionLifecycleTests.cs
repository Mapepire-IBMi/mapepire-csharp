using System.Data;
using Mapepire.Data;

namespace Mapepire.EntityFrameworkCore.Tests;

/// <summary>Pool reuse after transaction completion, against the Mapepire mock server.</summary>
[TestClass]
public sealed class ConnectionLifecycleTests
{
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
