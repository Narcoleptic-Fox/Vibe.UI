using System.Net;
using System.Net.Sockets;
using System.Text;
using Shouldly;
using Vibe.UI.Docs.E2E.Infrastructure;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Integration;

/// <summary>
/// The suite must test this checkout, whatever else is running on the machine. A decoy server
/// on the old default port (5000, and 5101 for the compatibility app) serves a page and a
/// dotnet.js of its own; the suite must neither use it nor accept it.
/// </summary>
[Trait("Category", TestCategories.Integration)]
public class ServerIsolationTests
{
    private const int OldDocsPort = 5000;
    private const int OldStandalonePort = 5101;

    [Fact]
    public async Task DocsServer_WithADecoyOnTheOldDefaultPort_IsThisCheckoutsOwnServer()
    {
        // Something on 5000 already counts as a decoy too: it is not this run's server.
        await using var decoy = DecoyServer.TryStart(OldDocsPort);

        await DocsServerManager.AcquireAsync();

        var baseUrl = new Uri(DocsServerManager.BaseUrl);
        if (Environment.GetEnvironmentVariable(DocsServerManager.BaseUrlVariable) is null)
        {
            baseUrl.Port.ShouldNotBe(
                OldDocsPort,
                "the run must start its own server, not use whatever holds 5000"
            );
        }

        using var http = new HttpClient();
        var served = await http.GetByteArrayAsync(
            $"{DocsServerManager.BaseUrl}/{DocsBuildIdentity.StampPath}"
        );
        served.ShouldBe(await File.ReadAllBytesAsync(DocsBuildIdentity.LocalStampFile));
    }

    [Fact]
    public async Task IdentityCheck_RefusesADecoy_AndNamesThePortAndWhatItFound()
    {
        await using var decoy = DecoyServer.Start();

        var failure = await Should.ThrowAsync<InvalidOperationException>(() =>
            DocsBuildIdentity.VerifyAsync(decoy.BaseUrl, CancellationToken.None)
        );

        failure.Message.ShouldContain($"port {decoy.Port}");
        failure.Message.ShouldContain("no docs assembly");
        failure.Message.ShouldContain("Vibe.UI.Docs.");
    }

    [Fact]
    public async Task CompatibilityServer_WithADecoyOnTheOldDefaultPort_IsStartedOnItsOwnPort()
    {
        if (Environment.GetEnvironmentVariable("VIBE_STANDALONE_BASE_URL") is not null)
        {
            return; // The caller owns that server.
        }

        await using var decoy = DecoyServer.TryStart(OldStandalonePort);

        var baseUrl = await CompatibilityServerManager.AcquireAsync(
            CompatibilityApp.StandaloneClient,
            CancellationToken.None
        );
        try
        {
            new Uri(baseUrl).Port.ShouldNotBe(OldStandalonePort);
            using var http = new HttpClient();
            (await http.GetStringAsync(baseUrl)).ShouldNotContain(DecoyServer.Marker);
        }
        finally
        {
            await CompatibilityServerManager.ReleaseAsync(CompatibilityApp.StandaloneClient);
        }
    }

    /// <summary>
    /// A minimal HTTP server on the loopback addresses that answers every request with a page
    /// and a dotnet.js that are not the docs build.
    /// </summary>
    private sealed class DecoyServer : IAsyncDisposable
    {
        public const string Marker = "decoy-server-not-this-checkout";

        private readonly List<TcpListener> _listeners;
        private readonly CancellationTokenSource _stop = new();
        private readonly List<Task> _loops = [];

        private DecoyServer(List<TcpListener> listeners, int port)
        {
            _listeners = listeners;
            Port = port;
            foreach (var listener in listeners)
            {
                _loops.Add(ServeAsync(listener, _stop.Token));
            }
        }

        public int Port { get; }

        public string BaseUrl => $"http://127.0.0.1:{Port}";

        /// <summary>On a free port.</summary>
        public static DecoyServer Start()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return new DecoyServer([listener], ((IPEndPoint)listener.LocalEndpoint).Port);
        }

        /// <summary>On <paramref name="port"/>, or null if something already holds it.</summary>
        public static DecoyServer? TryStart(int port)
        {
            var listeners = new List<TcpListener>();
            foreach (var address in new[] { IPAddress.Loopback, IPAddress.IPv6Loopback })
            {
                try
                {
                    var listener = new TcpListener(address, port);
                    listener.Start();
                    listeners.Add(listener);
                }
                catch (SocketException)
                {
                    // Taken, or no IPv6: whatever holds it is not this run's server either.
                }
            }

            return listeners.Count == 0 ? null : new DecoyServer(listeners, port);
        }

        private static async Task ServeAsync(TcpListener listener, CancellationToken stop)
        {
            while (!stop.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(stop);
                }
                catch (Exception) when (stop.IsCancellationRequested)
                {
                    return;
                }
                catch (SocketException)
                {
                    return;
                }

                _ = RespondAsync(client);
            }
        }

        private static async Task RespondAsync(TcpClient client)
        {
            using (client)
            {
                try
                {
                    var stream = client.GetStream();
                    var buffer = new byte[8192];
                    var read = await stream.ReadAsync(buffer);
                    var requestLine = Encoding.ASCII.GetString(buffer, 0, read).Split("\r\n")[0];
                    var body = requestLine.Contains("dotnet.js", StringComparison.Ordinal)
                        ? $"/* {Marker} */"
                        : $"<!DOCTYPE html><html><body><h1>{Marker}</h1></body></html>";
                    var content = Encoding.UTF8.GetBytes(body);
                    var head = Encoding.ASCII.GetBytes(
                        $"HTTP/1.1 200 OK\r\nContent-Type: text/html\r\nContent-Length: {content.Length}\r\nConnection: close\r\n\r\n"
                    );
                    await stream.WriteAsync(head);
                    await stream.WriteAsync(content);
                }
                catch (IOException)
                {
                    // The client went away.
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _stop.CancelAsync();
            foreach (var listener in _listeners)
            {
                listener.Stop();
            }

            await Task.WhenAll(_loops);
            _stop.Dispose();
        }
    }
}
