using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace UnitTests;

/// <summary>A WebSocket test double with queued replies and captured requests.</summary>
internal sealed class ScriptedWebSocket : WebSocket
{
    private sealed record Frame(byte[] Data, bool EndOfMessage, WebSocketMessageType Type, Exception? Error = null);

    private readonly ConcurrentQueue<Frame> _frames = new();
    private readonly SemaphoreSlim _availableFrames = new(0);
    private readonly ConcurrentQueue<string> _sentMessages = new();
    private readonly SemaphoreSlim _sendCountChanged = new(0);
    private int _activeSends;
    private int _maximumConcurrentSends;
    private WebSocketState _state = WebSocketState.Open;
    private Exception? _nextSendFailure;

    public override WebSocketCloseStatus? CloseStatus => null;
    public override string? CloseStatusDescription => null;
    public override WebSocketState State => _state;
    public override string? SubProtocol => null;
    public int MaximumConcurrentSends => Volatile.Read(ref _maximumConcurrentSends);
    public IReadOnlyCollection<string> SentMessages => _sentMessages.ToArray();
    public bool IsTerminated => State is WebSocketState.Aborted or WebSocketState.Closed;

    /// <inheritdoc />
    public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        int active = Interlocked.Increment(ref _activeSends);
        UpdateMaximumConcurrentSends(active);
        try
        {
            await Task.Delay(20, cancellationToken);
            Exception? failure = Interlocked.Exchange(ref _nextSendFailure, null);
            if (failure is not null)
            {
                EnqueueReceiveError(failure);
                throw failure;
            }
            _sentMessages.Enqueue(Encoding.UTF8.GetString(buffer.AsSpan()));
            _sendCountChanged.Release();
        }
        finally
        {
            Interlocked.Decrement(ref _activeSends);
        }
    }

    /// <summary>Records the peak number of simultaneous sends for concurrency assertions.</summary>
    /// <param name="active">The number of active sends.</param>
    private void UpdateMaximumConcurrentSends(int active)
    {
        int observed;
        while (active > (observed = Volatile.Read(ref _maximumConcurrentSends))
            && Interlocked.CompareExchange(ref _maximumConcurrentSends, active, observed) != observed)
        {
        }
    }

    /// <summary>Waits for a specified request count before the test queues a reply.</summary>
    /// <param name="count">The total number of requests expected.</param>
    public async Task WaitForSendCountAsync(int count)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (_sentMessages.Count < count)
        {
            await _sendCountChanged.WaitAsync(timeout.Token);
        }
    }

    public void EnqueueText(string text) => EnqueueFragment(text, true);

    /// <summary>Signals the receiver after queuing a frame.</summary>
    /// <param name="frame">The frame to make available.</param>
    public void EnqueueFragment(string text, bool endOfMessage) => Enqueue(new Frame(Encoding.UTF8.GetBytes(text), endOfMessage, WebSocketMessageType.Text));

    public void EnqueueClose() => Enqueue(new Frame([], true, WebSocketMessageType.Close));

    public void EnqueueReceiveError(Exception error) => Enqueue(new Frame([], true, WebSocketMessageType.Text, error));

    public void FailNextSend(Exception error) => _nextSendFailure = error;

    private void Enqueue(Frame frame)
    {
        _frames.Enqueue(frame);
        _availableFrames.Release();
    }

    /// <inheritdoc />
    public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        await _availableFrames.WaitAsync(cancellationToken);
        if (!_frames.TryDequeue(out Frame? frame))
        {
            throw new InvalidOperationException("Frame semaphore lost synchronization.");
        }
        if (frame.Error is not null)
        {
            throw frame.Error;
        }
        if (frame.Type == WebSocketMessageType.Close)
        {
            _state = WebSocketState.CloseReceived;
            return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
        }

        int count = Math.Min(buffer.Count, frame.Data.Length);
        frame.Data.AsSpan(0, count).CopyTo(buffer.AsSpan());
        return new WebSocketReceiveResult(count, frame.Type, frame.EndOfMessage);
    }

    public override void Abort()
    {
        _state = WebSocketState.Aborted;
        EnqueueClose();
    }
    public override void Dispose()
    {
        _state = WebSocketState.Closed;
        EnqueueClose();
    }

    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) =>
        Task.FromException(new NotSupportedException());

    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) =>
        Task.FromException(new NotSupportedException());
}
