using System.Reflection;
using System.Text.Json;
using io.github.mapepire_ibmi;
using io.github.mapepire_ibmi.types;

namespace UnitTests;

/// <summary>Opt-in, read-only cleanup checks against a live Mapepire server.</summary>
[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public sealed class QueryCleanupIntegrationTests
{
    private const string ThreeRows = "SELECT 1 AS N FROM SYSIBM.SYSDUMMY1 UNION ALL SELECT 2 AS N FROM SYSIBM.SYSDUMMY1 UNION ALL SELECT 3 AS N FROM SYSIBM.SYSDUMMY1 ORDER BY N";
    private static SqlJob? job;
    private readonly List<Query> queries = [];

    /// <summary>Connects once for the class, including only one authentication attempt.</summary>
    [ClassInitialize]
    public static void Connect(TestContext _)
    {
        string? host = Environment.GetEnvironmentVariable("MAPEPIRE_HOST");
        string? user = Environment.GetEnvironmentVariable("MAPEPIRE_USER");
        string? password = Environment.GetEnvironmentVariable("MAPEPIRE_PASSWORD");
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrEmpty(password))
        {
            Assert.Inconclusive("Set MAPEPIRE_HOST, MAPEPIRE_USER, and MAPEPIRE_PASSWORD to run the live cleanup tests.");
        }

        string portText = Environment.GetEnvironmentVariable("MAPEPIRE_PORT") ?? "8076";
        Assert.IsTrue(int.TryParse(portText, out int port), "MAPEPIRE_PORT must be a valid integer.");
        bool rejectUnauthorized = !string.Equals(Environment.GetEnvironmentVariable("MAPEPIRE_REJECT_UNAUTHORIZED"), "false", StringComparison.OrdinalIgnoreCase);
        var server = new DaemonServer(host!, port, user!, password!, rejectUnauthorized, Environment.GetEnvironmentVariable("MAPEPIRE_CA"));
        job = new SqlJob();
        try
        {
            ConnectionResult? connection = job.Connect(server);
            Assert.IsNotNull(connection);
            Assert.IsTrue(connection.Success, "The live connection was unsuccessful.");
        }
        catch
        {
            job.Close();
            job = null;
            throw;
        }
    }

    [ClassCleanup]
    public static void Disconnect() => job?.Close();

    [TestCleanup]
    public void CloseQueries()
    {
        foreach (Query query in queries)
        {
            query.Close();
        }
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void CompletedQueriesLeaveRegistry(bool terse, bool prepared)
    {
        for (int value = 1; value <= 3; value++)
        {
            Query query = NewQuery(prepared ? "VALUES CAST(? AS INTEGER)" : $"VALUES {value}",
                new QueryOptions { IsTerseResults = terse, Parameters = prepared ? [value] : null });
            QueryResult result = query.Execute(10);

            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.IsDone);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(1, result.Data.Count);
            Assert.AreEqual(value, ((JsonElement)result.Data[0].Values.Single()).GetInt32());
            AssertNotTracked(query);
            Assert.IsNull(Query.ById(result.Id));
        }
    }

    [TestMethod]
    public void PagedQueryStaysTrackedUntilCompletion()
    {
        Query query = NewQuery(ThreeRows);
        QueryResult result = query.Execute(1);
        string? cursorId = result.Id;
        var values = new List<int>();
        for (int page = 0; page < 5; page++)
        {
            Assert.IsNotNull(result.Data);
            values.AddRange(result.Data.Select(row => ((JsonElement)row["N"]).GetInt32()));
            if (result.IsDone)
            {
                break;
            }

            Assert.AreSame(query, Query.ById(cursorId));
            result = query.FetchMore(1);
        }

        Assert.IsTrue(result.IsDone, "The three-row query did not finish within five pages.");
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, values);
        AssertNotTracked(query);
        Assert.IsNull(Query.ById(cursorId));
    }

    [TestMethod]
    public void EarlyCloseRemovesPagedQuery()
    {
        Query query = NewQuery(ThreeRows);
        QueryResult result = query.Execute(1);
        Assert.IsFalse(result.IsDone);
        Assert.AreSame(query, Query.ById(result.Id));

        string response = query.Close();
        Assert.IsNotNull(response);
        using JsonDocument close = JsonDocument.Parse(response);
        Assert.IsTrue(close.RootElement.GetProperty("success").GetBoolean());
        AssertNotTracked(query);
        Assert.IsNull(Query.ById(result.Id));
        Assert.IsNull(query.Close());
    }

    [TestMethod]
    public void SqlErrorRemovesQuery()
    {
        Query query = NewQuery("SELECT FROM");

        Assert.ThrowsException<SqlException>(() => query.Execute());

        AssertNotTracked(query);
    }

    private Query NewQuery(string sql, QueryOptions? options = null)
    {
        Assert.IsNotNull(job);
        Query query = job.Query(sql, options ?? new QueryOptions());
        queries.Add(query);
        return query;
    }

    private static void AssertNotTracked(Query query)
    {
        var registry = (List<Query>)typeof(Query).GetField("GlobalQueryList", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        lock (registry)
        {
            Assert.IsFalse(registry.Any(entry => ReferenceEquals(entry, query)), "The live query remains in the global registry.");
        }
    }
}
