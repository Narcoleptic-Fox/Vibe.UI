using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Vibe.UI.CLI.Services;

/// <summary>
/// What giving a project one package reference changes. Worked out before anything is written,
/// so a project the CLI cannot change correctly is refused untouched.
/// </summary>
/// <param name="ProjectFile">The project that gets the reference.</param>
/// <param name="CentralPackageFile">
/// The Directory.Packages.props that holds the version when the project uses central package
/// management; otherwise null and the version goes on the PackageReference.
/// </param>
/// <param name="PackageId">The package.</param>
/// <param name="Version">The version written when the project names none of its own.</param>
/// <param name="AddsReference">Whether the project has no reference to the package yet.</param>
/// <param name="AddsCentralVersion">Whether the central file has no version for it yet.</param>
public sealed record PackageReferencePlan(
    string ProjectFile,
    string? CentralPackageFile,
    string PackageId,
    string Version,
    bool AddsReference,
    bool AddsCentralVersion
)
{
    /// <summary>Whether applying this plan writes to any file.</summary>
    public bool ChangesAnything => AddsReference || AddsCentralVersion;

    /// <summary>The files applying this plan edits.</summary>
    public IEnumerable<string> EditedFiles()
    {
        if (AddsCentralVersion && CentralPackageFile is not null)
        {
            yield return CentralPackageFile;
        }

        if (AddsReference)
        {
            yield return ProjectFile;
        }
    }
}

/// <summary>
/// Adds a package reference to a project the way <c>dotnet add package</c> does, without running
/// a restore: on the PackageReference, or in Directory.Packages.props under central package
/// management. A reference or version the app already has is left alone.
/// </summary>
public static class PackageReferenceEditor
{
    /// <summary>Works out what a reference to <paramref name="packageId"/> would change.</summary>
    /// <exception cref="InvalidOperationException">
    /// The project uses central package management and no Directory.Packages.props was found.
    /// </exception>
    public static PackageReferencePlan Plan(string projectFile, string packageId, string version)
    {
        var hasReference = HasItem(projectFile, "PackageReference", packageId);
        if (!CentralPackageManagement.IsEnabled(projectFile))
        {
            return new PackageReferencePlan(
                projectFile,
                null,
                packageId,
                version,
                !hasReference,
                false
            );
        }

        // Central package management rejects a Version on a PackageReference. The version goes
        // to Directory.Packages.props instead, as `dotnet add package` does.
        var packagesFile =
            CentralPackageManagement.FindPackagesFile(projectFile)
            ?? throw new InvalidOperationException(
                $"{Path.GetFileName(projectFile)} uses central package management, but no "
                    + "Directory.Packages.props was found in its directory or above it. Add "
                    + $"<PackageVersion Include=\"{packageId}\" Version=\"{version}\" /> to the "
                    + "file that holds your package versions, and "
                    + $"<PackageReference Include=\"{packageId}\" /> to {Path.GetFileName(projectFile)}."
            );

        return new PackageReferencePlan(
            projectFile,
            packagesFile,
            packageId,
            version,
            !hasReference,
            !HasItem(packagesFile, "PackageVersion", packageId)
        );
    }

    /// <summary>Writes what <paramref name="plan"/> says is missing, and nothing else.</summary>
    public static async Task ApplyAsync(PackageReferencePlan plan)
    {
        if (plan.AddsCentralVersion && plan.CentralPackageFile is not null)
        {
            await AddItemAsync(
                plan.CentralPackageFile,
                "PackageVersion",
                plan.PackageId,
                plan.Version
            );
        }

        if (plan.AddsReference)
        {
            await AddItemAsync(
                plan.ProjectFile,
                "PackageReference",
                plan.PackageId,
                plan.CentralPackageFile is null ? plan.Version : null
            );
        }
    }

    private static bool HasItem(string file, string itemName, string packageId)
    {
        var root = XDocument.Load(file).Root;
        if (root is null)
        {
            return false;
        }

        return root.Descendants(root.GetDefaultNamespace() + itemName)
            .Any(item =>
                string.Equals(
                    item.Attribute("Include")?.Value,
                    packageId,
                    StringComparison.OrdinalIgnoreCase
                )
            );
    }

    /// <summary>
    /// Adds <c>&lt;itemName Include="…" [Version="…"] /&gt;</c> after the last item of that kind,
    /// or in a new ItemGroup. The file keeps its whitespace, line endings and lack of an XML
    /// declaration: the only change is the added line.
    /// </summary>
    private static async Task AddItemAsync(
        string file,
        string itemName,
        string packageId,
        string? version
    )
    {
        var text = await File.ReadAllTextAsync(file);
        var newLine = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var document = XDocument.Parse(text, LoadOptions.PreserveWhitespace);
        var root =
            document.Root ?? throw new InvalidOperationException($"{file} has no root element.");
        var ns = root.GetDefaultNamespace();

        var item = new XElement(ns + itemName, new XAttribute("Include", packageId));
        if (version is not null)
        {
            item.Add(new XAttribute("Version", version));
        }

        var lastItem = root.Elements(ns + "ItemGroup")
            .SelectMany(group => group.Elements(ns + itemName))
            .LastOrDefault();
        if (lastItem is not null)
        {
            var indentation = lastItem.PreviousNode is XText leading
                ? leading.Value[(leading.Value.LastIndexOf('\n') + 1)..]
                : "    ";
            lastItem.AddAfterSelf(new XText(newLine + indentation), item);
        }
        else
        {
            // Before the closing tag, after whatever whitespace precedes it.
            root.Add(
                new XText("  "),
                new XElement(
                    ns + "ItemGroup",
                    new XText(newLine + "    "),
                    item,
                    new XText(newLine + "  ")
                ),
                new XText(newLine)
            );
        }

        var settings = new XmlWriterSettings
        {
            Async = true,
            OmitXmlDeclaration = document.Declaration is null,
            NewLineHandling = NewLineHandling.None,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };
        await using (var writer = XmlWriter.Create(file, settings))
        {
            await document.SaveAsync(writer, CancellationToken.None);
        }
    }
}
