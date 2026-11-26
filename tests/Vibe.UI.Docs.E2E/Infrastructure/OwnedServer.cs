using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Vibe.UI.Docs.E2E.Infrastructure;

/// <summary>
/// A sample app this test run started itself, from this checkout, on a port chosen at run time.
/// </summary>
/// <remarks>
/// Several checkouts share a machine, and a server one of them left on a well-known port used to
/// be adopted by the others: their tests then ran against a different build. So nothing here
/// adopts a server. A server is ours only when the process we started reports that it is
/// listening on the port we gave it; Kestrel prints that line only after it has bound the port,
/// so no other process can be the one answering there. If the port is taken in the moment
/// between choosing it and binding it, our process fails to bind and exits, and another port is
/// tried.
/// </remarks>
internal sealed class OwnedServer
{
    private const int Attempts = 3;
    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(90);

    private readonly Process _process;
    private readonly Queue<string> _recentOutput;

    private OwnedServer(string name, string baseUrl, Process process, Queue<string> recentOutput)
    {
        Name = name;
        BaseUrl = baseUrl;
        _process = process;
        _recentOutput = recentOutput;
    }

    public string Name { get; }

    public string BaseUrl { get; }

    public bool HasExited => _process.HasExited;

    public string RecentOutput
    {
        get
        {
            lock (_recentOutput)
            {
                return string.Join(Environment.NewLine, _recentOutput);
            }
        }
    }

    /// <param name="name">For messages, e.g. "Docs server".</param>
    /// <param name="projectPath">The project to run, relative to the repository root.</param>
    /// <param name="environment">Extra environment variables for the server process.</param>
    public static async Task<OwnedServer> StartAsync(
        string name,
        string projectPath,
        IReadOnlyDictionary<string, string> environment,
        CancellationToken cancellationToken
    )
    {
        var failures = new List<string>();
        for (var attempt = 1; attempt <= Attempts; attempt++)
        {
            var port = FreeLoopbackPort();
            var baseUrl = $"http://127.0.0.1:{port}";
            var (process, output, listening) = Launch(projectPath, baseUrl, environment);
            var server = new OwnedServer(name, baseUrl, process, output);

            var started = await WaitForListeningAsync(process, listening, cancellationToken);
            if (started && await RespondsAsync(server, cancellationToken))
            {
                ServerRegistry.Track(server);
                return server;
            }

            failures.Add(
                $"Attempt {attempt}, port {port}: "
                    + (
                        process.HasExited
                            ? $"the process exited with code {process.ExitCode}."
                            : "it did not report listening within the time allowed."
                    )
                    + Environment.NewLine
                    + server.RecentOutput
            );
            await server.StopAsync();
        }

        throw new InvalidOperationException(
            $"{name} ({projectPath}) did not start after {Attempts} attempts on free ports. "
                + "It must be built first: dotnet build Vibe.slnx -c Release."
                + Environment.NewLine
                + string.Join(Environment.NewLine, failures)
        );
    }

    public async Task StopAsync()
    {
        ServerRegistry.Forget(this);
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _process.WaitForExitAsync(timeout.Token);
            }
        }
        catch (InvalidOperationException)
        {
            // Already gone.
        }
        finally
        {
            _process.Dispose();
        }
    }

    internal void KillNow()
    {
        try
        {
            _process.Kill(entireProcessTree: true);
        }
        catch
        {
            // The run is ending; nothing more can be done.
        }
    }

    internal static string RepositoryRoot()
    {
        for (
            var directory = AppContext.BaseDirectory;
            !string.IsNullOrEmpty(directory);
            directory = Directory.GetParent(directory)?.FullName
        )
        {
            if (File.Exists(Path.Combine(directory, "Vibe.slnx")))
            {
                return directory;
            }
        }

        throw new InvalidOperationException(
            $"Could not find the repository root (Vibe.slnx) above {AppContext.BaseDirectory}."
        );
    }

    private static int FreeLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static (Process Process, Queue<string> Output, TaskCompletionSource Listening) Launch(
        string projectPath,
        string baseUrl,
        IReadOnlyDictionary<string, string> environment
    )
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            // --no-build: the run tests the build already made, and avoids output locks.
            // --no-launch-profile: launchSettings.json would pick its own port.
            Arguments =
                $"run --project \"{projectPath}\" -c Release --no-build --no-launch-profile --urls \"{baseUrl}\"",
            WorkingDirectory = RepositoryRoot(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var (key, value) in environment)
        {
            startInfo.EnvironmentVariables[key] = value;
        }

        var output = new Queue<string>();
        var listening = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var listeningLine = $"Now listening on: {baseUrl}";

        void OnLine(string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            lock (output)
            {
                output.Enqueue(line);
                while (output.Count > 40)
                {
                    output.Dequeue();
                }
            }

            if (line.Contains(listeningLine, StringComparison.OrdinalIgnoreCase))
            {
                listening.TrySetResult();
            }
        }

        var process =
            Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                $"Failed to start dotnet run for {projectPath}."
            );
        process.OutputDataReceived += (_, e) => OnLine(e.Data);
        process.ErrorDataReceived += (_, e) => OnLine(e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return (process, output, listening);
    }

    private static async Task<bool> WaitForListeningAsync(
        Process process,
        TaskCompletionSource listening,
        CancellationToken cancellationToken
    )
    {
        var exited = process.WaitForExitAsync(cancellationToken);
        var timeout = Task.Delay(StartTimeout, cancellationToken);
        var first = await Task.WhenAny(listening.Task, exited, timeout);
        return first == listening.Task && !process.HasExited;
    }

    private static async Task<bool> RespondsAsync(
        OwnedServer server,
        CancellationToken cancellationToken
    )
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTimeOffset.UtcNow + StartTimeout;
        while (DateTimeOffset.UtcNow < deadline && !server.HasExited)
        {
            try
            {
                using var response = await http.GetAsync(server.BaseUrl, cancellationToken);
                if ((int)response.StatusCode < 500)
                {
                    return true;
                }
            }
            catch (HttpRequestException)
            {
                // Listening, but not serving yet.
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // A slow first request.
            }

            await Task.Delay(250, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Stops every server still running when the test process ends, passed or failed. A server
    /// left behind by a test host that was killed outright no longer blocks anything: the next
    /// run picks its own free port.
    /// </summary>
    private static class ServerRegistry
    {
        private static readonly HashSet<OwnedServer> Running = [];

        static ServerRegistry()
        {
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                OwnedServer[] servers;
                lock (Running)
                {
                    servers = [.. Running];
                    Running.Clear();
                }

                foreach (var server in servers)
                {
                    server.KillNow();
                }
            };
        }

        public static void Track(OwnedServer server)
        {
            lock (Running)
            {
                Running.Add(server);
            }
        }

        public static void Forget(OwnedServer server)
        {
            lock (Running)
            {
                Running.Remove(server);
            }
        }
    }
}
