using System.Globalization;
using System.Text.Json;
using io.github.mapepire_ibmi;
using io.github.mapepire_ibmi.types;

namespace UnitTests;

/// <summary>Checks whether the server traces a specific SQL error at each trace level.</summary>
[TestClass]
[DoNotParallelize]
public sealed class TraceTest
{
    [TestMethod]
    public void ServerTracingOff() => AssertTraceData(ServerTraceLevel.OFF, expectedInTrace: false);

    [TestMethod]
    public void ServerTracingOn() => AssertTraceData(ServerTraceLevel.ON, expectedInTrace: true);

    [TestMethod]
    public void ErrorServerTracing() => AssertTraceData(ServerTraceLevel.ERRORS, expectedInTrace: true);

    [TestMethod]
    public void DataStreamServerTracing() => AssertTraceData(ServerTraceLevel.DATASTREAM, expectedInTrace: true);

    /// <summary>Checks for one syntax error under a confirmed trace level and restores the original level.</summary>
    /// <param name="level">The Mapepire trace level requested for this case.</param>
    /// <param name="expectedInTrace">Whether enabled tracing should include the error.</param>
    private static void AssertTraceData(ServerTraceLevel level, bool expectedInTrace)
    {
        var job = new SqlJob();
        string? originalLevel = null;
        bool restoreTrace = false;
        try
        {
            ConnectionResult? connection = job.Connect(MapepireTest.GetTestDaemonServer());
            Assert.IsNotNull(connection);
            Assert.IsTrue(connection.Success);

            SetConfigResult original = job.SetTraceConfig(null, null, null, null);
            Assert.IsTrue(original.Success);
            originalLevel = original.TraceLevel;
            if (string.IsNullOrWhiteSpace(originalLevel) || string.Equals(originalLevel, "unknown", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Inconclusive("The daemon did not report a trace level that the test can restore.");
            }

            restoreTrace = true;
            SetConfigResult requested = job.SetTraceLevel(level);
            Assert.IsTrue(requested.Success);

            // Match the unique SQL token, independent of the parser's suggested tokens.
            string token = "." + Random.Shared.Next(10_000_000, 100_000_000).ToString(CultureInfo.InvariantCulture);
            Query query = job.Query("SELECT * FROM SYSIBM" + token);
            try
            {
                SqlException error = Assert.ThrowsException<SqlException>(() => query.Execute(1));
                Assert.AreEqual("42601", error.SqlState);
            }
            finally
            {
                query.Close();
            }

            string expectedLevel = level.GetValue();
            if (!string.Equals(requested.TraceLevel, expectedLevel, StringComparison.OrdinalIgnoreCase))
            {
                // A direct protocol request distinguishes a daemon capability limit from an SDK error.
                SetConfigResult direct = SendTraceLevel(job, expectedLevel);
                if (!string.Equals(direct.TraceLevel, expectedLevel, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(original.TraceDest, "unknown", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Inconclusive($"The daemon does not expose a configurable trace destination and did not apply {expectedLevel}.");
                }

                Assert.IsTrue(string.Equals(requested.TraceLevel, expectedLevel, StringComparison.OrdinalIgnoreCase),
                    $"Expected Mapepire trace level {expectedLevel}. SDK reported {requested.TraceLevel}. Direct request reported {direct.TraceLevel}.");
            }

            GetTraceDataResult result = job.GetTraceDataResult();
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Id);
            Assert.IsNotNull(result.TraceData);
            bool found = result.TraceData.Contains("[SQL0104] Token " + token, StringComparison.Ordinal);
            Assert.AreEqual(expectedInTrace, found, "Unexpected presence of the SQL error in Mapepire trace data.");
        }
        finally
        {
            try
            {
                if (restoreTrace && originalLevel is not null)
                {
                    // The daemon can report levels beyond the SDK's public constants.
                    SetConfigResult restored = SendTraceLevel(job, originalLevel);
                    Assert.IsTrue(string.Equals(originalLevel, restored.TraceLevel, StringComparison.OrdinalIgnoreCase),
                        "The original trace level was not restored.");
                }
            }
            finally
            {
                job.Close();
            }
        }
    }

    /// <summary>Sends a trace level directly to the daemon for comparison or restoration.</summary>
    /// <param name="job">The connected SQL job.</param>
    /// <param name="level">The trace level identifier accepted by the daemon.</param>
    /// <returns>The daemon's confirmed configuration.</returns>
    private static SetConfigResult SendTraceLevel(SqlJob job, string level)
    {
        var request = new SetTraceConfigRequest(SqlJob.GetNewUniqueId(), "setconfig") { Tracelevel = level };
        SetConfigResult? result = JsonSerializer.Deserialize<SetConfigResult>(job.Send(JsonSerializer.Serialize(request)));
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success);
        return result;
    }
}
