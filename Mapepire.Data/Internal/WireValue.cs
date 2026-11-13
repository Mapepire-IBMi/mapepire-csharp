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
    /// <param name="value">The parameter value; null and <see cref="DBNull"/> become JSON null.</param>
    /// <param name="dbType">The parameter's explicit <see cref="DbType"/>, if one was set.</param>
    /// <returns>A string, number, bool or null.</returns>
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
            string text => text,
            char character => character.ToString(),
            bool flag => dbType == DbType.Boolean ? flag : (flag ? 1 : 0),
            byte or sbyte or short or ushort or int => Convert.ToInt32(value, CultureInfo.InvariantCulture),
            uint or long or ulong => Convert.ToString(value, CultureInfo.InvariantCulture),
            float single => single,
            double number => number,
            decimal money => money.ToString(CultureInfo.InvariantCulture),
            DateTime dateTime => dbType == DbType.Date
                ? dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : dbType == DbType.Time
                    ? dateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
                    : dateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture),
            DateTimeOffset offset => offset.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture),
            DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly time => time.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            TimeSpan timeSpan => FormatTime(timeSpan),
            Guid guid => guid.ToString("D"),
            byte[] bytes => Convert.ToHexString(bytes),
            Enum enumValue => numericTarget || dbType is null
                ? Convert.ToInt64(enumValue, CultureInfo.InvariantCulture)
                : enumValue.ToString(),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };
    }

    /// <summary>Writes a wire-ready value (see <see cref="FromParameter"/>) as a JSON value.</summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">A string, number, bool or null.</param>
    public static void Write(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string text:
                writer.WriteStringValue(text);
                break;
            case bool flag:
                writer.WriteBooleanValue(flag);
                break;
            case int integer:
                writer.WriteNumberValue(integer);
                break;
            case long big:
                writer.WriteNumberValue(big);
                break;
            case float single:
                writer.WriteNumberValue(single);
                break;
            case double number:
                writer.WriteNumberValue(number);
                break;
            case decimal money:
                writer.WriteNumberValue(money);
                break;
            default:
                writer.WriteStringValue(Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
        }
    }

    /// <summary>Converts a JSON value returned by the server to the CLR type of the column.</summary>
    /// <param name="element">The value; JSON null or an undefined element becomes <see cref="DBNull"/>.</param>
    /// <param name="type">The column type.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="InvalidCastException">The value cannot be converted to the column's type.</exception>
    public static object ToClr(JsonElement element, Db2Type type)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Undefined:
            case JsonValueKind.Null:
                return DBNull.Value;
        }

        // From here on the element is not null, so GetString() returns null only for non-string kinds,
        // which throw InvalidOperationException instead; the "!" below never hides a real null.
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
                    return element.TryGetDecimal(out var money) ? money : (decimal)element.GetDouble();
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
                var text = element.GetString()!;
                return type.Kind == Db2TypeKind.Date ? ParseDate(text) : ParseTimestamp(text);
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

    /// <summary>Parses a DATE value in ISO, USA, EUR or JIS format, or the date part of a timestamp.</summary>
    /// <param name="text">The value.</param>
    /// <returns>The date, with a time of midnight.</returns>
    internal static DateTime ParseDate(string text)
    {
        var value = text.Trim();
        if (value.Length > 10)
        {
            return ParseTimestamp(value).Date;
        }

        string[] formats = ["yyyy-MM-dd", "MM/dd/yyyy", "dd.MM.yyyy", "yyyy/MM/dd"];
        return DateTime.ParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
    }

    /// <summary>
    /// Parses a TIMESTAMP value in the Db2 format (<c>yyyy-MM-dd-HH.mm.ss.ffffff</c>) or the JDBC format
    /// (<c>yyyy-MM-dd HH:mm:ss.ffffff</c>). Fractional seconds beyond the 7 digits .NET supports are truncated.
    /// </summary>
    /// <param name="text">The value.</param>
    /// <returns>The timestamp.</returns>
    internal static DateTime ParseTimestamp(string text)
    {
        var value = text.Trim();

        // Db2 native format: yyyy-MM-dd-HH.mm.ss.ffffff
        if (value.Length >= 19 && value[10] == '-' && value[13] == '.')
        {
            value = string.Concat(value.AsSpan(0, 10), " ", value.Substring(11, 8).Replace('.', ':'), value.AsSpan(19));
        }

        // Fractional seconds can carry up to 12 digits; .NET supports 7.
        var dot = value.IndexOf('.', Math.Min(19, value.Length));
        if (dot > 0)
        {
            var end = dot + 1;
            while (end < value.Length && char.IsDigit(value[end]))
            {
                end++;
            }

            if (end - dot - 1 > 7)
            {
                value = string.Concat(value.AsSpan(0, dot + 8), value.AsSpan(end));
            }
        }

        string[] formats = ["yyyy-MM-dd HH:mm:ss.FFFFFFF", "yyyy-MM-ddTHH:mm:ss.FFFFFFF", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd"];
        return DateTime.ParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
    }

    /// <summary>Parses a TIME value with <c>.</c> or <c>:</c> separators, or the time part of a timestamp.</summary>
    /// <param name="text">The value.</param>
    /// <returns>The time of day.</returns>
    internal static TimeSpan ParseTime(string text)
    {
        var value = text.Trim();
        if (value.Length >= 19 && value[4] == '-')
        {
            return ParseTimestamp(value).TimeOfDay;
        }

        var parts = value.Replace('.', ':').Split(':');
        return new TimeSpan(
            int.Parse(parts[0], CultureInfo.InvariantCulture),
            parts.Length > 1 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : 0,
            parts.Length > 2 ? int.Parse(parts[2], CultureInfo.InvariantCulture) : 0);
    }

    /// <summary>Formats a time of day for a TIME parameter.</summary>
    /// <param name="timeSpan">The time of day.</param>
    /// <returns>The time as <c>HH:mm:ss</c>; Db2 for i TIME has whole-second precision.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or a day or longer.</exception>
    private static string FormatTime(TimeSpan timeSpan)
    {
        if (timeSpan < TimeSpan.Zero || timeSpan >= TimeSpan.FromDays(1))
        {
            throw new ArgumentOutOfRangeException(nameof(timeSpan), timeSpan, "TIME values must be between 00:00:00 and 23:59:59.");
        }

        return timeSpan.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }

    private static double ParseDouble(string text)
        => text.Trim().ToUpperInvariant() switch
        {
            "NAN" => double.NaN,
            "INFINITY" or "+INFINITY" => double.PositiveInfinity,
            "-INFINITY" => double.NegativeInfinity,
            var number => double.Parse(number, NumberStyles.Float, CultureInfo.InvariantCulture),
        };

    private static bool ParseBool(string text)
        => text.Trim().ToLowerInvariant() switch
        {
            "true" or "1" or "t" or "y" or "yes" => true,
            "false" or "0" or "f" or "n" or "no" => false,
            _ => throw new FormatException($"'{text}' is not a boolean value."),
        };

    /// <summary>Parses a binary value sent as a JSON array of bytes, a hexadecimal string, or a base64 string.</summary>
    /// <param name="element">The value.</param>
    /// <returns>The bytes.</returns>
    private static byte[] ParseBinary(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            var bytes = new byte[element.GetArrayLength()];
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                bytes[index++] = unchecked((byte)item.GetInt32());
            }

            return bytes;
        }

        var text = element.GetString() ?? string.Empty;
        if (text.Length % 2 == 0 && text.All(Uri.IsHexDigit))
        {
            return Convert.FromHexString(text);
        }

        return Convert.FromBase64String(text);
    }
}
