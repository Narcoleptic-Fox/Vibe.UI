using System.Diagnostics;
using System.Net.Http;

namespace Vibe.UI.Docs.E2E.Infrastructure;

internal static class DocsServerManager
{
    private static readonly object _lock = new();
    private static Process? _process;
    private static int _activeUsers;

    internal static string DefaultBaseUrl => "http://localhost:5000";

    internal static async Task AcquireAsync(string baseUrl, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _activeUsers++;
        }

        // If the caller is not using the default URL, assume an external server is managed elsewhere.
        if (!string.Equals(baseUrl.TrimEnd('/'), DefaultBaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await EnsureStartedAsync(cancellationToken);
        await WaitForReadyAsync(baseUrl, cancellationToken);
    }

    internal static void Release(string baseUrl)
    {
        var shouldStop = false;

        lock (_lock)
        {
            _activeUsers = Math.Max(0, _activeUsers - 1);

            if (_activeUsers == 0
                && string.Equals(baseUrl.TrimEnd('/'), DefaultBaseUrl, StringComparison.OrdinalIgnoreCase)
                && _process != null
                && !_process.HasExited)
            {
                shouldStop = true;
            }
        }

        if (!shouldStop)
        {
            return;
        }

        try
        {
            _process?.Kill(entireProcessTree: true);
        }
        catch
        {
            // Ignore shutdown failures; test runner is exiting anyway.
        }
    }

    private static Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_process != null && !_process.HasExited)
            {
                return Task.CompletedTask;
            }

            // Start the docs site using the Blazor WASM dev server.
            // Use --no-build to avoid concurrent build output locks during test runs.
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                // Force a stable port for tests (ignore launchSettings.json which uses a random dev port).
                Arguments = "run --project \"samples/Vibe.UI.Docs/Vibe.UI.Docs.csproj\" -c Release --no-build --no-launch-profile --urls \"http://localhost:5000\"",
                WorkingDirectory = GetRepoRoot(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            startInfo.EnvironmentVariables["DOTNET_ENVIRONMENT"] = "Development";

            _process = Process.Start(startInfo);
            if (_process == null)
            {
                throw new InvalidOperationException("Failed to start docs server process.");
            }

            _process.OutputDataReceived += (_, __) => { };
            _process.ErrorDataReceived += (_, __) => { };
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            AppDomain.CurrentDomain.ProcessExit += (_, __) =>
            {
                try { _process?.Kill(entireProcessTree: true); } catch { }
            };
        }

        return Task.CompletedTask;
    }

    private static async Task WaitForReadyAsync(string baseUrl, CancellationToken cancellationToken)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        var deadline = DateTimeOffset.UtcNow.AddSeconds(60);
        Exception? last = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var response = await http.GetAsync(baseUrl, cancellationToken);
                if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 500)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                last = ex;
            }

            await Task.Delay(500, cancellationToken);
        }

        throw new TimeoutException($"Docs server did not become ready at {baseUrl} within 60s.", last);
    }

    private static string GetRepoRoot()
    {
        // Tests execute under the repo; walk up until we find the solution file.
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Vibe.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        // Fallback: use current directory.
        return Directory.GetCurrentDirectory();
    }
}
