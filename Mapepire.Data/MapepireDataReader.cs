using System.Collections;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Mapepire.Data.Internal;

namespace Mapepire.Data;

/// <summary>
/// Reads rows returned by a <see cref="MapepireCommand"/>. Rows are fetched from the server in blocks
/// of <see cref="MapepireCommand.FetchSize"/> as the reader advances.
/// </summary>
/// <remarks>
/// While rows remain unread, the reader holds a cursor open on the server. Closing or disposing the reader
/// closes the cursor.
/// </remarks>
public sealed class MapepireDataReader : DbDataReader
{
    private readonly MapepireConnection _connection;
    private readonly string _sql;
    private readonly int _fetchSize;
    private readonly CommandBehavior _behavior;
    private readonly IReadOnlyList<ServerColumn> _columns;
    private readonly Db2Type[] _types;
    private readonly int _recordsAffected;
    private readonly string? _continuationId;
    private bool _serverDone;
    private List<JsonElement[]> _page;
    private int _pageIndex = -1;
    private JsonElement[]? _current;
    private bool _closed;
    private bool _readAny;
    private Dictionary<string, int>? _ordinals;

    /// <summary>Creates a reader over the first page of a result.</summary>
    /// <param name="connection">The connection that ran the statement; later pages are fetched through it.</param>
    /// <param name="result">The server's reply to the statement.</param>
    /// <param name="sql">The statement, which the server needs again to fetch more rows.</param>
    /// <param name="fetchSize">Rows to request per round trip.</param>
    /// <param name="behavior">The command behavior the reader was created with.</param>
    internal MapepireDataReader(
        MapepireConnection connection,
        ServerResult result,
        string sql,
        int fetchSize,
        CommandBehavior behavior)
    {
        _connection = connection;
        _sql = sql;
        _fetchSize = fetchSize;
        _behavior = behavior;
        _columns = result.Columns ?? [];
        _types = _columns.Select(column => Db2Type.FromName(column.TypeName)).ToArray();
        _recordsAffected = _columns.Count > 0 ? -1 : result.UpdateCount;
        _page = result.Rows;
        _serverDone = result.IsDone;
        _continuationId = result.Id;
    }

    /// <inheritdoc />
    public override int Depth => 0;

    /// <inheritdoc />
    public override int FieldCount => _columns.Count;

    /// <inheritdoc />
    public override bool HasRows => _readAny || _page.Count > 0;

    /// <inheritdoc />
    public override bool IsClosed => _closed;

    /// <inheritdoc />
    public override int RecordsAffected => _recordsAffected;

    /// <inheritdoc />
    public override object this[int ordinal] => GetValue(ordinal);

    /// <inheritdoc />
    public override object this[string name] => GetValue(GetOrdinal(name));

    /// <inheritdoc />
    public override bool Read()
    {
        if (_closed)
        {
            throw new InvalidOperationException("The reader is closed.");
        }

        if (_behavior.HasFlag(CommandBehavior.SingleRow) && _readAny && _current is not null)
        {
            _current = null;
            return false;
        }

        while (true)
        {
            if (_pageIndex + 1 < _page.Count)
            {
                _pageIndex++;
                _current = _page[_pageIndex];
                _readAny = true;
                return true;
            }

            if (_serverDone || _continuationId is null || _columns.Count == 0)
            {
                _current = null;
                return false;
            }

            var more = _connection.Session.FetchMore(_continuationId, _sql, _fetchSize, _columns);
            _page = more.Rows;
            _pageIndex = -1;
            _serverDone = more.IsDone;
            if (_page.Count == 0 && !_serverDone)
            {
                // Defensive: an empty page that is not the end would loop forever.
                _serverDone = true;
            }
        }
    }

    /// <inheritdoc />
    public override Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Rows already buffered do not need a round trip.
        if (!_closed && _pageIndex + 1 < _page.Count && !_behavior.HasFlag(CommandBehavior.SingleRow))
        {
            return Task.FromResult(Read());
        }

        return Task.Run(Read, cancellationToken);
    }

    /// <inheritdoc />
    public override bool NextResult()
    {
        _current = null;
        _page = [];
        _pageIndex = -1;
        CloseServerCursor();
        return false;
    }

    /// <inheritdoc />
    public override void Close()
    {
        if (_closed)
        {
            return;
        }

        _closed = true;
        _current = null;
        try
        {
            CloseServerCursor();
            if (_connection.State == ConnectionState.Open)
            {
                _connection.OnCommandCompleted();
            }
        }
        finally
        {
            if (_behavior.HasFlag(CommandBehavior.CloseConnection))
            {
                _connection.Close();
            }
        }
    }

    /// <inheritdoc />
    public override object GetValue(int ordinal) => WireValue.ToClr(GetRawValue(ordinal), _types[ordinal]);

    /// <inheritdoc />
    public override int GetValues(object[] values)
    {
        var count = Math.Min(values.Length, FieldCount);
        for (var i = 0; i < count; i++)
        {
            values[i] = GetValue(i);
        }

        return count;
    }

    /// <inheritdoc />
    public override bool IsDBNull(int ordinal) => GetRawValue(ordinal).ValueKind is JsonValueKind.Null or JsonValueKind.Undefined;

    /// <inheritdoc />
    public override string GetName(int ordinal) => _columns[ordinal].Name;

    /// <inheritdoc />
    public override int GetOrdinal(string name)
    {
        if (_ordinals is null)
        {
            _ordinals = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < _columns.Count; i++)
            {
                _ordinals.TryAdd(_columns[i].Name, i);
            }
        }

        if (_ordinals.TryGetValue(name, out var ordinal))
        {
            return ordinal;
        }

        for (var i = 0; i < _columns.Count; i++)
        {
            if (string.Equals(_columns[i].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new IndexOutOfRangeException($"Column '{name}' not found.");
    }

    /// <inheritdoc />
    public override string GetDataTypeName(int ordinal) => _columns[ordinal].TypeName;

    /// <inheritdoc />
    public override Type GetFieldType(int ordinal) => _types[ordinal].ClrType;

    /// <inheritdoc />
    public override T GetFieldValue<T>(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value is DBNull)
        {
            if (default(T) is null)
            {
                // T is a reference or nullable type, so null is a valid result.
                return default!;
            }

            throw new InvalidCastException($"Column '{GetName(ordinal)}' is null.");
        }

        if (value is T typed)
        {
            return typed;
        }

        var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T)ConvertTo(value, target);
    }

    /// <inheritdoc />
    public override bool GetBoolean(int ordinal) => GetFieldValue<bool>(ordinal);

    /// <inheritdoc />
    public override byte GetByte(int ordinal) => GetFieldValue<byte>(ordinal);

    /// <inheritdoc />
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        var bytes = GetFieldValue<byte[]>(ordinal);
        if (buffer is null)
        {
            return bytes.Length;
        }

        var count = (int)Math.Max(0, Math.Min(length, bytes.Length - dataOffset));
        Array.Copy(bytes, dataOffset, buffer, bufferOffset, count);
        return count;
    }

    /// <inheritdoc />
    public override char GetChar(int ordinal) => GetFieldValue<char>(ordinal);

    /// <inheritdoc />
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        var text = GetString(ordinal);
        if (buffer is null)
        {
            return text.Length;
        }

        var count = (int)Math.Max(0, Math.Min(length, text.Length - dataOffset));
        text.CopyTo((int)dataOffset, buffer, bufferOffset, count);
        return count;
    }

    /// <inheritdoc />
    public override DateTime GetDateTime(int ordinal) => GetFieldValue<DateTime>(ordinal);

    /// <inheritdoc />
    public override decimal GetDecimal(int ordinal) => GetFieldValue<decimal>(ordinal);

    /// <inheritdoc />
    public override double GetDouble(int ordinal) => GetFieldValue<double>(ordinal);

    /// <inheritdoc />
    public override float GetFloat(int ordinal) => GetFieldValue<float>(ordinal);

    /// <inheritdoc />
    public override Guid GetGuid(int ordinal) => GetFieldValue<Guid>(ordinal);

    /// <inheritdoc />
    public override short GetInt16(int ordinal) => GetFieldValue<short>(ordinal);

    /// <inheritdoc />
    public override int GetInt32(int ordinal) => GetFieldValue<int>(ordinal);

    /// <inheritdoc />
    public override long GetInt64(int ordinal) => GetFieldValue<long>(ordinal);

    /// <inheritdoc />
    public override string GetString(int ordinal) => GetFieldValue<string>(ordinal);

    /// <summary>Gets the value of a TIME column.</summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    public TimeSpan GetTimeSpan(int ordinal) => GetFieldValue<TimeSpan>(ordinal);

    /// <inheritdoc />
    public override IEnumerator GetEnumerator() => new DbEnumerator(this, _behavior.HasFlag(CommandBehavior.CloseConnection));

    /// <inheritdoc />
    public override DataTable GetSchemaTable()
    {
        var table = new DataTable("SchemaTable");
        table.Columns.Add(SchemaTableColumn.ColumnName, typeof(string));
        table.Columns.Add(SchemaTableColumn.ColumnOrdinal, typeof(int));
        table.Columns.Add(SchemaTableColumn.ColumnSize, typeof(int));
        table.Columns.Add(SchemaTableColumn.NumericPrecision, typeof(int));
        table.Columns.Add(SchemaTableColumn.NumericScale, typeof(int));
        table.Columns.Add(SchemaTableColumn.DataType, typeof(Type));
        table.Columns.Add("DataTypeName", typeof(string));
        table.Columns.Add(SchemaTableColumn.AllowDBNull, typeof(bool));
        table.Columns.Add(SchemaTableColumn.ProviderType, typeof(int));

        for (var i = 0; i < _columns.Count; i++)
        {
            var column = _columns[i];
            table.Rows.Add(column.Name, i, column.DisplaySize, column.Precision, column.Scale, _types[i].ClrType, column.TypeName, true, (int)_types[i].DbType);
        }

        return table;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Close();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Converts a column value to a type other than its natural CLR type, for example DATE to
    /// <see cref="DateOnly"/>, CHAR(36) to <see cref="Guid"/>, or SMALLINT to <see cref="bool"/>.
    /// </summary>
    /// <param name="value">The non-null column value.</param>
    /// <param name="target">The requested type, without any <see cref="Nullable{T}"/> wrapper.</param>
    /// <returns>The converted value.</returns>
    private static object ConvertTo(object value, Type target)
    {
        // Column values are never null here, and their ToString() always returns text, so the "!" below is safe.
        if (target == typeof(DateOnly))
        {
            return DateOnly.FromDateTime(value is DateTime dateTime ? dateTime : WireValue.ParseDate(value.ToString()!));
        }

        if (target == typeof(TimeOnly))
        {
            return value switch
            {
                TimeSpan timeSpan => TimeOnly.FromTimeSpan(timeSpan),
                DateTime dateTime => TimeOnly.FromDateTime(dateTime),
                _ => TimeOnly.FromTimeSpan(WireValue.ParseTime(value.ToString()!)),
            };
        }

        if (target == typeof(TimeSpan))
        {
            return value is DateTime dateTime ? dateTime.TimeOfDay : WireValue.ParseTime(value.ToString()!);
        }

        if (target == typeof(DateTimeOffset))
        {
            var dateTime = value is DateTime timestamp ? timestamp : WireValue.ParseTimestamp(value.ToString()!);
            return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
        }

        if (target == typeof(DateTime) && value is string timestampText)
        {
            return WireValue.ParseTimestamp(timestampText);
        }

        if (target == typeof(Guid))
        {
            return value is byte[] bytes ? new Guid(bytes) : Guid.Parse(value.ToString()!.Trim());
        }

        if (target == typeof(bool))
        {
            return value switch
            {
                string text => text.Trim() is "1" or "Y" or "y" || bool.Parse(text.Trim()),
                _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture) != 0,
            };
        }

        if (target == typeof(char))
        {
            var text = value.ToString()!;
            return text.Length > 0 ? text[0] : '\0';
        }

        if (target.IsEnum)
        {
            return Enum.ToObject(target, Convert.ChangeType(value, Enum.GetUnderlyingType(target), CultureInfo.InvariantCulture));
        }

        return Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
    }

    /// <summary>Closes the server cursor if rows remain unread. Safe to call more than once.</summary>
    private void CloseServerCursor()
    {
        if (!_serverDone && _continuationId is not null && _connection.State == ConnectionState.Open)
        {
            _serverDone = true;
            try
            {
                _connection.Session.CloseCursor(_continuationId);
            }
            catch (MapepireException)
            {
                // The server may already have closed the cursor; there is nothing left to release.
            }
        }

        _serverDone = true;
    }

    /// <summary>Gets the JSON value of a column in the current row.</summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value; a value missing from the row is undefined, which reads as null.</returns>
    /// <exception cref="InvalidOperationException">There is no current row.</exception>
    /// <exception cref="IndexOutOfRangeException">The ordinal is out of range.</exception>
    private JsonElement GetRawValue(int ordinal)
    {
        var row = _current ?? throw new InvalidOperationException("No current row. Call Read() first.");
        if ((uint)ordinal >= (uint)_columns.Count)
        {
            throw new IndexOutOfRangeException($"Column ordinal {ordinal} is out of range.");
        }

        return ordinal < row.Length ? row[ordinal] : default;
    }
}
