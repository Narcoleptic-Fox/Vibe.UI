using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Vibe.UI.CLI.Services;

/// <summary>
/// Copies the FluentValidation adapter into a vendored project, the way <c>vibe init</c> copies
/// Metadata/: verbatim, so it keeps its <c>Vibe.UI.FluentValidation</c> and <c>Vibe.UI</c>
/// namespaces and sits beside the infrastructure it builds on. It also adds the FluentValidation
/// package reference to the project the files land in, and the Razor using for
/// <c>&lt;FluentValidator /&gt;</c>.
/// </summary>
public static class FluentValidationAdapterInstaller
{
    /// <summary>The adapter's source files. A test keeps this equal to src/Vibe.UI.FluentValidation.</summary>
    public static readonly IReadOnlyList<string> SourceFiles =
    [
        "FluentValidationMetadataProvider.cs",
        "FluentValidationServiceCollectionExtensions.cs",
        "FluentValidator.cs",
        "ValidatorDescriptorSource.cs",
    ];

    public const string PackageId = "FluentValidation";

    public const string RazorUsing = "@using global::Vibe.UI.FluentValidation";

    /// <summary>Where the adapter goes, relative to the project.</summary>
    public static readonly string TargetDirectory = Path.Combine("Vibe", "FluentValidation");

    /// <summary>
    /// The FluentValidation version the adapter is built with, recorded at build time from
    /// Vibe.UI.FluentValidation.csproj.
    /// </summary>
    public static string PackageVersion { get; } =
        typeof(FluentValidationAdapterInstaller)
            .Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "FluentValidationVersion")
            ?.Value
        ?? throw new InvalidOperationException(
            "This build of the Vibe.UI CLI does not record a FluentValidation version."
        );

    public sealed record Result(
        string AdapterDirectory,
        string ProjectFile,
        string PackageVersion,
        bool PackageReferenceAdded
    );

    public static async Task<Result> InstallAsync(string projectPath, string? projectFile)
    {
        // Fail before copying anything: a project with the files and no package does not build.
        if (projectFile is null || !File.Exists(projectFile))
        {
            throw new InvalidOperationException(
                $"No .csproj file found for {projectPath}. The FluentValidation adapter needs a "
                    + "project to add the FluentValidation package reference to."
            );
        }

        var sourceDirectory = FindSourceDirectory();
        var targetDirectory = Path.Combine(projectPath, TargetDirectory);
        Directory.CreateDirectory(targetDirectory);

        foreach (var fileName in SourceFiles)
        {
            await File.WriteAllTextAsync(
                Path.Combine(targetDirectory, fileName),
                await File.ReadAllTextAsync(Path.Combine(sourceDirectory, fileName))
            );
        }

        var added = await AddPackageReferenceAsync(projectFile);
        await RazorImportsService.EnsureUsingsAsync(projectPath, [RazorUsing]);

        return new Result(targetDirectory, projectFile, PackageVersion, added);
    }

    /// <summary>
    /// The packaged copy (Templates/Infrastructure/FluentValidation) or, when the CLI runs from
    /// this repository, the adapter project itself. A directory counts only if it holds every
    /// file, so a package that lost one fails here instead of producing a project that does not
    /// compile.
    /// </summary>
    private static string FindSourceDirectory()
    {
        var assemblyLocation =
            Path.GetDirectoryName(typeof(FluentValidationAdapterInstaller).Assembly.Location)
            ?? AppContext.BaseDirectory;

        var candidates = new[]
        {
            // Installed tool: Templates/ in the package root, three levels above tools/net10.0/any.
            Path.Combine(
                assemblyLocation,
                "..",
                "..",
                "..",
                "Templates",
                "Infrastructure",
                "FluentValidation"
            ),
            Path.Combine(assemblyLocation, "Templates", "Infrastructure", "FluentValidation"),
            // Running from this repository: src/Vibe.UI.CLI/bin/<config>/net10.0 or
            // tests/Vibe.UI.CLI.Tests/bin/<config>/net10.0.
            Path.Combine(
                assemblyLocation,
                "..",
                "..",
                "..",
                "..",
                "..",
                "src",
                "Vibe.UI.FluentValidation"
            ),
        }.Select(Path.GetFullPath).ToArray();

        foreach (var candidate in candidates)
        {
            if (!Directory.Exists(candidate))
            {
                continue;
            }

            var missing = SourceFiles
                .Where(fileName => !File.Exists(Path.Combine(candidate, fileName)))
                .ToArray();
            if (missing.Length > 0)
            {
                throw new FileNotFoundException(
                    $"The FluentValidation adapter at {candidate} is incomplete. Missing: "
                        + string.Join(", ", missing)
                        + ". Reinstall the Vibe.UI CLI."
                );
            }

            return candidate;
        }

        throw new DirectoryNotFoundException(
            "Could not find the FluentValidation adapter sources. Searched: "
                + string.Join(", ", candidates)
        );
    }

    /// <summary>
    /// Adds a FluentValidation package reference unless the project already has one, in which
    /// case the app's choice of version is left alone.
    /// </summary>
    private static async Task<bool> AddPackageReferenceAsync(string projectFile)
    {
        var text = await File.ReadAllTextAsync(projectFile);
        var newLine = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var document = XDocument.Parse(text, LoadOptions.PreserveWhitespace);
        var root =
            document.Root
            ?? throw new InvalidOperationException($"{projectFile} has no root element.");
        var ns = root.GetDefaultNamespace();

        var existing = root.Descendants(ns + "PackageReference")
            .Any(reference =>
                string.Equals(
                    reference.Attribute("Include")?.Value,
                    PackageId,
                    StringComparison.OrdinalIgnoreCase
                )
            );
        if (existing)
        {
            return false;
        }

        var reference = new XElement(
            ns + "PackageReference",
            new XAttribute("Include", PackageId),
            new XAttribute("Version", PackageVersion)
        );

        // The file is loaded with its whitespace kept, so the rest of it is left as the app wrote
        // it. The new line copies the indentation of the reference it follows.
        var lastReference = root.Elements(ns + "ItemGroup")
            .SelectMany(group => group.Elements(ns + "PackageReference"))
            .LastOrDefault();
        if (lastReference is not null)
        {
            var indentation = lastReference.PreviousNode is XText leading
                ? leading.Value[(leading.Value.LastIndexOf('\n') + 1)..]
                : "    ";
            lastReference.AddAfterSelf(new XText(newLine + indentation), reference);
        }
        else
        {
            // Before the closing tag, after whatever whitespace precedes it.
            root.Add(
                new XText("  "),
                new XElement(
                    ns + "ItemGroup",
                    new XText(newLine + "    "),
                    reference,
                    new XText(newLine + "  ")
                ),
                new XText(newLine)
            );
        }

        // Written back without an XML declaration the file did not have, and with its own line
        // endings: the only change is the added reference.
        var settings = new XmlWriterSettings
        {
            Async = true,
            OmitXmlDeclaration = document.Declaration is null,
            NewLineHandling = NewLineHandling.None,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };
        await using (var writer = XmlWriter.Create(projectFile, settings))
        {
            await document.SaveAsync(writer, CancellationToken.None);
        }

        return true;
    }
}
