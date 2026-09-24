using io.github.mapepire_ibmi;
using io.github.mapepire_ibmi.types;

namespace UnitTests;

[TestClass]
public sealed class ResponseCorrelationIntegrationTests
{
    [TestMethod]
    [TestCategory("Integration")]
    public async Task ConcurrentGetVersionCallsReturnSuccessfulDistinctResponses()
    {
        string? host = Environment.GetEnvironmentVariable("MAPEPIRE_HOST");
        string? portText = Environment.GetEnvironmentVariable("MAPEPIRE_PORT");
        string? user = Environment.GetEnvironmentVariable("MAPEPIRE_USER");
        string? password = Environment.GetEnvironmentVariable("MAPEPIRE_PASSWORD");

        if (string.IsNullOrWhiteSpace(host)
            || string.IsNullOrWhiteSpace(portText)
            || string.IsNullOrWhiteSpace(user)
            || string.IsNullOrWhiteSpace(password))
        {
            Assert.Inconclusive("Set MAPEPIRE_HOST, MAPEPIRE_PORT, MAPEPIRE_USER, and MAPEPIRE_PASSWORD to run this integration test.");
        }

        Assert.IsTrue(int.TryParse(portText, out int port), "MAPEPIRE_PORT must be a valid integer.");
        string? rejectUnauthorizedSetting = Environment.GetEnvironmentVariable("MAPEPIRE_REJECT_UNAUTHORIZED");
        bool rejectUnauthorized = !string.Equals(rejectUnauthorizedSetting, "false", StringComparison.OrdinalIgnoreCase);

        SqlJob? job = null;
        try
        {
            job = new SqlJob();
            ConnectionResult? connection = job.Connect(new DaemonServer(host!, port, user!, password!, rejectUnauthorized));
            Assert.IsNotNull(connection, "Connect returned no result.");
            Assert.IsTrue(connection.Success, "Connection was unsuccessful.");

            Task<VersionCheckResult>[] calls = Enumerable.Range(0, 20)
                .Select(_ => Task.Run(() => job.GetVersion()))
                .ToArray();
            VersionCheckResult[] results = await Task.WhenAll(calls).WaitAsync(TimeSpan.FromSeconds(30));

            Assert.AreEqual(20, results.Length);
            Assert.IsTrue(results.All(result => result.Success), "At least one GetVersion call failed.");
            Assert.IsTrue(results.All(result => !string.IsNullOrWhiteSpace(result.Id)), "A response did not echo its request ID.");
            Assert.AreEqual(20, results.Select(result => result.Id).Distinct(StringComparer.Ordinal).Count(),
                "GetVersion responses must echo distinct request IDs.");
        }
        finally
        {
            job?.Close();
        }
    }
}
