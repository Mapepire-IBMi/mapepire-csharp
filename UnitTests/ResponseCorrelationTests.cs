using System.Net.WebSockets;
using System.Text.Json;
using io.github.mapepire_ibmi;

namespace UnitTests;

[TestClass]
public sealed class ResponseCorrelationTests
{
    [TestMethod]
    public async Task ConcurrentSendsReceiveTheirMatchingResponsesWhenRepliesAreReversed()
    {
        var socket = new ScriptedWebSocket();
        var job = new SqlJob(socket);

        Task<string> first = Task.Run(() => job.Send("{\"id\":\"first\",\"type\":\"test\"}"));
        Task<string> second = Task.Run(() => job.Send("{\"id\":\"second\",\"type\":\"test\"}"));
        await socket.WaitForSendCountAsync(2);

        socket.EnqueueText("{\"id\":\"second\",\"result\":2}");
        socket.EnqueueText("{\"id\":\"first\",\"result\":1}");

        Assert.AreEqual("second", ReadId(await second.WaitAsync(TimeSpan.FromSeconds(5))));
        Assert.AreEqual("first", ReadId(await first.WaitAsync(TimeSpan.FromSeconds(5))));
        Assert.AreEqual(1, socket.MaximumConcurrentSends, "WebSocket sends must be serialized.");
    }

    [TestMethod]
    public async Task UnknownResponseIdDoesNotPreventMatchingResponse()
    {
        var socket = new ScriptedWebSocket();
        var job = new SqlJob(socket);
        Task<string> send = Task.Run(() => job.Send("{\"id\":\"expected\",\"type\":\"test\"}"));
        await socket.WaitForSendCountAsync(1);

        socket.EnqueueText("{\"id\":\"unrecognized\",\"result\":0}");
        socket.EnqueueText("{\"id\":\"expected\",\"result\":1}");

        Assert.AreEqual("expected", ReadId(await send.WaitAsync(TimeSpan.FromSeconds(5))));
    }

    [TestMethod]
    public async Task FetchResponseCorrelatesByOperationIdRatherThanContinuationId()
    {
        var socket = new ScriptedWebSocket();
        var job = new SqlJob(socket);
        Task<string> send = Task.Run(() => job.Send("{\"id\":\"fetch-op\",\"cont_id\":\"cursor-original\",\"type\":\"sqlmore\"}"));
        await socket.WaitForSendCountAsync(1);

        socket.EnqueueText("{\"id\":\"cursor-original\",\"result\":0}");
        Task completedAfterCursorId = await Task.WhenAny(send, Task.Delay(TimeSpan.FromMilliseconds(100)));
        Assert.AreNotSame(send, completedAfterCursorId, "A continuation ID must not complete the request.");

        socket.EnqueueText("{\"id\":\"fetch-op\",\"result\":1}");
        Assert.AreEqual("fetch-op", ReadId(await send.WaitAsync(TimeSpan.FromSeconds(5))));
    }

    [TestMethod]
    public async Task CloseFailsPendingSendLocally()
    {
        var socket = new ScriptedWebSocket();
        var job = new SqlJob(socket);
        Task<string> send = Task.Run(() => job.Send("{\"id\":\"pending\",\"type\":\"test\"}"));
        await socket.WaitForSendCountAsync(1);

        job.Close();

        await Assert.ThrowsExceptionAsync<ObjectDisposedException>(async () => await send.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [TestMethod]
    public async Task DuplicatePendingRequestIdIsRejected()
    {
        var socket = new ScriptedWebSocket();
        var job = new SqlJob(socket);
        Task<string> first = Task.Run(() => job.Send("{\"id\":\"duplicate\",\"type\":\"test\"}"));
        await socket.WaitForSendCountAsync(1);

        Task second = Task.Run(() => job.Send("{\"id\":\"duplicate\",\"type\":\"test\"}"));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await second.WaitAsync(TimeSpan.FromSeconds(5)));

        socket.EnqueueText("{\"id\":\"duplicate\",\"result\":1}");
        Assert.AreEqual("duplicate", ReadId(await first.WaitAsync(TimeSpan.FromSeconds(5))));
        Assert.AreEqual(1, socket.SentMessages.Count);
    }

    [TestMethod]
    public async Task SendAssemblesFragmentedResponse()
    {
        var socket = new ScriptedWebSocket();
        var job = new SqlJob(socket);
        Task<string> send = Task.Run(() => job.Send("{\"id\":\"fragmented\",\"type\":\"test\"}"));
        await socket.WaitForSendCountAsync(1);

        socket.EnqueueFragment("{\"id\":\"frag", endOfMessage: false);
        socket.EnqueueFragment("mented\",\"result\":true}", endOfMessage: true);

        Assert.AreEqual("fragmented", ReadId(await send.WaitAsync(TimeSpan.FromSeconds(5))));
    }

    [TestMethod]
    public async Task DisconnectFailsPendingSends()
    {
        var socket = new ScriptedWebSocket();
        var job = new SqlJob(socket);
        Task<string> first = Task.Run(() => job.Send("{\"id\":\"first\",\"type\":\"test\"}"));
        Task<string> second = Task.Run(() => job.Send("{\"id\":\"second\",\"type\":\"test\"}"));
        await socket.WaitForSendCountAsync(2);
        socket.EnqueueClose();

        await Assert.ThrowsExceptionAsync<WebSocketException>(async () => await first.WaitAsync(TimeSpan.FromSeconds(5)));
        await Assert.ThrowsExceptionAsync<WebSocketException>(async () => await second.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [TestMethod]
    public async Task WebSocketReceiveErrorFailsPendingSend()
    {
        var socket = new ScriptedWebSocket();
        var job = new SqlJob(socket);
        Task<string> send = Task.Run(() => job.Send("{\"id\":\"error\",\"type\":\"test\"}"));
        await socket.WaitForSendCountAsync(1);
        socket.EnqueueReceiveError(new WebSocketException("scripted receive failure"));

        await Assert.ThrowsExceptionAsync<WebSocketException>(async () => await send.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [TestMethod]
    public async Task WebSocketSendFailureSettlesPendingSend()
    {
        var socket = new ScriptedWebSocket();
        socket.FailNextSend(new WebSocketException("scripted send failure"));
        var job = new SqlJob(socket);
        Task<string> send = Task.Run(() => job.Send("{\"id\":\"send-error\",\"type\":\"test\"}"));

        await Assert.ThrowsExceptionAsync<WebSocketException>(async () => await send.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(socket.IsTerminated, "A failed send must abort or dispose its socket to release the receiver.");

        Task<string> subsequentSend = Task.Run(() => job.Send("{\"id\":\"after-error\",\"type\":\"test\"}"));
        await Assert.ThrowsExceptionAsync<WebSocketException>(async () => await subsequentSend.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    private static string? ReadId(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("id").GetString();
    }

}
