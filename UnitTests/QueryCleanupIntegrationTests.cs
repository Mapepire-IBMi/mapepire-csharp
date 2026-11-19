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
    private static SqlJob? s_job;
    private readonly List<Query> _queries = [];

    /// <summary>Connects once for the class, including only one authentication attempt.</summary>
    /// <param name="_">Test context required by MSTest.</param>
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
        var server = new DaemonServer(host, port, user, password, rejectUnauthorized, Environment.GetEnvironmentVariable("MAPEPIRE_CA"));
        s_job = new SqlJob();
        try
        {
            ConnectionResult? connection = s_job.Connect(server);
            Assert.IsNotNull(connection);
            Assert.IsTrue(connection.Success, "The live connection was unsuccessful.");
        }
        catch
        {
            s_job.Close();
            s_job = null;
            throw;
        }
    }

    [ClassCleanup]
    public static void Disconnect() => s_job?.Close();

    /// <summary>Closes any query created by the current test before the next case runs.</summary>
    [TestCleanup]
    public void CloseQueries()
    {
        foreach (Query query in _queries)
        {
            query.Close();
        }
    }

    /// <summary>Verifies that repeated queries complete and unregister with each combination of execution options.</summary>
    /// <param name="terse">Whether to request terse rows.</param>
    /// <param name="prepared">Whether to use a prepared statement.</param>
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

    /// <summary>Reads all three rows and checks the original cursor ID between pages.</summary>
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

    /// <summary>Checks that closing a live cursor succeeds and unregisters it.</summary>
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

    /// <summary>Verifies that a syntax error from IBM i unregisters the query.</summary>
    [TestMethod]
    public void SqlErrorRemovesQuery()
    {
        Query query = NewQuery("SELECT FROM");

        Assert.ThrowsException<SqlException>(() => query.Execute());

        AssertNotTracked(query);
    }

    /// <summary>Creates a live query and tracks it for test cleanup.</summary>
    /// <param name="sql">The read-only statement to run.</param>
    /// <param name="options">Optional execution settings.</param>
    /// <returns>The tracked live query.</returns>
    private Query NewQuery(string sql, QueryOptions? options = null)
    {
        Assert.IsNotNull(s_job);
        Query query = s_job.Query(sql, options ?? new QueryOptions());
        _queries.Add(query);
        return query;
    }

    /// <summary>Verifies that the registry no longer retains the specified live query.</summary>
    /// <param name="query">The exact instance to look up.</param>
    private static void AssertNotTracked(Query query)
    {
        var registry = typeof(Query).GetField("GlobalQueryList", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as List<Query>;
        Assert.IsNotNull(registry, "The query registry should be available to the test.");
        lock (registry)
        {
            Assert.IsFalse(registry.Any(entry => ReferenceEquals(entry, query)), "The live query remains in the global registry.");
        }
    }
}
