using System.Data;
using System.Globalization;
using System.Text.Json;

namespace Mapepire.Data.Internal;

/// <summary>
/// Converts values between .NET and the JSON representation used by the Mapepire protocol.
/// </summary>
internal static class WireValue
{
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

    /// <summary>
    /// Converts a parameter value to a wire-ready value (string, number, bool or null).
    /// Values that would lose precision as JSON numbers (decimal, 64-bit integers) are sent as strings,
    /// which Db2 converts to the target column type.
    /// </summary>
    public static object? FromParameter(object? value, DbType? dbType)
    {
        if (value is null || value is DBNull)
        {
            return null;
        }

        var numericTarget = dbType is DbType.Int16 or DbType.Int32 or DbType.Int64 or DbType.Byte or DbType.SByte
            or DbType.UInt16 or DbType.UInt32 or DbType.UInt64 or DbType.Decimal or DbType.Double or DbType.Single
            or DbType.VarNumeric or DbType.Currency;

        return value switch
        {
            string s => s,
            char c => c.ToString(),
            bool b => dbType == DbType.Boolean ? b : (b ? 1 : 0),
            byte or sbyte or short or ushort or int => Convert.ToInt32(value, CultureInfo.InvariantCulture),
            uint or long or ulong => Convert.ToString(value, CultureInfo.InvariantCulture),
            float f => f,
            double d => d,
            decimal m => m.ToString(CultureInfo.InvariantCulture),
            DateTime dt => dbType == DbType.Date
                ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : dbType == DbType.Time
                    ? dt.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
                    : dt.ToString(TimestampFormat, CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly t => t.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            TimeSpan ts => FormatTime(ts),
            Guid g => g.ToString("D"),
            byte[] bytes => Convert.ToHexString(bytes),
            Enum e => numericTarget || dbType == null
                ? Convert.ToInt64(e, CultureInfo.InvariantCulture)
                : e.ToString(),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };
    }

    private static string FormatTime(TimeSpan ts)
    {
        if (ts < TimeSpan.Zero || ts >= TimeSpan.FromDays(1))
        {
            throw new ArgumentOutOfRangeException(nameof(ts), ts, "TIME values must be between 00:00:00 and 23:59:59.");
        }

        return ts.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }

    public static void Write(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case float f:
                writer.WriteNumberValue(f);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case decimal m:
                writer.WriteNumberValue(m);
                break;
            default:
                writer.WriteStringValue(Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
        }
    }

    /// <summary>Converts a JSON value returned by the server to the CLR type of the column.</summary>
    public static object ToClr(JsonElement element, Db2Type type)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Undefined:
            case JsonValueKind.Null:
                return DBNull.Value;
        }

        var clrType = type.ClrType;
        try
        {
            if (clrType == typeof(string))
            {
                return element.ValueKind == JsonValueKind.String ? element.GetString()! : element.GetRawText();
            }

            if (clrType == typeof(int))
            {
                return element.ValueKind == JsonValueKind.Number ? element.GetInt32() : int.Parse(element.GetString()!, CultureInfo.InvariantCulture);
            }

            if (clrType == typeof(short))
            {
                return element.ValueKind == JsonValueKind.Number ? element.GetInt16() : short.Parse(element.GetString()!, CultureInfo.InvariantCulture);
            }

            if (clrType == typeof(long))
            {
                return element.ValueKind == JsonValueKind.Number ? element.GetInt64() : long.Parse(element.GetString()!, CultureInfo.InvariantCulture);
            }

            if (clrType == typeof(decimal))
            {
                if (element.ValueKind == JsonValueKind.Number)
                {
                    return element.TryGetDecimal(out var m) ? m : (decimal)element.GetDouble();
                }

                return decimal.Parse(element.GetString()!, NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            if (clrType == typeof(double))
            {
                return element.ValueKind == JsonValueKind.Number ? element.GetDouble() : ParseDouble(element.GetString()!);
            }

            if (clrType == typeof(float))
            {
                return element.ValueKind == JsonValueKind.Number ? element.GetSingle() : (float)ParseDouble(element.GetString()!);
            }

            if (clrType == typeof(bool))
            {
                return element.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Number => element.GetDouble() != 0,
                    _ => ParseBool(element.GetString()!),
                };
            }

            if (clrType == typeof(DateTime))
            {
                var s = element.GetString()!;
                return type.Kind == Db2TypeKind.Date ? ParseDate(s) : ParseTimestamp(s);
            }

            if (clrType == typeof(TimeSpan))
            {
                return ParseTime(element.GetString()!);
            }

            if (clrType == typeof(byte[]))
            {
                return ParseBinary(element);
            }
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException or OverflowException)
        {
            throw new InvalidCastException(
                $"Unable to convert server value {element.GetRawText()} to {clrType.Name} for a {type.Name} column.", ex);
        }

        return element.ValueKind == JsonValueKind.String ? element.GetString()! : element.GetRawText();
    }

    private static double ParseDouble(string s)
        => s.Trim().ToUpperInvariant() switch
        {
            "NAN" => double.NaN,
            "INFINITY" or "+INFINITY" => double.PositiveInfinity,
            "-INFINITY" => double.NegativeInfinity,
            var t => double.Parse(t, NumberStyles.Float, CultureInfo.InvariantCulture),
        };

    private static bool ParseBool(string s)
        => s.Trim().ToLowerInvariant() switch
        {
            "true" or "1" or "t" or "y" or "yes" => true,
            "false" or "0" or "f" or "n" or "no" => false,
            _ => throw new FormatException($"'{s}' is not a boolean value."),
        };

    internal static DateTime ParseDate(string s)
    {
        s = s.Trim();
        if (s.Length > 10)
        {
            return ParseTimestamp(s).Date;
        }

        string[] formats = ["yyyy-MM-dd", "MM/dd/yyyy", "dd.MM.yyyy", "yyyy/MM/dd"];
        return DateTime.ParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
    }

    internal static DateTime ParseTimestamp(string s)
    {
        s = s.Trim();

        // Db2 native format: yyyy-MM-dd-HH.mm.ss.ffffff
        if (s.Length >= 19 && s[10] == '-' && s[13] == '.')
        {
            s = string.Concat(s.AsSpan(0, 10), " ", s.Substring(11, 8).Replace('.', ':'), s.AsSpan(19));
        }

        // Fractional seconds can carry up to 12 digits; .NET supports 7.
        var dot = s.IndexOf('.', Math.Min(19, s.Length));
        if (dot > 0)
        {
            var end = dot + 1;
            while (end < s.Length && char.IsDigit(s[end]))
            {
                end++;
            }

            if (end - dot - 1 > 7)
            {
                s = string.Concat(s.AsSpan(0, dot + 8), s.AsSpan(end));
            }
        }

        string[] formats = ["yyyy-MM-dd HH:mm:ss.FFFFFFF", "yyyy-MM-ddTHH:mm:ss.FFFFFFF", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd"];
        return DateTime.ParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
    }

    internal static TimeSpan ParseTime(string s)
    {
        s = s.Trim();
        if (s.Length >= 19 && s[4] == '-')
        {
            return ParseTimestamp(s).TimeOfDay;
        }

        var parts = s.Replace('.', ':').Split(':');
        return new TimeSpan(
            int.Parse(parts[0], CultureInfo.InvariantCulture),
            parts.Length > 1 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : 0,
            parts.Length > 2 ? int.Parse(parts[2], CultureInfo.InvariantCulture) : 0);
    }

    private static byte[] ParseBinary(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            var bytes = new byte[element.GetArrayLength()];
            var i = 0;
            foreach (var b in element.EnumerateArray())
            {
                bytes[i++] = unchecked((byte)b.GetInt32());
            }

            return bytes;
        }

        var s = element.GetString() ?? string.Empty;
        if (s.Length % 2 == 0 && s.All(Uri.IsHexDigit))
        {
            return Convert.FromHexString(s);
        }

        return Convert.FromBase64String(s);
    }
}
