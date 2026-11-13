using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Mapepire.Data;

/// <summary>
/// Builds and parses connection strings for <see cref="MapepireConnection"/>.
/// </summary>
/// <example>
/// <c>Server=myibmi;Port=8076;User ID=MYUSER;Password=secret;Default Schema=MYLIB;Auto Commit=false</c>
/// </example>
public sealed class MapepireConnectionStringBuilder : DbConnectionStringBuilder
{
    private static readonly Dictionary<string, string> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["server"] = Keywords.Server,
        ["host"] = Keywords.Server,
        ["data source"] = Keywords.Server,
        ["datasource"] = Keywords.Server,
        ["port"] = Keywords.Port,
        ["user id"] = Keywords.UserId,
        ["userid"] = Keywords.UserId,
        ["user"] = Keywords.UserId,
        ["uid"] = Keywords.UserId,
        ["username"] = Keywords.UserId,
        ["password"] = Keywords.Password,
        ["pwd"] = Keywords.Password,
        ["default schema"] = Keywords.DefaultSchema,
        ["defaultschema"] = Keywords.DefaultSchema,
        ["database"] = Keywords.DefaultSchema,
        ["initial catalog"] = Keywords.DefaultSchema,
        ["schema"] = Keywords.DefaultSchema,
        ["libraries"] = Keywords.Libraries,
        ["library list"] = Keywords.Libraries,
        ["reject unauthorized"] = Keywords.RejectUnauthorized,
        ["rejectunauthorized"] = Keywords.RejectUnauthorized,
        ["trust server certificate"] = Keywords.TrustServerCertificate,
        ["trustservercertificate"] = Keywords.TrustServerCertificate,
        ["ca"] = Keywords.Ca,
        ["ca certificate"] = Keywords.Ca,
        ["naming"] = Keywords.Naming,
        ["auto commit"] = Keywords.AutoCommit,
        ["autocommit"] = Keywords.AutoCommit,
        ["transaction isolation"] = Keywords.TransactionIsolation,
        ["isolation level"] = Keywords.TransactionIsolation,
        ["date format"] = Keywords.DateFormat,
        ["time format"] = Keywords.TimeFormat,
        ["fetch size"] = Keywords.FetchSize,
        ["block size"] = Keywords.FetchSize,
        ["pooling"] = Keywords.Pooling,
        ["max pool size"] = Keywords.MaxPoolSize,
        ["maximum pool size"] = Keywords.MaxPoolSize,
        ["connection idle validation"] = Keywords.IdleValidationSeconds,
        ["idle validation seconds"] = Keywords.IdleValidationSeconds,
    };

    /// <summary>Canonical keyword names.</summary>
    public static class Keywords
    {
        public const string Server = "Server";
        public const string Port = "Port";
        public const string UserId = "User ID";
        public const string Password = "Password";
        public const string DefaultSchema = "Default Schema";
        public const string Libraries = "Libraries";
        public const string RejectUnauthorized = "Reject Unauthorized";
        public const string TrustServerCertificate = "Trust Server Certificate";
        public const string Ca = "CA";
        public const string Naming = "Naming";
        public const string AutoCommit = "Auto Commit";
        public const string TransactionIsolation = "Transaction Isolation";
        public const string DateFormat = "Date Format";
        public const string TimeFormat = "Time Format";
        public const string FetchSize = "Fetch Size";
        public const string Pooling = "Pooling";
        public const string MaxPoolSize = "Max Pool Size";
        public const string IdleValidationSeconds = "Idle Validation Seconds";
    }

    public MapepireConnectionStringBuilder()
    {
    }

    public MapepireConnectionStringBuilder(string? connectionString)
    {
        ConnectionString = connectionString ?? string.Empty;
    }

    /// <inheritdoc />
    [AllowNull]
    public override object this[string keyword]
    {
        get => base[Canonical(keyword)];
        set => base[Canonical(keyword)] = value;
    }

    /// <inheritdoc />
    public override bool ContainsKey(string keyword) => base.ContainsKey(Canonical(keyword));

    /// <inheritdoc />
    public override bool Remove(string keyword) => base.Remove(Canonical(keyword));

    /// <inheritdoc />
    public override bool TryGetValue(string keyword, [NotNullWhen(true)] out object? value)
        => base.TryGetValue(Canonical(keyword), out value);

    private static string Canonical(string keyword)
        => Synonyms.TryGetValue(keyword.Trim(), out var canonical) ? canonical : keyword;

    /// <summary>Host name of the IBM i running the Mapepire server.</summary>
    public string? Server
    {
        get => GetString(Keywords.Server);
        set => SetValue(Keywords.Server, value);
    }

    /// <summary>Port of the Mapepire server. Defaults to 8076.</summary>
    public int Port
    {
        get => GetInt(Keywords.Port, 8076);
        set => SetValue(Keywords.Port, value);
    }

    /// <summary>IBM i user profile.</summary>
    public string? UserId
    {
        get => GetString(Keywords.UserId);
        set => SetValue(Keywords.UserId, value);
    }

    /// <summary>Password for the user profile.</summary>
    public string? Password
    {
        get => GetString(Keywords.Password);
        set => SetValue(Keywords.Password, value);
    }

    /// <summary>
    /// Default schema (library) for unqualified names. It is placed first in the JDBC "libraries" property.
    /// </summary>
    public string? DefaultSchema
    {
        get => GetString(Keywords.DefaultSchema);
        set => SetValue(Keywords.DefaultSchema, value);
    }

    /// <summary>Additional libraries (comma separated) to add to the library list.</summary>
    public string? Libraries
    {
        get => GetString(Keywords.Libraries);
        set => SetValue(Keywords.Libraries, value);
    }

    /// <summary>Whether to reject untrusted server certificates. Defaults to true.</summary>
    public bool RejectUnauthorized
    {
        get
        {
            if (ContainsKey(Keywords.TrustServerCertificate))
            {
                return !GetBool(Keywords.TrustServerCertificate, false);
            }

            return GetBool(Keywords.RejectUnauthorized, true);
        }
        set
        {
            Remove(Keywords.TrustServerCertificate);
            SetValue(Keywords.RejectUnauthorized, value);
        }
    }

    /// <summary>Base64 encoded (DER) CA certificate used to validate the server certificate.</summary>
    public string? Ca
    {
        get => GetString(Keywords.Ca);
        set => SetValue(Keywords.Ca, value);
    }

    /// <summary>JDBC naming convention: "sql" (default) or "system".</summary>
    public string Naming
    {
        get => GetString(Keywords.Naming) ?? "sql";
        set => SetValue(Keywords.Naming, value);
    }

    /// <summary>
    /// Whether every statement is committed immediately. Defaults to true. Set to false to use
    /// <see cref="MapepireConnection.BeginTransaction()"/> (the tables involved must be journaled).
    /// </summary>
    public bool AutoCommit
    {
        get => GetBool(Keywords.AutoCommit, true);
        set => SetValue(Keywords.AutoCommit, value);
    }

    /// <summary>
    /// JDBC "transaction isolation" property: none, read uncommitted, read committed, repeatable read or serializable.
    /// </summary>
    public string? TransactionIsolation
    {
        get => GetString(Keywords.TransactionIsolation);
        set => SetValue(Keywords.TransactionIsolation, value);
    }

    /// <summary>JDBC "date format" property. Defaults to "iso".</summary>
    public string DateFormat
    {
        get => GetString(Keywords.DateFormat) ?? "iso";
        set => SetValue(Keywords.DateFormat, value);
    }

    /// <summary>JDBC "time format" property. Defaults to "iso".</summary>
    public string TimeFormat
    {
        get => GetString(Keywords.TimeFormat) ?? "iso";
        set => SetValue(Keywords.TimeFormat, value);
    }

    /// <summary>Number of rows requested from the server per round trip. Defaults to 100.</summary>
    public int FetchSize
    {
        get => GetInt(Keywords.FetchSize, 100);
        set => SetValue(Keywords.FetchSize, value);
    }

    /// <summary>Whether to pool server connections. Defaults to true.</summary>
    public bool Pooling
    {
        get => GetBool(Keywords.Pooling, true);
        set => SetValue(Keywords.Pooling, value);
    }

    /// <summary>Maximum number of idle connections kept per connection string. Defaults to 100.</summary>
    public int MaxPoolSize
    {
        get => GetInt(Keywords.MaxPoolSize, 100);
        set => SetValue(Keywords.MaxPoolSize, value);
    }

    /// <summary>
    /// Pooled connections idle for longer than this many seconds are validated with a round trip
    /// before reuse. Defaults to 30.
    /// </summary>
    public int IdleValidationSeconds
    {
        get => GetInt(Keywords.IdleValidationSeconds, 30);
        set => SetValue(Keywords.IdleValidationSeconds, value);
    }

    /// <summary>
    /// Returns the effective library list: the default schema followed by any additional libraries.
    /// </summary>
    public IReadOnlyList<string> GetLibraryList()
    {
        var result = new List<string>();
        if (!string.IsNullOrWhiteSpace(DefaultSchema))
        {
            result.Add(DefaultSchema.Trim());
        }

        if (!string.IsNullOrWhiteSpace(Libraries))
        {
            foreach (var lib in Libraries.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!result.Contains(lib, StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(lib);
                }
            }
        }

        return result;
    }

    private void SetValue(string keyword, object? value)
    {
        if (value == null)
        {
            Remove(keyword);
        }
        else
        {
            base[keyword] = value is bool b ? (b ? "true" : "false") : Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }

    private string? GetString(string keyword)
        => TryGetValue(keyword, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private int GetInt(string keyword, int defaultValue)
    {
        var s = GetString(keyword);
        return string.IsNullOrWhiteSpace(s) ? defaultValue : int.Parse(s, CultureInfo.InvariantCulture);
    }

    private bool GetBool(string keyword, bool defaultValue)
    {
        var s = GetString(keyword)?.Trim();
        if (string.IsNullOrEmpty(s))
        {
            return defaultValue;
        }

        return s.ToLowerInvariant() switch
        {
            "true" or "yes" or "1" or "on" => true,
            "false" or "no" or "0" or "off" => false,
            _ => throw new ArgumentException($"Invalid boolean value '{s}' for connection string keyword '{keyword}'."),
        };
    }
}
