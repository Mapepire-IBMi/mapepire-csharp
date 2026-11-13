using System.Text;

namespace Mapepire.Data.Internal;

/// <summary>
/// Db2 for i only understands positional (<c>?</c>) parameter markers. This rewrites named markers
/// (<c>@name</c>) into positional markers and returns the parameter names in the order they appear,
/// so a parameter used several times is bound several times. String literals, delimited identifiers
/// and comments are left untouched. Trailing statement terminators are removed, because a terminating
/// semicolon is not valid in a single prepared statement.
/// </summary>
internal static class SqlParameterRewriter
{
    public readonly record struct Result(string Sql, IReadOnlyList<string>? NamedOrder, int PositionalCount);

    /// <param name="sql">The command text.</param>
    /// <param name="isKnownName">Returns true when <c>@name</c> is a parameter of the command.</param>
    public static Result Rewrite(string sql, Func<string, bool> isKnownName)
    {
        var sb = new StringBuilder(sql.Length);
        List<string>? names = null;
        var positional = 0;
        var i = 0;

        while (i < sql.Length)
        {
            var c = sql[i];

            if (c is '\'' or '"')
            {
                var end = SkipQuoted(sql, i, c);
                sb.Append(sql, i, end - i);
                i = end;
                continue;
            }

            if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                var end = sql.IndexOf('\n', i);
                end = end < 0 ? sql.Length : end;
                sb.Append(sql, i, end - i);
                i = end;
                continue;
            }

            if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                var end = sql.IndexOf("*/", i + 2, StringComparison.Ordinal);
                end = end < 0 ? sql.Length : end + 2;
                sb.Append(sql, i, end - i);
                i = end;
                continue;
            }

            if (c == '?')
            {
                positional++;
                sb.Append(c);
                i++;
                continue;
            }

            if (c == '@' && (i == 0 || !IsIdentifierChar(sql[i - 1])) && i + 1 < sql.Length && IsIdentifierStart(sql[i + 1]))
            {
                var end = i + 1;
                while (end < sql.Length && IsParameterNameChar(sql[end]))
                {
                    end++;
                }

                var name = sql.Substring(i + 1, end - i - 1);
                if (isKnownName(name))
                {
                    (names ??= []).Add(name);
                    sb.Append('?');
                    i = end;
                    continue;
                }
            }

            sb.Append(c);
            i++;
        }

        if (names != null && positional > 0)
        {
            throw new InvalidOperationException("Command text mixes named (@name) and positional (?) parameter markers.");
        }

        return new Result(TrimTerminator(sb.ToString()), names, positional);
    }

    private static int SkipQuoted(string sql, int start, char quote)
    {
        var i = start + 1;
        while (i < sql.Length)
        {
            if (sql[i] == quote)
            {
                if (i + 1 < sql.Length && sql[i + 1] == quote)
                {
                    i += 2;
                    continue;
                }

                return i + 1;
            }

            i++;
        }

        return sql.Length;
    }

    internal static string TrimTerminator(string sql)
    {
        var end = sql.Length;
        while (end > 0 && (char.IsWhiteSpace(sql[end - 1]) || sql[end - 1] == ';'))
        {
            end--;
        }

        var start = 0;
        while (start < end && char.IsWhiteSpace(sql[start]))
        {
            start++;
        }

        return sql.Substring(start, end - start);
    }

    private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';

    private static bool IsParameterNameChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    // Db2 for i ordinary identifiers may contain @, # and $.
    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '@' or '#' or '$';
}
