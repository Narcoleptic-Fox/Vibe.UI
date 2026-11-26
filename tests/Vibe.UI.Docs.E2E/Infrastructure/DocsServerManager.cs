namespace Vibe.UI.Docs.E2E.Infrastructure;

/// <summary>
/// The docs site the browser tests run against.
/// </summary>
/// <remarks>
/// <para>
/// Without <c>DOCS_BASE_URL</c>, the run starts its own docs server from this checkout on a free
/// port (see <see cref="OwnedServer"/>) and stops it when the test process ends. It never uses a
/// server it did not start, whatever is listening on the old default port 5000.
/// </para>
/// <para>
/// With <c>DOCS_BASE_URL</c>, the caller owns the server, as before.
/// </para>
/// <para>
/// Either way, before the first test the server must prove it serves this checkout's build
/// (<see cref="DocsBuildIdentity"/>). The result is shared by the whole run: one failure fails
/// every test fast with the same message, instead of each one timing out on a page that is not
/// there.
/// </para>
/// </remarks>
internal static class DocsServerManager
{
    internal const string BaseUrlVariable = "DOCS_BASE_URL";
    internal const string SkipIdentityCheckVariable = "DOCS_SKIP_IDENTITY_CHECK";

    private const string ProjectPath = "samples/Vibe.UI.Docs/Vibe.UI.Docs.csproj";

    private static readonly Lazy<Task<string>> Server = new(
        () => StartOrVerifyAsync(CancellationToken.None),
        LazyThreadSafetyMode.ExecutionAndPublication
    );

    private static string? _baseUrl;

    /// <summary>The docs site's URL, without a trailing slash. Set by <see cref="AcquireAsync"/>.</summary>
    internal static string BaseUrl =>
        _baseUrl
        ?? throw new InvalidOperationException(
            $"{nameof(DocsServerManager)}.{nameof(AcquireAsync)} has not completed; the docs URL is not known yet."
        );

    internal static async Task AcquireAsync()
    {
        _baseUrl = await Server.Value;
    }

    private static async Task<string> StartOrVerifyAsync(CancellationToken cancellationToken)
    {
        var configured = Environment.GetEnvironmentVariable(BaseUrlVariable);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var external = configured.TrimEnd('/');
            await WaitForExternalAsync(external, cancellationToken);
            await CheckIdentityAsync(external, owned: false, cancellationToken);
            return external;
        }

        var server = await OwnedServer.StartAsync(
            "Docs server",
            ProjectPath,
            new Dictionary<string, string> { ["DOTNET_ENVIRONMENT"] = "Development" },
            cancellationToken
        );
        Console.WriteLine(
            $"Docs server for this run: {server.BaseUrl} (started from {OwnedServer.RepositoryRoot()})."
        );
        await CheckIdentityAsync(server.BaseUrl, owned: true, cancellationToken);
        return server.BaseUrl;
    }

    private static async Task CheckIdentityAsync(
        string baseUrl,
        bool owned,
        CancellationToken cancellationToken
    )
    {
        // Only for a server the caller runs, such as a published copy of this checkout, whose
        // files legitimately differ from the local build. Never honoured for a server this run
        // started: that one is always this checkout's build.
        if (
            !owned
            && bool.TryParse(
                Environment.GetEnvironmentVariable(SkipIdentityCheckVariable),
                out var skip
            )
            && skip
        )
        {
            Console.Error.WriteLine(
                $"WARNING: {SkipIdentityCheckVariable} is set, so {baseUrl} is not checked against this checkout's build."
            );
            return;
        }

        await DocsBuildIdentity.VerifyAsync(baseUrl, cancellationToken);
    }

    private static async Task WaitForExternalAsync(
        string baseUrl,
        CancellationToken cancellationToken
    )
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTimeOffset.UtcNow.AddSeconds(90);
        Exception? last = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using var response = await http.GetAsync(baseUrl, cancellationToken);
                if ((int)response.StatusCode < 500)
                {
                    return;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                last = ex;
            }

            await Task.Delay(500, cancellationToken);
        }

        throw new TimeoutException(
            $"The docs server at {baseUrl} ({BaseUrlVariable}) did not answer within 90s.",
            last
        );
    }
}
