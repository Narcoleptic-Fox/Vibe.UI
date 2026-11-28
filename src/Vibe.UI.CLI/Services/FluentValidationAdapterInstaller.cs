using System.Reflection;

namespace Vibe.UI.CLI.Services;

/// <summary>
/// The FluentValidation adapter, vendored the way <c>vibe init</c> copies Metadata/: verbatim, so
/// it keeps its <c>Vibe.UI.FluentValidation</c> and <c>Vibe.UI</c> namespaces and sits beside the
/// infrastructure it builds on. Besides its files (<see cref="Plan"/>) it needs the
/// FluentValidation package reference in the project the files land in, and the Razor using for
/// <c>&lt;FluentValidator /&gt;</c> (<see cref="PlanProjectChanges"/>).
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

    /// <summary>
    /// What adding the adapter changes in the project besides its own files. Worked out before
    /// anything is written, so a project the CLI cannot change correctly is refused untouched.
    /// </summary>
    /// <param name="ProjectFile">The project the adapter compiles in.</param>
    /// <param name="CentralPackageFile">
    /// The Directory.Packages.props that holds the version when the project uses central package
    /// management; otherwise null and the version goes on the PackageReference.
    /// </param>
    public sealed record ProjectChanges(
        string ProjectPath,
        string ProjectFile,
        string? CentralPackageFile,
        bool AddsReference,
        bool AddsCentralVersion,
        string ImportsFile,
        bool AddsUsing
    )
    {
        /// <summary>The project files this will edit, for the confirmation list.</summary>
        public IEnumerable<string> EditedFiles()
        {
            if (AddsReference)
            {
                yield return ProjectFile;
            }

            if (AddsCentralVersion && CentralPackageFile is not null)
            {
                yield return CentralPackageFile;
            }

            if (AddsUsing)
            {
                yield return ImportsFile;
            }
        }
    }

    /// <summary>The adapter's files, from the packaged templates or this repository.</summary>
    public static IReadOnlyList<PlannedFile> Plan()
    {
        var sourceDirectory = FindSourceDirectory();
        return SourceFiles
            .Select(fileName => new PlannedFile(
                Path.Combine(sourceDirectory, fileName),
                Path.Combine(TargetDirectory, fileName)
            ))
            .ToList();
    }

    public static ProjectChanges PlanProjectChanges(string projectPath, string? projectFile)
    {
        if (projectFile is null || !File.Exists(projectFile))
        {
            throw new InvalidOperationException(
                $"No .csproj file found for {projectPath}. The FluentValidation adapter needs a "
                    + "project to add the FluentValidation package reference to."
            );
        }

        // The package half is the same for any package: see PackageReferenceEditor.
        var package = PackageReferenceEditor.Plan(projectFile, PackageId, PackageVersion);
        return new ProjectChanges(
            projectPath,
            projectFile,
            package.CentralPackageFile,
            package.AddsReference,
            package.AddsCentralVersion,
            RazorImportsService.ImportsPath(projectPath),
            RazorImportsService.MissingUsings(projectPath, [RazorUsing]).Count > 0
        );
    }

    /// <summary>
    /// Adds the package reference (and, under central package management, the package version)
    /// unless already there, so an app's own choice of version is left alone; then the Razor using.
    /// </summary>
    public static async Task ApplyProjectChangesAsync(ProjectChanges changes)
    {
        await PackageReferenceEditor.ApplyAsync(
            new PackageReferencePlan(
                changes.ProjectFile,
                changes.CentralPackageFile,
                PackageId,
                PackageVersion,
                changes.AddsReference,
                changes.AddsCentralVersion
            )
        );

        await RazorImportsService.EnsureUsingsAsync(changes.ProjectPath, [RazorUsing]);
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
}
