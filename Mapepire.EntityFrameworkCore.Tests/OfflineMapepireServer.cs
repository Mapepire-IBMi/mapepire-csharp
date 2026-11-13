using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Mapepire.Data;

namespace Mapepire.EntityFrameworkCore.Tests;

/// <summary>A loopback TLS websocket server for exercising provider lifecycle paths without IBM i.</summary>
internal sealed class OfflineMapepireServer : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _shutdown = new(TimeSpan.FromSeconds(30));
    private readonly X509Certificate2 _certificate;
    private readonly Task _server;
    private readonly Func<string, object> _execute;

    public OfflineMapepireServer(Func<string, object> execute)
    {
        _execute = execute;
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        // Windows SslStream cannot use the ephemeral key returned by CreateSelfSigned.
        _certificate = X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx), password: null);
        _listener.Start();
        ConnectionString = new MapepireConnectionStringBuilder
        {
            Server = "127.0.0.1",
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port,
            UserId = "TEST",
            Password = "unused",
            RejectUnauthorized = false,
            AutoCommit = false,
        }.ConnectionString;
        _server = AcceptConnectionsAsync();
    }

    public string ConnectionString { get; }

    public static object Success() => new { success = true, is_done = true, update_count = 1 };

    private async Task AcceptConnectionsAsync()
    {
        var clients = new List<Task>();
        try
        {
            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync(_shutdown.Token);
                clients.Add(ServeAsync(client, clients.Count + 1));
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
        }
        finally
        {
            await Task.WhenAll(clients);
        }
    }

    private async Task ServeAsync(TcpClient client, int sessionId)
    {
        using (client)
        using (var stream = new SslStream(client.GetStream()))
        {
            try
            {
                await stream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions { ServerCertificate = _certificate }, _shutdown.Token);
                var header = new StringBuilder();
                var singleByte = new byte[1];
                while (!header.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
                {
                    if (header.Length > 8192 || await stream.ReadAsync(singleByte, _shutdown.Token) == 0)
                    {
                        throw new IOException("Incomplete websocket upgrade request.");
                    }

                    header.Append((char)singleByte[0]);
                }

                var key = header.ToString().Split("\r\n")
                    .Single(line => line.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase)).Split(':', 2)[1].Trim();
                var accept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
                await stream.WriteAsync(Encoding.ASCII.GetBytes(
                    "HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: " + accept + "\r\n\r\n"), _shutdown.Token);
                using var socket = WebSocket.CreateFromStream(stream, isServer: true, subProtocol: null, Timeout.InfiniteTimeSpan);
                var buffer = new byte[8192];
                while (true)
                {
                    using var message = new MemoryStream();
                    WebSocketReceiveResult received;
                    do
                    {
                        received = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), _shutdown.Token);
                        if (received.MessageType == WebSocketMessageType.Close)
                        {
                            return;
                        }

                        message.Write(buffer, 0, received.Count);
                    }
                    while (!received.EndOfMessage);

                    using var json = JsonDocument.Parse(message.ToArray());
                    var root = json.RootElement;
                    object response = root.GetProperty("type").GetString() switch
                    {
                        "connect" => new { success = true, job = "OFFLINE/TEST/" + sessionId },
                        "getversion" => new { success = true, version = "offline-test" },
                        "sql" => _execute(root.GetProperty("sql").GetString()!),
                        _ => throw new InvalidOperationException("Unexpected test protocol request."),
                    };
                    await socket.SendAsync(new ArraySegment<byte>(JsonSerializer.SerializeToUtf8Bytes(response)), WebSocketMessageType.Text, true, _shutdown.Token);
                }
            }
            catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
            {
            }
            catch (WebSocketException)
            {
                // SqlJob.Close disposes its socket without a websocket close handshake.
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _shutdown.CancelAsync();
        _listener.Stop();
        try
        {
            await _server;
        }
        finally
        {
            _certificate.Dispose();
            _shutdown.Dispose();
        }
    }
}
