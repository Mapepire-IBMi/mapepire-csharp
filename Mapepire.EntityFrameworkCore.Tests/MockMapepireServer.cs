using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Mapepire.Data;

namespace Mapepire.EntityFrameworkCore.Tests;

/// <summary>
/// Runs the Mapepire mock server (https://github.com/Mapepire-IBMi/mapepire-mockserver) in a child Java process,
/// for exercising provider paths without IBM i. The server answers SQL from the given rules, then its built-in rules.
/// </summary>
/// <remarks>
/// The build downloads the mock server jar next to the test assembly. Running it needs Java 8 or later, found under
/// JAVA_HOME or on the PATH.
/// </remarks>
internal sealed partial class MockMapepireServer : IAsyncDisposable
{
    private const string JarFileName = "mapepire-mockserver.jar";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan TraceTimeout = TimeSpan.FromSeconds(5);

    private readonly Process _process;
    private readonly string _rulesFile;
    private readonly ConcurrentQueue<string> _output = new();
    private readonly TaskCompletionSource<int> _listening = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private MockMapepireServer(Process process, string rulesFile)
    {
        _process = process;
        _rulesFile = rulesFile;
        process.OutputDataReceived += OnOutput;
        process.ErrorDataReceived += OnOutput;
        process.Exited += (_, _) => _listening.TrySetException(
            new InvalidOperationException("The mock server exited during startup:" + Environment.NewLine + string.Join(Environment.NewLine, _output)));
    }

    public string ConnectionString { get; private set; } = "";

    /// <summary>Starts a mock server that answers SQL from <paramref name="sqlRules"/> before its built-in rules.</summary>
    /// <param name="sqlRules">
    /// SQL rules in the mock server's rule file format. A pattern is a regular expression matched case-insensitively
    /// against the whole statement.
    /// </param>
    /// <returns>The running server.</returns>
    /// <exception cref="FileNotFoundException">The build did not download the mock server jar.</exception>
    /// <exception cref="InvalidOperationException">The mock server did not start.</exception>
    public static async Task<MockMapepireServer> StartAsync(params object[] sqlRules)
    {
        var jar = Path.Combine(AppContext.BaseDirectory, JarFileName);
        if (!File.Exists(jar))
        {
            throw new FileNotFoundException("The build did not download the mock server jar.", jar);
        }

        var rulesFile = Path.Combine(Path.GetTempPath(), "mapepire-mock-rules-" + Guid.NewGuid().ToString("N") + ".json");
        await File.WriteAllTextAsync(rulesFile, JsonSerializer.Serialize(new { include_defaults = true, sql = sqlRules }));

        var startInfo = new ProcessStartInfo(FindJava())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        // --verbose echoes every request, which ExecutedSql reads back.
        foreach (var argument in new[] { "-jar", jar, "--host", "127.0.0.1", "--port", "0", "--rules", rulesFile, "--verbose" })
        {
            startInfo.ArgumentList.Add(argument);
        }

        var server = new MockMapepireServer(new Process { StartInfo = startInfo, EnableRaisingEvents = true }, rulesFile);
        try
        {
            server._process.Start();
            server._process.BeginOutputReadLine();
            server._process.BeginErrorReadLine();
            var port = await server._listening.Task.WaitAsync(StartupTimeout);
            server.ConnectionString = new MapepireConnectionStringBuilder
            {
                Server = "localhost",
                Port = port,
                UserId = "MOCK",
                Password = "mockingbird",
                // The mock server presents a bundled self-signed certificate.
                RejectUnauthorized = false,
                AutoCommit = false,
            }.ConnectionString;
            return server;
        }
        catch
        {
            await server.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Waits until the server has received <paramref name="count"/> statements matching <paramref name="predicate"/>,
    /// up to a short timeout, and returns how many it received.
    /// </summary>
    /// <param name="predicate">Selects the statements to count.</param>
    /// <param name="count">The number of statements to wait for.</param>
    /// <returns>The number of matching statements received.</returns>
    public async Task<int> WaitForSqlAsync(Func<string, bool> predicate, int count)
    {
        // The server logs each request before replying, but its output reaches this process asynchronously.
        var deadline = DateTime.UtcNow + TraceTimeout;
        while (true)
        {
            var received = ExecutedSql().Count(predicate);
            if (received >= count || DateTime.UtcNow >= deadline)
            {
                return received;
            }

            await Task.Delay(20);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }
        }
        catch (InvalidOperationException)
        {
            // The process never started.
        }
        finally
        {
            _process.Dispose();
            File.Delete(_rulesFile);
        }
    }

    /// <summary>The SQL text of every sql and prepared request the server has logged so far.</summary>
    private IEnumerable<string> ExecutedSql()
    {
        foreach (var line in _output)
        {
            var match = RequestLine().Match(line);
            if (!match.Success)
            {
                continue;
            }

            using var request = JsonDocument.Parse(match.Groups["json"].Value);
            if (request.RootElement.TryGetProperty("sql", out var sql) && sql.ValueKind == JsonValueKind.String)
            {
                yield return sql.GetString()!;
            }
        }
    }

    private void OnOutput(object sender, DataReceivedEventArgs e)
    {
        if (e.Data is null)
        {
            return;
        }

        _output.Enqueue(e.Data);
        var match = ListeningLine().Match(e.Data);
        if (match.Success)
        {
            _listening.TrySetResult(int.Parse(match.Groups["port"].Value));
        }
    }

    private static string FindJava()
    {
        var executable = OperatingSystem.IsWindows() ? "java.exe" : "java";
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrEmpty(javaHome))
        {
            var candidate = Path.Combine(javaHome, "bin", executable);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return executable;
    }

    [GeneratedRegex(@"listening on wss?://[^/]+:(?<port>\d+)/")]
    private static partial Regex ListeningLine();

    // For example: [2026-01-01 12:00:00.000] [conn1] [IN] {"id":"query4","type":"sql","sql":"ROLLBACK",...}
    [GeneratedRegex(@"\[IN\] (?<json>\{.*\})$")]
    private static partial Regex RequestLine();
}
