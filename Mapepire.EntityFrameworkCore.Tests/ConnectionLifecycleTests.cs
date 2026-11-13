using System.Data;
using Mapepire.Data;

namespace Mapepire.EntityFrameworkCore.Tests;

/// <summary>Pool reuse after transaction completion, using simulated SQL replies over a real local connection.</summary>
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
        await using var server = new OfflineMapepireServer(sql => sql is "COMMIT" or "ROLLBACK"
            ? new { success = false, error = "Simulated transaction failure", sql_rc = -901, sql_state = "58004" }
            : OfflineMapepireServer.Success());
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
        await using var server = new OfflineMapepireServer(_ => OfflineMapepireServer.Success());
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
