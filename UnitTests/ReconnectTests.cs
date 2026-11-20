using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using io.github.mapepire_ibmi;
using io.github.mapepire_ibmi.types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace UnitTests;

/// <summary>Checks the public reconnect path against a local Mapepire endpoint.</summary>
[TestClass]
public sealed class ReconnectTests
{
    /// <summary>Verifies transport replacement, pending request cleanup, and routing when the server reuses its job ID.</summary>
    [TestMethod]
    public async Task ReconnectReplacesSocketWhenServerReusesJobIdAsync()
    {
        const string jobId = "123456/QUSER/QZDASOINIT";
        int connections = 0;
        var firstPending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using X509Certificate2 certificate = CreateCertificate();

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options =>
            options.Listen(IPAddress.Loopback, 0, endpoint => endpoint.UseHttps(certificate)));
        await using WebApplication app = builder.Build();
        app.UseWebSockets();
        app.Map("/db/", async context =>
        {
            using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
            int connection = Interlocked.Increment(ref connections);
            await ServeConnectionAsync(socket, connection, jobId, firstPending, deadline.Token);
            if (connection == 1)
            {
                firstClosed.TrySetResult();
            }
        });
        await app.StartAsync(deadline.Token);
        string address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var daemon = new DaemonServer("127.0.0.1", new Uri(address).Port, "test", "test", false);
        var job = new SqlJob();
        try
        {
            ConnectionResult? first = await Task.Run(() => job.Connect(daemon)).WaitAsync(deadline.Token);
            Assert.IsNotNull(first);
            Assert.IsTrue(first.Success);
            Assert.AreEqual(jobId, first.Job);

            Task<string> pending = Task.Run(() => job.Send("""{"id":"pending","type":"test_pending"}"""));
            await firstPending.Task.WaitAsync(deadline.Token);
            ConnectionResult? second = await Task.Run(() => job.Connect(daemon)).WaitAsync(deadline.Token);
            Assert.IsNotNull(second);
            Assert.IsTrue(second.Success);
            Assert.AreEqual(jobId, second.Job);
            Assert.AreEqual(2, Volatile.Read(ref connections));
            await Assert.ThrowsExceptionAsync<WebSocketException>(async () => await pending.WaitAsync(deadline.Token));
            await firstClosed.Task.WaitAsync(deadline.Token);

            VersionCheckResult version = await Task.Run(job.GetVersion).WaitAsync(deadline.Token);
            Assert.IsTrue(version.Success);
            Assert.AreEqual("socket-2", version.Version);
        }
        finally
        {
            job.Close();
            using var shutdownDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await app.StopAsync(shutdownDeadline.Token);
        }
    }

    /// <summary>Echoes request IDs and identifies the socket that received the version request.</summary>
    /// <param name="socket">The accepted WebSocket.</param>
    /// <param name="connection">The connection number assigned by the test endpoint.</param>
    /// <param name="jobId">The job ID to return for either connection.</param>
    /// <param name="firstPending">The signal that the first socket received the unanswered request.</param>
    /// <param name="cancellationToken">Bounds the lifetime of the test endpoint.</param>
    private static async Task ServeConnectionAsync(WebSocket socket, int connection, string jobId,
        TaskCompletionSource firstPending, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[4096];
        try
        {
            while (true)
            {
                var frame = await socket.ReceiveAsync(buffer.AsMemory(), cancellationToken);
                if (frame.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                using JsonDocument request = JsonDocument.Parse(buffer.AsMemory(0, frame.Count));
                string? id = request.RootElement.GetProperty("id").GetString();
                string? type = request.RootElement.GetProperty("type").GetString();
                if (connection == 1 && type == "test_pending")
                {
                    firstPending.TrySetResult();
                    continue;
                }

                byte[] response = type == "connect"
                    ? JsonSerializer.SerializeToUtf8Bytes(new { id, success = true, job = jobId })
                    : JsonSerializer.SerializeToUtf8Bytes(new { id, success = true, version = $"socket-{connection}" });
                await socket.SendAsync(response.AsMemory(), WebSocketMessageType.Text, true, cancellationToken);
            }
        }
        catch (WebSocketException) when (socket.State is WebSocketState.Aborted or WebSocketState.Closed)
        {
            // Disposing a replaced client socket can end the read without a close frame.
        }
    }

    /// <summary>Creates a short-lived certificate for the loopback-only server.</summary>
    /// <returns>The certificate with a Windows-compatible private key, owned by the caller.</returns>
    private static X509Certificate2 CreateCertificate()
    {
        using RSA key = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));
        return X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pkcs12), (string?)null);
    }
}
