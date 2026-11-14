using System.Buffers;
using System.Text;
using System.Text.Json;
using io.github.mapepire_ibmi;
using io.github.mapepire_ibmi.types;

namespace Mapepire.Data.Internal;

/// <summary>
/// A physical connection to the Mapepire server (one server job). Wraps <see cref="SqlJob"/> and
/// serializes access to its single websocket.
/// </summary>
internal sealed class ServerSession : IDisposable
{
    private readonly SqlJob _job;
    private readonly object _sync = new();
    private bool _broken;

    private ServerSession(SqlJob job, string? jobName, string? serverVersion)
    {
        _job = job;
        JobName = jobName;
        ServerVersion = serverVersion;
    }

    public string? JobName { get; }

    public string? ServerVersion { get; }

    public bool IsBroken => _broken;

    public DateTime LastUsedUtc { get; private set; } = DateTime.UtcNow;

    /// <summary>Connects to the Mapepire server and starts a server job.</summary>
    /// <param name="settings">The connection settings.</param>
    /// <returns>The open session.</returns>
    /// <exception cref="InvalidOperationException">The settings do not name a server.</exception>
    /// <exception cref="MapepireException">The server could not be reached or rejected the connection.</exception>
    public static ServerSession Open(MapepireConnectionStringBuilder settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Server))
        {
            throw new InvalidOperationException("The connection string does not specify a Server.");
        }

        var daemon = new DaemonServer
        {
            Host = settings.Server,
            Port = settings.Port,
            User = settings.UserId,
            Password = settings.Password,
            RejectUnauthorized = settings.RejectUnauthorized,
            Ca = settings.Ca,
        };

        var job = new SqlJob(new JDBCOptions(BuildJdbcOptions(settings)));
        ConnectionResult? result;
        try
        {
            result = job.Connect(daemon);
        }
        catch (Exception ex)
        {
            // The message names the server and port only; the daemon object carries the password.
            var inner = Unwrap(ex);
            throw new MapepireException(
                $"Unable to connect to the Mapepire server at {settings.Server}:{settings.Port}: {inner.Message}",
                innerException: inner);
        }

        string? version = null;
        try
        {
            version = job.GetVersion().Version;
        }
        catch
        {
            // The version is informational only; a connection that works without it is still usable.
        }

        return new ServerSession(job, result?.Job, version);
    }

    /// <summary>Runs a statement. Values in <paramref name="parameters"/> must already be wire-ready.</summary>
    /// <param name="sql">The statement, with positional parameter markers.</param>
    /// <param name="parameters">The parameter values in marker order, or null for none.</param>
    /// <param name="rows">The number of rows to return in the first page.</param>
    /// <returns>The first page of results, or the update count for other statements.</returns>
    /// <exception cref="MapepireException">Db2 reported an error, or communication failed.</exception>
    public ServerResult Execute(string sql, IReadOnlyList<object?>? parameters, int rows)
    {
        var id = SqlJob.GetNewUniqueId("query");
        var request = Serialize(writer =>
        {
            writer.WriteString("id", id);
            writer.WriteString("type", parameters is { Count: > 0 } ? "prepare_sql_execute" : "sql");
            writer.WriteString("sql", sql);
            writer.WriteBoolean("terse", true);
            writer.WriteNumber("rows", Math.Max(rows, 1));
            if (parameters is { Count: > 0 })
            {
                writer.WritePropertyName("parameters");
                writer.WriteStartArray();
                foreach (var parameter in parameters)
                {
                    WireValue.Write(writer, parameter);
                }

                writer.WriteEndArray();
            }
        });

        var result = ServerResult.Parse(Send(request));
        if (!result.IsSuccess)
        {
            throw result.ToException("Failed to run statement.");
        }

        return result;
    }

    /// <summary>Fetches the next page of rows from an open cursor.</summary>
    /// <param name="continuationId">The id of the request that opened the cursor.</param>
    /// <param name="sql">The statement that opened the cursor.</param>
    /// <param name="rows">The number of rows to return.</param>
    /// <param name="columns">The columns of the first page; <c>sqlmore</c> replies carry no metadata.</param>
    /// <returns>The next page.</returns>
    /// <exception cref="MapepireException">Db2 reported an error, or communication failed.</exception>
    public ServerResult FetchMore(string continuationId, string sql, int rows, IReadOnlyList<ServerColumn>? columns)
    {
        var request = Serialize(writer =>
        {
            writer.WriteString("id", SqlJob.GetNewUniqueId("fetchMore"));
            writer.WriteString("cont_id", continuationId);
            writer.WriteString("type", "sqlmore");
            writer.WriteString("sql", sql);
            writer.WriteNumber("rows", Math.Max(rows, 1));
        });

        var result = ServerResult.Parse(Send(request), columns);
        if (!result.IsSuccess)
        {
            throw result.ToException("Failed to fetch more rows.");
        }

        return result;
    }

    /// <summary>Closes an open cursor on the server.</summary>
    /// <param name="continuationId">The id of the request that opened the cursor.</param>
    public void CloseCursor(string continuationId)
    {
        var request = Serialize(writer =>
        {
            writer.WriteString("id", SqlJob.GetNewUniqueId("sqlclose"));
            writer.WriteString("cont_id", continuationId);
            writer.WriteString("type", "sqlclose");
        });

        Send(request);
    }

    /// <summary>Checks with a round trip that the server still answers. A session that does not is marked broken.</summary>
    /// <returns>True if the server answered.</returns>
    public bool IsAlive()
    {
        try
        {
            lock (_sync)
            {
                _job.GetVersion();
            }

            LastUsedUtc = DateTime.UtcNow;
            return true;
        }
        catch
        {
            // Any failure means the session cannot be reused; the pool discards it.
            _broken = true;
            return false;
        }
    }

    /// <summary>Ends the session and closes its websocket.</summary>
    public void Dispose()
    {
        _broken = true;
        try
        {
            _job.Close();
        }
        catch
        {
            // Closing is best effort; the server ends the job when the websocket goes away.
        }
    }

    /// <summary>Maps connection settings onto the JDBC properties understood by the Mapepire server.</summary>
    /// <param name="settings">The connection settings.</param>
    /// <returns>The JDBC properties.</returns>
    internal static Dictionary<object, object> BuildJdbcOptions(MapepireConnectionStringBuilder settings)
    {
        // Values are passed as strings: the SDK flattens them into "key=value;key=value".
        var options = new Dictionary<object, object>
        {
            ["naming"] = settings.Naming.ToLowerInvariant(),
            ["date format"] = settings.DateFormat.ToLowerInvariant(),
            ["time format"] = settings.TimeFormat.ToLowerInvariant(),
            ["auto commit"] = settings.AutoCommit ? "true" : "false",
        };

        var libraries = settings.GetLibraryList();
        if (libraries.Count > 0)
        {
            options["libraries"] = string.Join(",", libraries);
        }

        if (!string.IsNullOrWhiteSpace(settings.TransactionIsolation))
        {
            options["transaction isolation"] = settings.TransactionIsolation.Trim().ToLowerInvariant();
        }

        return options;
    }

    /// <summary>Builds a JSON request object.</summary>
    /// <param name="body">Writes the request's properties.</param>
    /// <returns>The request text.</returns>
    private static string Serialize(Action<Utf8JsonWriter> body)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            body(writer);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    // SqlJob blocks on Task.Wait(), which wraps failures in an AggregateException.
    private static Exception Unwrap(Exception exception)
        => exception is AggregateException { InnerExceptions.Count: 1 } aggregate ? aggregate.InnerExceptions[0] : exception;

    /// <summary>Sends a request and waits for the reply. A communication failure marks the session broken.</summary>
    /// <param name="request">The request text.</param>
    /// <returns>The reply text.</returns>
    /// <exception cref="MapepireException">The session is broken, or communication failed.</exception>
    private string Send(string request)
    {
        if (_broken)
        {
            throw new MapepireException("The connection to the Mapepire server is broken.");
        }

        lock (_sync)
        {
            try
            {
                var response = _job.Send(request);
                LastUsedUtc = DateTime.UtcNow;
                return response;
            }
            catch (Exception ex)
            {
                _broken = true;
                var inner = Unwrap(ex);
                throw new MapepireException("Communication with the Mapepire server failed: " + inner.Message, innerException: inner);
            }
        }
    }
}
