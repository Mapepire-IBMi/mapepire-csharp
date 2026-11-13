using System.Text.Json;

namespace Mapepire.Data.Internal;

/// <summary>Column description returned by the Mapepire server.</summary>
internal sealed record ServerColumn(string Name, string Label, string TypeName, int Precision, int Scale, int DisplaySize);

/// <summary>Output parameter returned by the Mapepire server (1-based index).</summary>
internal sealed record ServerOutputParameter(int Index, string? Name, string? TypeName, JsonElement Value);

/// <summary>
/// A parsed response to an <c>sql</c>, <c>prepare_sql_execute</c> or <c>sqlmore</c> request.
/// Row values are kept as <see cref="JsonElement"/> and converted lazily by the data reader.
/// </summary>
internal sealed class ServerResult
{
    public string? Id { get; private init; }
    public bool Success { get; private init; }
    public string? Error { get; private init; }
    public int SqlCode { get; private init; }
    public string? SqlState { get; private init; }
    public bool IsDone { get; private init; }
    public bool HasResults { get; private init; }
    public int UpdateCount { get; private init; }
    public IReadOnlyList<ServerColumn>? Columns { get; private init; }
    public List<JsonElement[]> Rows { get; private init; } = [];
    public IReadOnlyList<ServerOutputParameter> OutputParameters { get; private init; } = [];

    public static ServerResult Parse(string json, IReadOnlyList<ServerColumn>? knownColumns = null)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        List<ServerColumn>? columns = null;
        if (root.TryGetProperty("metadata", out var metadata) && metadata.ValueKind == JsonValueKind.Object
            && metadata.TryGetProperty("columns", out var cols) && cols.ValueKind == JsonValueKind.Array)
        {
            columns = [];
            foreach (var c in cols.EnumerateArray())
            {
                var name = GetString(c, "name") ?? GetString(c, "label") ?? $"COL{columns.Count + 1}";
                columns.Add(new ServerColumn(
                    name,
                    GetString(c, "label") ?? name,
                    (GetString(c, "type") ?? "VARCHAR").Trim().ToUpperInvariant(),
                    GetInt(c, "precision"),
                    GetInt(c, "scale"),
                    GetInt(c, "display_size")));
            }
        }

        IReadOnlyList<ServerColumn>? effectiveColumns = columns ?? knownColumns;

        var rows = new List<JsonElement[]>();
        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in data.EnumerateArray())
            {
                rows.Add(ParseRow(row, effectiveColumns));
            }
        }

        var outputs = new List<ServerOutputParameter>();
        if (root.TryGetProperty("output_parms", out var parms) && parms.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in parms.EnumerateArray())
            {
                outputs.Add(new ServerOutputParameter(
                    GetInt(p, "index"),
                    GetString(p, "name"),
                    GetString(p, "type"),
                    p.TryGetProperty("value", out var v) ? v.Clone() : default));
            }
        }

        return new ServerResult
        {
            Id = GetString(root, "id"),
            Success = root.TryGetProperty("success", out var s) && s.ValueKind == JsonValueKind.True,
            Error = GetString(root, "error"),
            SqlCode = GetInt(root, "sql_rc"),
            SqlState = GetString(root, "sql_state"),
            IsDone = !root.TryGetProperty("is_done", out var d) || d.ValueKind != JsonValueKind.False,
            HasResults = root.TryGetProperty("has_results", out var h) && h.ValueKind == JsonValueKind.True,
            UpdateCount = root.TryGetProperty("update_count", out var u) && u.ValueKind == JsonValueKind.Number ? u.GetInt32() : -1,
            Columns = effectiveColumns,
            Rows = rows,
            OutputParameters = outputs,
        };
    }

    private static JsonElement[] ParseRow(JsonElement row, IReadOnlyList<ServerColumn>? columns)
    {
        if (row.ValueKind == JsonValueKind.Array)
        {
            // Terse format: values are positional.
            var values = new JsonElement[row.GetArrayLength()];
            var i = 0;
            foreach (var v in row.EnumerateArray())
            {
                values[i++] = v.Clone();
            }

            return values;
        }

        if (row.ValueKind == JsonValueKind.Object)
        {
            // Non-terse format: values keyed by column name. Properties are emitted in column order,
            // so use positional order and fall back to a name lookup if the counts do not match.
            var props = row.EnumerateObject().ToList();
            var count = columns?.Count ?? props.Count;
            var values = new JsonElement[count];
            if (columns == null || props.Count == columns.Count)
            {
                for (var i = 0; i < props.Count; i++)
                {
                    values[i] = props[i].Value.Clone();
                }
            }
            else
            {
                for (var i = 0; i < columns.Count; i++)
                {
                    values[i] = row.TryGetProperty(columns[i].Name, out var v) ? v.Clone() : default;
                }
            }

            return values;
        }

        return [];
    }

    private static string? GetString(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int GetInt(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : 0;

    public MapepireException ToException(string fallbackMessage)
        => new(Error ?? fallbackMessage, SqlCode, SqlState);
}
