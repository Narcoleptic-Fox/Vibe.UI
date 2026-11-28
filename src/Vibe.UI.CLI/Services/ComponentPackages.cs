using System.Reflection;

namespace Vibe.UI.CLI.Services;

/// <summary>
/// The packages a copied component needs that are not Vibe.UI's own. Vibe.UI the library
/// references them itself; a project that takes a component as source has to reference them
/// too, so <c>vibe add</c> writes the reference.
/// </summary>
public static class ComponentPackages
{
    /// <summary>Microsoft's QuickGrid, which <c>VibeGrid</c> wraps.</summary>
    public const string QuickGrid = "Microsoft.AspNetCore.Components.QuickGrid";

    private const string MetadataPrefix = "ComponentPackageVersion:";

    /// <summary>
    /// The version of <paramref name="packageId"/> that Vibe.UI is built and tested with,
    /// recorded at build time from Vibe.UI.csproj so the version written cannot drift from it.
    /// </summary>
    /// <exception cref="InvalidOperationException">This build records no version for it.</exception>
    public static string VersionOf(string packageId) =>
        typeof(ComponentPackages)
            .Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == MetadataPrefix + packageId)
            ?.Value
        ?? throw new InvalidOperationException(
            $"This build of the Vibe.UI CLI does not record a version for {packageId}."
        );
}
