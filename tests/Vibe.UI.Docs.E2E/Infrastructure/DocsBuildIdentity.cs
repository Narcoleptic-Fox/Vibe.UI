using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Vibe.UI.Docs.E2E.Infrastructure;

/// <summary>
/// Whether a docs server is serving this checkout's build.
/// </summary>
/// <remarks>
/// The build writes <c>_framework/dotnet.js</c> with the boot manifest inside it, and the
/// manifest names every assembly by a hash of its content (<c>Vibe.UI.Docs.{hash}.wasm</c>). The
/// served file is compared byte for byte with the one in this checkout's build output, so a
/// server running a build of other source does not match. Another checkout of the same commit
/// normally does not match either, since its assemblies record their own build paths; when it
/// does, it is the same build. Nothing is added to the docs app for this.
/// </remarks>
internal static partial class DocsBuildIdentity
{
    internal const string StampPath = "_framework/dotnet.js";

    /// <summary>The build output the docs server runs from (dotnet run -c Release --no-build).</summary>
    internal static string LocalStampFile =>
        Path.Combine(
            OwnedServer.RepositoryRoot(),
            "samples",
            "Vibe.UI.Docs",
            "bin",
            "Release",
            "net10.0",
            "wwwroot",
            "_framework",
            "dotnet.js"
        );

    internal static async Task VerifyAsync(string baseUrl, CancellationToken cancellationToken)
    {
        var localFile = LocalStampFile;
        if (!File.Exists(localFile))
        {
            throw new InvalidOperationException(
                $"This checkout has no docs build to compare the server with: {localFile} is missing. "
                    + "Build it first: dotnet build Vibe.slnx -c Release."
            );
        }

        var expected = await File.ReadAllBytesAsync(localFile, cancellationToken);
        var url = $"{baseUrl}/{StampPath}";

        byte[] served;
        using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
        {
            using var response = await http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"The docs server at {baseUrl} (port {new Uri(baseUrl).Port}) is not this checkout's docs build: "
                        + $"GET {url} returned {(int)response.StatusCode}."
                );
            }

            served = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }

        if (SHA256.HashData(served).AsSpan().SequenceEqual(SHA256.HashData(expected)))
        {
            return;
        }

        throw new InvalidOperationException(
            $"The docs server at {baseUrl} (port {new Uri(baseUrl).Port}) is not serving this checkout's build, "
                + "so the tests would run against someone else's pages. "
                + $"It serves {Describe(served)}; this checkout's {localFile} has {Describe(expected)}. "
                + "Rebuild (dotnet build Vibe.slnx -c Release), or, without DOCS_BASE_URL, let the run start its own server."
        );
    }

    private static string Describe(byte[] content)
    {
        var text = System.Text.Encoding.UTF8.GetString(content);
        var assembly = DocsAssembly().Match(text);
        var hash = Convert.ToHexString(SHA256.HashData(content))[..12];
        return assembly.Success
            ? $"{assembly.Value} (dotnet.js {hash})"
            : $"no docs assembly (dotnet.js {hash}, {content.Length} bytes)";
    }

    [GeneratedRegex(@"Vibe\.UI\.Docs\.[a-z0-9]+\.wasm")]
    private static partial Regex DocsAssembly();
}
