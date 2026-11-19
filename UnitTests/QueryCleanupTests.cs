using System.Net.WebSockets;
using System.Reflection;
using System.Text.Json;
using io.github.mapepire_ibmi;
using io.github.mapepire_ibmi.types;

namespace UnitTests;

/// <summary>Offline regression coverage for the process-wide query registry.</summary>
[TestClass]
[DoNotParallelize]
public sealed class QueryCleanupTests
{
    private readonly List<Query> _queries = [];
    private readonly List<SqlJob> _jobs = [];

    /// <summary>Unregisters queries created by the test and closes their scripted jobs.</summary>
    [TestCleanup]
    public void CleanupTestResources()
    {
        foreach (Query query in _queries)
        {
            query.RemoveFromGlobalQueryList();
        }

        foreach (SqlJob job in _jobs)
        {
            job.Close();
        }
    }

    /// <summary>Checks that closing before execution unregisters the query and permits repeated cleanup.</summary>
    [TestMethod]
    public void ClosingUnexecutedQueryReleasesItAndCanBeRepeated()
    {
        Query query = NewQuery(new SqlJob());
        Assert.IsTrue(IsRegistered(query));

        Assert.IsNull(query.Close());
        Assert.IsNull(query.Close());

        Assert.IsFalse(IsRegistered(query));
    }

    /// <summary>Verifies that each execution mode unregisters a completed query.</summary>
    /// <param name="terse">Whether to request terse rows.</param>
    /// <param name="prepared">Whether to use a prepared statement.</param>
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task ExecuteCompletionReleasesQueryAsync(bool terse, bool prepared)
    {
        SqlJob job = NewJob(out ScriptedWebSocket socket);
        Query query = NewQuery(job, new QueryOptions { IsTerseResults = terse, Parameters = prepared ? [1] : null });
        QueryResult result = await ReplyAsync(socket, query.Execute, done: true);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.IsDone);
        Assert.AreEqual(prepared ? "prepare_sql_execute" : "sql", ReadRequest(socket).GetProperty("type").GetString());
        Assert.IsFalse(IsRegistered(query));
        Assert.IsNull(Query.ById(result.Id));
        Assert.IsNull(query.Close());
    }

    /// <summary>Verifies that the original continuation ID remains usable until the last page.</summary>
    /// <param name="terse">Whether the first page uses terse rows.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task PagingKeepsOriginalCursorUntilLastPageAsync(bool terse)
    {
        SqlJob job = NewJob(out ScriptedWebSocket socket);
        Query query = NewQuery(job, new QueryOptions { IsTerseResults = terse });
        QueryResult first = await ReplyAsync(socket, () => query.Execute(1), done: false);
        Assert.AreSame(query, Query.ById(first.Id));

        await ReplyAsync(socket, () => query.FetchMore(1), done: false);
        Assert.IsTrue(IsRegistered(query));
        Assert.AreSame(query, Query.ById(first.Id));
        Assert.AreEqual(first.Id, ReadRequest(socket).GetProperty("cont_id").GetString());

        QueryResult last = await ReplyAsync(socket, () => query.FetchMore(1), done: true);
        Assert.IsTrue(last.IsDone);
        Assert.AreEqual(first.Id, ReadRequest(socket).GetProperty("cont_id").GetString());
        Assert.IsFalse(IsRegistered(query));
        Assert.IsNull(Query.ById(first.Id));
    }

    /// <summary>Checks that an SQL error unregisters the query even when the server omits the completion flag.</summary>
    /// <param name="terse">Whether to request terse rows.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ExecuteErrorWithoutDoneFlagReleasesQueryAsync(bool terse)
    {
        SqlJob job = NewJob(out ScriptedWebSocket socket);
        Query query = NewQuery(job, new QueryOptions { IsTerseResults = terse });
        SqlException error = await Assert.ThrowsExceptionAsync<SqlException>(async () =>
            await ReplyAsync(socket, query.Execute, error: true));

        Assert.AreEqual("42704", error.SqlState);
        Assert.IsFalse(IsRegistered(query));
    }

    /// <summary>Checks that a failed CL command returns its failure and leaves no registered query or SQL cursor to close.</summary>
    [TestMethod]
    public async Task FailedClCommandReturnsFailureAndReleasesQueryAsync()
    {
        SqlJob job = NewJob(out ScriptedWebSocket socket);
        Query query = job.ClCommand("TESTCOMMAND");
        _queries.Add(query);

        QueryResult result = await ReplyAsync(socket, query.Execute, error: true);

        Assert.IsFalse(result.Success, "CL failures must still be returned rather than thrown.");
        Assert.IsFalse(IsRegistered(query));
        socket.FailNextSend(new WebSocketException("A CL reply has no SQL cursor to close."));
        Assert.IsNull(await Task.Run(query.Close).WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.AreEqual(1, socket.SentMessages.Count);
    }

    /// <summary>Checks that a CL reply without a completion flag is treated as complete.</summary>
    [TestMethod]
    public async Task SuccessfulClCommandWithoutDoneFlagReleasesQueryAsync()
    {
        SqlJob job = NewJob(out ScriptedWebSocket socket);
        Query query = job.ClCommand("TESTCOMMAND");
        _queries.Add(query);
        Task<QueryResult> running = Task.Run(query.Execute);
        await socket.WaitForSendCountAsync(1);
        string? id = ReadRequest(socket).GetProperty("id").GetString();
        Assert.IsNotNull(id, "The request should have a correlation ID.");
        // Server 2.5+ returns CL messages directly, with no cursor or is_done field.
        socket.EnqueueText(JsonSerializer.Serialize(new { id, success = true, data = new[] { new { text = "done" } } }));

        QueryResult result = await running.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.IsDone, "CL messages arrive in one reply even when the server omits is_done.");
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("done", ((JsonElement)result.Data[0]["text"]).GetString());
        Assert.IsFalse(IsRegistered(query));
        Assert.IsNull(query.Close());
        Assert.AreEqual(1, socket.SentMessages.Count, "A CL reply does not create a server SQL cursor.");
    }

    /// <summary>Closes a query before its first response arrives and verifies that the late response cannot re-register it.</summary>
    [TestMethod]
    public async Task ResponseAfterCloseDoesNotRegisterQueryAgainAsync()
    {
        SqlJob job = NewJob(out ScriptedWebSocket socket);
        Query query = NewQuery(job);
        Task<QueryResult> running = Task.Run(query.Execute);
        await socket.WaitForSendCountAsync(1);
        string? id = ReadRequest(socket).GetProperty("id").GetString();
        Assert.IsNotNull(id, "The request should have a correlation ID.");
        Assert.IsNull(query.Close());
        Assert.IsFalse(IsRegistered(query));

        socket.EnqueueText(JsonSerializer.Serialize(new { id, success = true, is_done = false, data = Array.Empty<object>() }));
        await running.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsFalse(IsRegistered(query), "An explicit close must win over late registry registration.");
        Assert.IsNull(Query.ById(id));
    }

    /// <summary>Checks that value equality does not merge the registry entries for distinct query objects.</summary>
    [TestMethod]
    public void DistinctValueEqualQueriesAreRegisteredAndRemovedByIdentity()
    {
        var job = new SqlJob();
        var first = new ValueEqualQuery(job);
        var second = new ValueEqualQuery(job);
        _queries.Add(first);
        _queries.Add(second);

        Assert.IsTrue(IsRegistered(first));
        Assert.IsTrue(IsRegistered(second), "Value equality must not merge distinct query objects.");
        second.Close();

        Assert.IsTrue(IsRegistered(first), "Closing one query must not remove a different equal query.");
        Assert.IsFalse(IsRegistered(second));
    }

    /// <summary>Compares the request payload with the original serializer and verifies that SQL and parameter values stay separate.</summary>
    /// <param name="terse">Whether to request terse rows.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task PreparedParametersRemainSeparateFromSqlAndKeepTheirTypedWireFormatAsync(bool terse)
    {
        SqlJob job = NewJob(out ScriptedWebSocket socket);
        const string sql = "VALUES CAST(? AS VARCHAR(200))";
        const string value = "x'); DROP TABLE EXAMPLE; --\n\"quoted\"";
        var parameters = new List<object> { value, 1234567890123456789L, 123.45m, true, null! };
        Query query = job.Query(sql, new QueryOptions { Parameters = parameters, IsTerseResults = terse });
        _queries.Add(query);

        await ReplyAsync(socket, query.Execute, done: true);
        JsonElement request = ReadRequest(socket);
        string? id = request.GetProperty("id").GetString();
        Assert.IsNotNull(id, "The request should have a correlation ID.");
        var expected = new SqlExecuteWithParametersRequest(id, "prepare_sql_execute", sql, terse, 100, parameters);

        Assert.AreEqual(JsonSerializer.Serialize(expected) + "\n", socket.SentMessages.Last(), "Moving serialization into the cleanup helper must preserve the previous wire representation.");
        Assert.AreEqual(sql, request.GetProperty("sql").GetString());
        Assert.AreEqual(value, request.GetProperty("parameters")[0].GetString());
    }

    /// <summary>Verifies that cleanup on one job preserves the active cursor on another.</summary>
    [TestMethod]
    public async Task CompletingOneJobsQueryDoesNotRemoveAnotherJobsCursorAsync()
    {
        SqlJob firstJob = NewJob(out ScriptedWebSocket firstSocket);
        SqlJob secondJob = NewJob(out ScriptedWebSocket secondSocket);
        Query first = NewQuery(firstJob);
        Query second = NewQuery(secondJob);
        QueryResult open = await ReplyAsync(secondSocket, second.Execute, done: false);

        await ReplyAsync(firstSocket, first.Execute, done: true);
        first.Cleanup();

        Assert.AreEqual(0, Query.GetOpenIds(firstJob).Count);
        CollectionAssert.AreEqual(new[] { open.Id }, Query.GetOpenIds(secondJob));
        Assert.AreSame(second, Query.ById(open.Id));
        Assert.IsTrue(IsRegistered(second));
        await ReplyAsync(secondSocket, second.FetchMore, done: true);
    }

    /// <summary>Checks that a failed fetch unregisters the original continuation ID.</summary>
    [TestMethod]
    public async Task FetchErrorWithoutDoneFlagReleasesQueryAsync()
    {
        SqlJob job = NewJob(out ScriptedWebSocket socket);
        Query query = NewQuery(job);
        QueryResult first = await ReplyAsync(socket, query.Execute, done: false);

        await Assert.ThrowsExceptionAsync<SqlException>(async () =>
            await ReplyAsync(socket, query.FetchMore, error: true));

        Assert.IsFalse(IsRegistered(query));
        Assert.IsNull(Query.ById(first.Id));
    }

    /// <summary>Checks that a transport send failure unregisters the query during execution or fetching.</summary>
    /// <param name="fetch">Whether to fail a fetch instead of the initial execution.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SendFailureReleasesQueryAsync(bool fetch)
    {
        SqlJob job = NewJob(out ScriptedWebSocket socket);
        Query query = NewQuery(job);
        if (fetch)
        {
            await ReplyAsync(socket, query.Execute, done: false);
        }

        socket.FailNextSend(new WebSocketException("scripted send failure"));
        Func<QueryResult> operation = fetch ? query.FetchMore : query.Execute;
        await Assert.ThrowsExceptionAsync<WebSocketException>(async () =>
            await Task.Run(operation).WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.IsFalse(IsRegistered(query));
    }

    /// <summary>Checks that a result that cannot be deserialized unregisters the query.</summary>
    /// <param name="fetch">Whether to fail a fetch instead of the initial execution.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task InvalidResultReleasesQueryAsync(bool fetch)
    {
        SqlJob job = NewJob(out ScriptedWebSocket socket);
        Query query = NewQuery(job);
        if (fetch)
        {
            await ReplyAsync(socket, query.Execute, done: false);
        }

        Func<QueryResult> operation = fetch ? query.FetchMore : query.Execute;
        await Assert.ThrowsExceptionAsync<JsonException>(async () =>
            await ReplyAsync(socket, operation, invalidResult: true));

        Assert.IsFalse(IsRegistered(query));
    }

    /// <summary>Checks that a serialization error unregisters the query before it reaches the transport.</summary>
    [TestMethod]
    public void ParameterSerializationFailureReleasesQuery()
    {
        var cyclicParameter = new List<object>();
        cyclicParameter.Add(cyclicParameter);
        Query query = NewQuery(new SqlJob(), new QueryOptions { Parameters = [cyclicParameter] });

        Assert.ThrowsException<JsonException>(() => query.Execute());

        Assert.IsFalse(IsRegistered(query));
    }

    /// <summary>Checks that a transport receive failure unregisters the query during execution or fetching.</summary>
    /// <param name="fetch">Whether to fail a fetch instead of the initial execution.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ReceiveFailureReleasesQueryAsync(bool fetch)
    {
        SqlJob job = NewJob(out ScriptedWebSocket socket);
        Query query = NewQuery(job);
        if (fetch)
        {
            await ReplyAsync(socket, query.Execute, done: false);
        }

        int count = socket.SentMessages.Count;
        Func<QueryResult> operation = fetch ? query.FetchMore : query.Execute;
        Task<QueryResult> running = Task.Run(operation);
        await socket.WaitForSendCountAsync(count + 1);
        socket.EnqueueReceiveError(new WebSocketException("scripted receive failure"));
        await Assert.ThrowsExceptionAsync<WebSocketException>(async () =>
            await running.WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.IsFalse(IsRegistered(query));
    }

    /// <summary>Verifies that a failed close still unregisters the query.</summary>
    [TestMethod]
    public async Task FailedCloseReleasesQueryAsync()
    {
        SqlJob job = NewJob(out ScriptedWebSocket socket);
        Query query = NewQuery(job);
        QueryResult first = await ReplyAsync(socket, query.Execute, done: false);
        socket.FailNextSend(new WebSocketException("scripted close failure"));

        await Assert.ThrowsExceptionAsync<WebSocketException>(async () =>
            await Task.Run(query.Close).WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.IsFalse(IsRegistered(query));
        Assert.IsNull(Query.ById(first.Id));
        Assert.IsNull(query.Close());
    }

    /// <summary>Verifies that explicit close uses the original continuation ID once.</summary>
    [TestMethod]
    public async Task ClosingActiveCursorSendsOneCloseWithOriginalCursorIdAsync()
    {
        SqlJob job = NewJob(out ScriptedWebSocket socket);
        Query query = NewQuery(job);
        QueryResult first = await ReplyAsync(socket, query.Execute, done: false);
        Task<string> closing = Task.Run(query.Close);
        await socket.WaitForSendCountAsync(2);
        JsonElement request = ReadRequest(socket);
        Assert.AreEqual("sqlclose", request.GetProperty("type").GetString());
        Assert.AreEqual(first.Id, request.GetProperty("cont_id").GetString());
        socket.EnqueueText(JsonSerializer.Serialize(new { id = request.GetProperty("id").GetString(), success = true }));
        await closing.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsNull(query.Close());
        Assert.AreEqual(2, socket.SentMessages.Count);
        Assert.IsFalse(IsRegistered(query));
    }

    /// <summary>Verifies that cleanup removes terminal entries and preserves queries that have not finished.</summary>
    [TestMethod]
    public void CleanupRemovesOnlyTerminalQueriesFromAllJobs()
    {
        Query completed = NewQuery(new SqlJob());
        Query failed = NewQuery(new SqlJob());
        Query unexecuted = NewQuery(new SqlJob());
        Query paged = NewQuery(new SqlJob());
        // Reproduce entries left by older execution paths, without requiring a server.
        SetState(completed, QueryState.RUN_DONE);
        SetState(failed, QueryState.ERROR);
        SetState(paged, QueryState.RUN_MORE_DATA_AVAILABLE);

        completed.Cleanup();
        completed.Cleanup();

        Assert.IsFalse(IsRegistered(completed));
        Assert.IsFalse(IsRegistered(failed));
        Assert.IsTrue(IsRegistered(unexecuted));
        Assert.IsTrue(IsRegistered(paged));
    }

    /// <summary>Checks that input validation does not discard a query that can still be run or paged.</summary>
    [TestMethod]
    public async Task InvalidRowCountKeepsQueryAvailableForValidCallAsync()
    {
        SqlJob job = NewJob(out ScriptedWebSocket socket);
        Query query = NewQuery(job);
        Assert.ThrowsException<Exception>(() => query.Execute(0));
        Assert.IsTrue(IsRegistered(query));
        QueryResult first = await ReplyAsync(socket, query.Execute, done: false);

        Assert.ThrowsException<Exception>(() => query.FetchMore(0));
        Assert.AreSame(query, Query.ById(first.Id));
        await ReplyAsync(socket, query.FetchMore, done: true);
        Assert.IsFalse(IsRegistered(query));
    }

    /// <summary>Checks that a retry after an SQL error registers its replacement cursor exactly once.</summary>
    [TestMethod]
    public async Task RetryingFailedQueryRegistersItsNewCursorOnlyOnceAsync()
    {
        SqlJob job = NewJob(out ScriptedWebSocket socket);
        Query query = NewQuery(job);
        await Assert.ThrowsExceptionAsync<SqlException>(async () =>
            await ReplyAsync(socket, query.Execute, error: true));

        QueryResult retried = await ReplyAsync(socket, query.Execute, done: false);
        Assert.AreSame(query, Query.ById(retried.Id));
        CollectionAssert.AreEqual(new[] { retried.Id }, Query.GetOpenIds(job));

        await ReplyAsync(socket, query.FetchMore, done: true);
        Assert.IsFalse(IsRegistered(query));
    }

    /// <summary>Verifies that repeated executions leave no completed queries in the process-wide registry.</summary>
    [TestMethod]
    public async Task RepeatedCompletionsDoNotAccumulateRegistryEntriesAsync()
    {
        SqlJob job = NewJob(out ScriptedWebSocket socket);
        for (int i = 0; i < 100; i++)
        {
            Query query = NewQuery(job);
            await ReplyAsync(socket, query.Execute, done: true);
        }

        Assert.IsFalse(_queries.Any(IsRegistered), "Completed queries must not remain rooted by the registry.");
    }

    /// <summary>Creates a job backed by a scripted socket and tracks it for teardown.</summary>
    /// <param name="socket">The socket that records requests and supplies replies.</param>
    /// <returns>The scripted job.</returns>
    private SqlJob NewJob(out ScriptedWebSocket socket)
    {
        socket = new ScriptedWebSocket();
        var job = new SqlJob(socket);
        _jobs.Add(job);
        return job;
    }

    /// <summary>Creates a query and tracks it for teardown.</summary>
    /// <param name="job">The job that owns the query.</param>
    /// <param name="options">Optional execution settings.</param>
    /// <returns>The tracked query.</returns>
    private Query NewQuery(SqlJob job, QueryOptions? options = null)
    {
        Query query = job.Query("VALUES 1", options ?? new QueryOptions());
        _queries.Add(query);
        return query;
    }

    /// <summary>Checks the registry by reference under the same lock used by the SDK.</summary>
    /// <param name="query">The exact instance to look up.</param>
    /// <returns>Whether the query is registered.</returns>
    private static bool IsRegistered(Query query)
    {
        var registry = typeof(Query).GetField("GlobalQueryList", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as List<Query>;
        Assert.IsNotNull(registry, "The query registry should be available to the test.");
        lock (registry)
        {
            return registry.Any(entry => ReferenceEquals(entry, query));
        }
    }

    private static void SetState(Query query, QueryState state)
    {
        FieldInfo? field = typeof(Query).GetField("State", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, "The query state should be available to the test.");
        field.SetValue(query, state);
    }

    private sealed class ValueEqualQuery(SqlJob job) : Query(job, "VALUES 1", new QueryOptions())
    {
        public override bool Equals(object? other) => other is ValueEqualQuery;

        public override int GetHashCode() => 0;
    }

    /// <summary>Reads the most recent outbound request independently of the JSON document lifetime.</summary>
    /// <param name="socket">The socket that recorded the request.</param>
    /// <returns>The outbound JSON object.</returns>
    private static JsonElement ReadRequest(ScriptedWebSocket socket)
    {
        using JsonDocument document = JsonDocument.Parse(socket.SentMessages.Last());
        return document.RootElement.Clone();
    }

    /// <summary>Supplies a matching scripted reply after the operation sends its request.</summary>
    /// <param name="socket">The socket that receives the scripted response.</param>
    /// <param name="operation">The synchronous query operation to run.</param>
    /// <param name="done">Whether a successful reply is terminal.</param>
    /// <param name="error">Whether to return an SQL error.</param>
    /// <param name="invalidResult">Whether to send an invalid completion flag.</param>
    /// <returns>The result returned by the query operation.</returns>
    private static async Task<QueryResult> ReplyAsync(ScriptedWebSocket socket, Func<QueryResult> operation,
        bool done = false, bool error = false, bool invalidResult = false)
    {
        int count = socket.SentMessages.Count;
        Task<QueryResult> running = Task.Run(operation);
        await socket.WaitForSendCountAsync(count + 1);
        string? id = ReadRequest(socket).GetProperty("id").GetString();
        Assert.IsNotNull(id, "The request should have a correlation ID.");
        object reply = error
            ? new { id, success = false, error = "scripted SQL error", sql_rc = -204, sql_state = "42704" }
            : invalidResult
                ? new { id, success = true, is_done = "not a boolean" }
                : new { id, success = true, is_done = done, data = Array.Empty<object>() };
        socket.EnqueueText(JsonSerializer.Serialize(reply));
        return await running.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
