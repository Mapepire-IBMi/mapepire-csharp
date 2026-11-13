using System.Collections.Concurrent;

namespace Mapepire.Data.Internal;

/// <summary>
/// Keeps idle server sessions per connection string. Opening a Mapepire connection starts a
/// websocket and a server job, so reusing them matters for EF Core, which opens and closes the
/// connection for every operation.
/// </summary>
internal static class SessionPool
{
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<ServerSession>> Pools = new();

    public static ServerSession Rent(string key, MapepireConnectionStringBuilder settings)
    {
        if (settings.Pooling && Pools.TryGetValue(key, out var queue))
        {
            while (queue.TryDequeue(out var session))
            {
                if (session.IsBroken)
                {
                    session.Dispose();
                    continue;
                }

                var idle = DateTime.UtcNow - session.LastUsedUtc;
                if (idle.TotalSeconds > settings.IdleValidationSeconds && !session.Validate())
                {
                    session.Dispose();
                    continue;
                }

                return session;
            }
        }

        return ServerSession.Open(settings);
    }

    public static void Return(string key, MapepireConnectionStringBuilder settings, ServerSession session)
    {
        if (!settings.Pooling || session.IsBroken)
        {
            session.Dispose();
            return;
        }

        var queue = Pools.GetOrAdd(key, _ => new ConcurrentQueue<ServerSession>());
        if (queue.Count >= settings.MaxPoolSize)
        {
            session.Dispose();
            return;
        }

        queue.Enqueue(session);
    }

    public static void Clear(string key)
    {
        if (Pools.TryRemove(key, out var queue))
        {
            while (queue.TryDequeue(out var session))
            {
                session.Dispose();
            }
        }
    }

    public static void ClearAll()
    {
        foreach (var key in Pools.Keys)
        {
            Clear(key);
        }
    }
}
