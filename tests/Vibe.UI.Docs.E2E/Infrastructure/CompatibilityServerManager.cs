namespace Vibe.UI.Docs.E2E.Infrastructure;

/// <summary>
/// The compatibility sample apps the compatibility tests run against.
/// </summary>
/// <remarks>
/// Without <c>VIBE_STANDALONE_BASE_URL</c> / <c>VIBE_WEBAPP_BASE_URL</c>, each test starts the app
/// from this checkout on a free port (see <see cref="OwnedServer"/>) and stops it afterwards. It
/// used to start it on a fixed port (5101, 5102) and use whatever answered there, which could be
/// another checkout's app. With the variable set, the caller owns the server, as before.
/// </remarks>
internal static class CompatibilityServerManager
{
    private static readonly object Lock = new();
    private static readonly Dictionary<CompatibilityApp, ServerState> Servers = new();

    /// <summary>Starts the app (or uses the configured URL) and returns its URL, without a trailing slash.</summary>
    internal static async Task<string> AcquireAsync(
        CompatibilityApp app,
        CancellationToken cancellationToken
    )
    {
        var configured = Environment.GetEnvironmentVariable(GetEnvironmentVariableName(app));
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/');
        }

        var state = GetOrCreateState(app);
        await state.LifecycleGate.WaitAsync(cancellationToken);
        try
        {
            if (state.Server is null || state.Server.HasExited)
            {
                state.Server = await OwnedServer.StartAsync(
                    $"{app} compatibility server",
                    GetProjectPath(app),
                    CreateEnvironment(app),
                    cancellationToken
                );
            }

            state.ActiveUsers += 1;
            return state.Server.BaseUrl;
        }
        finally
        {
            state.LifecycleGate.Release();
        }
    }

    internal static async Task ReleaseAsync(CompatibilityApp app)
    {
        if (
            !string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(GetEnvironmentVariableName(app))
            )
        )
        {
            return;
        }

        ServerState? state;
        lock (Lock)
        {
            if (!Servers.TryGetValue(app, out state))
            {
                return;
            }
        }

        await state.LifecycleGate.WaitAsync();
        try
        {
            state.ActiveUsers = Math.Max(0, state.ActiveUsers - 1);
            if (state.ActiveUsers == 0 && state.Server is not null)
            {
                await state.Server.StopAsync();
                state.Server = null;
            }
        }
        finally
        {
            state.LifecycleGate.Release();
        }
    }

    private static ServerState GetOrCreateState(CompatibilityApp app)
    {
        lock (Lock)
        {
            if (!Servers.TryGetValue(app, out var state))
            {
                state = new ServerState();
                Servers[app] = state;
            }

            return state;
        }
    }

    private static string GetEnvironmentVariableName(CompatibilityApp app)
    {
        return app switch
        {
            CompatibilityApp.StandaloneClient => "VIBE_STANDALONE_BASE_URL",
            CompatibilityApp.WebApp => "VIBE_WEBAPP_BASE_URL",
            _ => throw new ArgumentOutOfRangeException(nameof(app), app, null),
        };
    }

    private static Dictionary<string, string> CreateEnvironment(CompatibilityApp app)
    {
        var profileRoot = Path.Combine(
            Path.GetTempPath(),
            "Vibe.UI.CompatibilityE2E",
            Environment.ProcessId.ToString(),
            app.ToString()
        );
        var localAppDataPath = Path.Combine(profileRoot, "LocalAppData");
        var appDataPath = Path.Combine(profileRoot, "AppData");
        var dataProtectionPath = Path.Combine(profileRoot, "DataProtectionKeys");

        Directory.CreateDirectory(localAppDataPath);
        Directory.CreateDirectory(appDataPath);
        Directory.CreateDirectory(dataProtectionPath);

        return new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["DOTNET_ENVIRONMENT"] = "Development",
            ["Logging__EventLog__LogLevel__Default"] = "None",
            ["LOCALAPPDATA"] = localAppDataPath,
            ["APPDATA"] = appDataPath,
            ["VIBE_COMPATIBILITY_DATA_PROTECTION_PATH"] = dataProtectionPath,
        };
    }

    private static string GetProjectPath(CompatibilityApp app)
    {
        return app switch
        {
            CompatibilityApp.StandaloneClient =>
                "samples/Vibe.UI.Compatibility.StandaloneClient/Vibe.UI.Compatibility.StandaloneClient.csproj",
            CompatibilityApp.WebApp =>
                "samples/Vibe.UI.Compatibility.WebApp/Vibe.UI.Compatibility.WebApp/Vibe.UI.Compatibility.WebApp.csproj",
            _ => throw new ArgumentOutOfRangeException(nameof(app), app, null),
        };
    }

    private sealed class ServerState
    {
        // A new test must not reuse a process while the previous test is still stopping it.
        internal SemaphoreSlim LifecycleGate { get; } = new(1, 1);
        internal OwnedServer? Server { get; set; }
        internal int ActiveUsers { get; set; }
    }
}
