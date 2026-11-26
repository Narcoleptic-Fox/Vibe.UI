using System.Xml.Linq;

namespace Vibe.UI.CLI.Services;

/// <summary>
/// Whether a project uses NuGet central package management, where package versions live in
/// Directory.Packages.props and a PackageReference may not carry a Version.
/// </summary>
public static class CentralPackageManagement
{
    public const string PackagesFileName = "Directory.Packages.props";

    private const string BuildPropsFileName = "Directory.Build.props";
    private const string Property = "ManagePackageVersionsCentrally";

    /// <summary>
    /// The project's own setting wins. Otherwise the nearest Directory.Packages.props and every
    /// Directory.Build.props above the project are read, the places the property is normally set.
    /// This reads the files rather than evaluating MSBuild, so a value that comes from an imported
    /// file or a condition is not seen.
    /// </summary>
    public static bool IsEnabled(string projectFile)
    {
        var own = ReadSetting(projectFile);
        if (own is not null)
        {
            return own.Value;
        }

        var packagesFile = FindPackagesFile(projectFile);
        if (packagesFile is not null && ReadSetting(packagesFile) is { } fromPackages)
        {
            return fromPackages;
        }

        foreach (var directory in DirectoriesAbove(projectFile))
        {
            var buildProps = Path.Combine(directory, BuildPropsFileName);
            if (File.Exists(buildProps) && ReadSetting(buildProps) is { } fromBuild)
            {
                return fromBuild;
            }
        }

        return false;
    }

    /// <summary>The nearest Directory.Packages.props in the project's directory or above it.</summary>
    public static string? FindPackagesFile(string projectFile) =>
        DirectoriesAbove(projectFile)
            .Select(directory => Path.Combine(directory, PackagesFileName))
            .FirstOrDefault(File.Exists);

    private static IEnumerable<string> DirectoriesAbove(string projectFile)
    {
        for (
            var directory = new FileInfo(Path.GetFullPath(projectFile)).Directory;
            directory is not null;
            directory = directory.Parent
        )
        {
            yield return directory.FullName;
        }
    }

    private static bool? ReadSetting(string file)
    {
        var root = XDocument.Load(file).Root;
        var value = root?.Descendants(root.GetDefaultNamespace() + Property).LastOrDefault()?.Value;
        return value is null
            ? null
            : value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
    }
}
