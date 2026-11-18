using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace UnitTests;

internal sealed class ScriptedWebSocket : WebSocket
{
    private sealed record Frame(byte[] Data, bool EndOfMessage, WebSocketMessageType Type, Exception? Error = null);

    private readonly ConcurrentQueue<Frame> frames = new();
    private readonly SemaphoreSlim availableFrames = new(0);
    private readonly ConcurrentQueue<string> sentMessages = new();
    private readonly SemaphoreSlim sendCountChanged = new(0);
    private int activeSends;
    private int maximumConcurrentSends;
    private WebSocketState state = WebSocketState.Open;
    private Exception? nextSendFailure;

    public override WebSocketCloseStatus? CloseStatus => null;
    public override string? CloseStatusDescription => null;
    public override WebSocketState State => state;
    public override string? SubProtocol => null;
    public int MaximumConcurrentSends => Volatile.Read(ref maximumConcurrentSends);
    public IReadOnlyCollection<string> SentMessages => sentMessages.ToArray();
    public bool IsTerminated => State is WebSocketState.Aborted or WebSocketState.Closed;

    public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        int active = Interlocked.Increment(ref activeSends);
        UpdateMaximumConcurrentSends(active);
        try
        {
            await Task.Delay(20, cancellationToken);
            Exception? failure = Interlocked.Exchange(ref nextSendFailure, null);
            if (failure != null)
            {
                EnqueueReceiveError(failure);
                throw failure;
            }
            sentMessages.Enqueue(Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count));
            sendCountChanged.Release();
        }
        finally
        {
            Interlocked.Decrement(ref activeSends);
        }
    }

    private void UpdateMaximumConcurrentSends(int active)
    {
        int observed;
        while (active > (observed = Volatile.Read(ref maximumConcurrentSends))
            && Interlocked.CompareExchange(ref maximumConcurrentSends, active, observed) != observed)
        {
        }
    }

    public async Task WaitForSendCountAsync(int count)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (sentMessages.Count < count)
            await sendCountChanged.WaitAsync(timeout.Token);
    }

    public void EnqueueText(string text) => EnqueueFragment(text, true);

    public void EnqueueFragment(string text, bool endOfMessage) => Enqueue(new Frame(Encoding.UTF8.GetBytes(text), endOfMessage, WebSocketMessageType.Text));

    public void EnqueueClose() => Enqueue(new Frame([], true, WebSocketMessageType.Close));

    public void EnqueueReceiveError(Exception error) => Enqueue(new Frame([], true, WebSocketMessageType.Text, error));

    public void FailNextSend(Exception error) => nextSendFailure = error;

    private void Enqueue(Frame frame)
    {
        frames.Enqueue(frame);
        availableFrames.Release();
    }

    public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        await availableFrames.WaitAsync(cancellationToken);
        if (!frames.TryDequeue(out Frame? frame)) throw new InvalidOperationException("Frame semaphore lost synchronization.");
        if (frame.Error != null) throw frame.Error;
        if (frame.Type == WebSocketMessageType.Close)
        {
            state = WebSocketState.CloseReceived;
            return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
        }

        int count = Math.Min(buffer.Count, frame.Data.Length);
        Array.Copy(frame.Data, 0, buffer.Array!, buffer.Offset, count);
        return new WebSocketReceiveResult(count, frame.Type, frame.EndOfMessage);
    }

    public override void Abort()
    {
        state = WebSocketState.Aborted;
        EnqueueClose();
    }
    public override void Dispose()
    {
        state = WebSocketState.Closed;
        EnqueueClose();
    }

    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) =>
        Task.FromException(new NotSupportedException());

    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) =>
        Task.FromException(new NotSupportedException());
}
