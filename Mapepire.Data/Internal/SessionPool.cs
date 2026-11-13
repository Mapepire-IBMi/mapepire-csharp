using System.Collections.Concurrent;

namespace Mapepire.Data.Internal;

/// <summary>
/// Keeps idle server sessions per connection string. Opening a Mapepire connection starts a
/// websocket and a server job, so reusing them matters for EF Core, which opens and closes the
/// connection for every operation.
/// </summary>
internal static class SessionPool
{
    // Shared by every thread in the process. The concurrent collections make each operation safe on its
    // own; the size check in Return is not atomic with the enqueue, so the pool can briefly exceed
    // Max Pool Size by the number of threads returning sessions at the same moment.
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<ServerSession>> s_pools = new();

    /// <summary>
    /// Takes an idle session for the connection string, or opens a new one. Sessions that are broken, or idle
    /// for longer than <see cref="MapepireConnectionStringBuilder.IdleValidationSeconds"/> and no longer
    /// answering, are discarded.
    /// </summary>
    /// <param name="connectionString">The connection string; it identifies the pool.</param>
    /// <param name="settings">The parsed connection string.</param>
    /// <returns>An open session.</returns>
    public static ServerSession Rent(string connectionString, MapepireConnectionStringBuilder settings)
    {
        if (settings.Pooling && s_pools.TryGetValue(connectionString, out var queue))
        {
            while (queue.TryDequeue(out var session))
            {
                if (session.IsBroken)
                {
                    session.Dispose();
                    continue;
                }

                var idle = DateTime.UtcNow - session.LastUsedUtc;
                if (idle.TotalSeconds > settings.IdleValidationSeconds && !session.IsAlive())
                {
                    session.Dispose();
                    continue;
                }

                return session;
            }
        }

        return ServerSession.Open(settings);
    }

    /// <summary>
    /// Returns a session to its pool. The session is closed instead when pooling is off, the session is broken,
    /// or the pool is full.
    /// </summary>
    /// <param name="connectionString">The connection string; it identifies the pool.</param>
    /// <param name="settings">The parsed connection string.</param>
    /// <param name="session">The session to return.</param>
    public static void Return(string connectionString, MapepireConnectionStringBuilder settings, ServerSession session)
    {
        if (!settings.Pooling || session.IsBroken)
        {
            session.Dispose();
            return;
        }

        var queue = s_pools.GetOrAdd(connectionString, _ => new ConcurrentQueue<ServerSession>());
        if (queue.Count >= settings.MaxPoolSize)
        {
            session.Dispose();
            return;
        }

        queue.Enqueue(session);
    }

    /// <summary>Closes the idle sessions of one connection string.</summary>
    /// <param name="connectionString">The connection string; it identifies the pool.</param>
    public static void Clear(string connectionString)
    {
        if (s_pools.TryRemove(connectionString, out var queue))
        {
            while (queue.TryDequeue(out var session))
            {
                session.Dispose();
            }
        }
    }

    /// <summary>Closes the idle sessions of every connection string matching <paramref name="predicate"/>.</summary>
    /// <param name="predicate">Selects the connection strings to clear.</param>
    public static void Clear(Func<MapepireConnectionStringBuilder, bool> predicate)
    {
        foreach (var connectionString in s_pools.Keys)
        {
            if (predicate(new MapepireConnectionStringBuilder(connectionString)))
            {
                Clear(connectionString);
            }
        }
    }

    /// <summary>Closes the idle sessions of every connection string.</summary>
    public static void ClearAll()
    {
        foreach (var connectionString in s_pools.Keys)
        {
            Clear(connectionString);
        }
    }
}
