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
            var inner = ex is AggregateException { InnerExceptions.Count: 1 } agg ? agg.InnerException! : ex;
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
            // Version is informational only.
        }

        return new ServerSession(job, result?.Job, version);
    }

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

    /// <summary>Runs a statement. Values in <paramref name="parameters"/> must already be wire-ready.</summary>
    public ServerResult Execute(string sql, IReadOnlyList<object?>? parameters, int rows)
    {
        var id = SqlJob.GetNewUniqueId("query");
        var request = Serialize(w =>
        {
            w.WriteString("id", id);
            w.WriteString("type", parameters is { Count: > 0 } ? "prepare_sql_execute" : "sql");
            w.WriteString("sql", sql);
            w.WriteBoolean("terse", true);
            w.WriteNumber("rows", Math.Max(rows, 1));
            if (parameters is { Count: > 0 })
            {
                w.WritePropertyName("parameters");
                w.WriteStartArray();
                foreach (var p in parameters)
                {
                    WireValue.Write(w, p);
                }

                w.WriteEndArray();
            }
        });

        var result = ServerResult.Parse(Send(request));
        if (!result.Success)
        {
            throw result.ToException("Failed to run statement.");
        }

        return result;
    }

    public ServerResult FetchMore(string continuationId, string sql, int rows, IReadOnlyList<ServerColumn>? columns)
    {
        var request = Serialize(w =>
        {
            w.WriteString("id", SqlJob.GetNewUniqueId("fetchMore"));
            w.WriteString("cont_id", continuationId);
            w.WriteString("type", "sqlmore");
            w.WriteString("sql", sql);
            w.WriteNumber("rows", Math.Max(rows, 1));
        });

        var result = ServerResult.Parse(Send(request), columns);
        if (!result.Success)
        {
            throw result.ToException("Failed to fetch more rows.");
        }

        return result;
    }

    public void CloseCursor(string continuationId)
    {
        var request = Serialize(w =>
        {
            w.WriteString("id", SqlJob.GetNewUniqueId("sqlclose"));
            w.WriteString("cont_id", continuationId);
            w.WriteString("type", "sqlclose");
        });

        Send(request);
    }

    /// <summary>Checks that the server still answers.</summary>
    public bool Validate()
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
            _broken = true;
            return false;
        }
    }

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
                var inner = ex is AggregateException { InnerExceptions.Count: 1 } agg ? agg.InnerException! : ex;
                throw new MapepireException("Communication with the Mapepire server failed: " + inner.Message, innerException: inner);
            }
        }
    }

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

    public void Dispose()
    {
        _broken = true;
        try
        {
            _job.Close();
        }
        catch
        {
            // Ignore errors while closing the websocket.
        }
    }
}
