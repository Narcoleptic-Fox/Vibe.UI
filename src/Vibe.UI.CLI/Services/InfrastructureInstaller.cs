namespace Vibe.UI.CLI.Services;

/// <summary>
/// One file the CLI copies from its templates into a project.
/// </summary>
/// <param name="Source">Absolute path of the template file.</param>
/// <param name="Target">Path relative to the project directory.</param>
public sealed record PlannedFile(string Source, string Target);

/// <summary>
/// Which optional parts of the infrastructure a project has. <c>vibe init</c> takes them from its
/// options; <c>vibe update --infrastructure</c> reads them from what the project already holds.
/// </summary>
public sealed record InfrastructureSelection(
    bool Themes,
    bool Charts,
    bool FluentValidation,
    bool IncludeBaseStylesheet
);

/// <summary>
/// The list of infrastructure files <c>vibe init</c> copies, and the copy itself. Init and
/// <c>vibe update --infrastructure</c> both take their list from <see cref="Plan"/>, so a
/// refreshed project has the same files as a freshly initialised one.
/// </summary>
public static class InfrastructureInstaller
{
    /// <summary>
    /// Holds the colour scheme init appends, so only init writes it.
    /// </summary>
    public const string BaseStylesheet = "vibe-base.css";

    private const string ChartScript = "vibe-chart.js";
    private const string ThemeScript = "vibe-theme.js";
    private const string ChartDataBuilder = "ChartDataBuilder.cs";

    public static IReadOnlyList<PlannedFile> Plan(
        string templatePath,
        InfrastructureSelection selection
    )
    {
        var infrastructure = Path.Combine(templatePath, "Infrastructure");
        if (!Directory.Exists(infrastructure))
        {
            throw new DirectoryNotFoundException(
                $"Infrastructure templates not found at: {infrastructure}"
            );
        }

        var vibe = "Vibe";
        var files = new List<PlannedFile>();

        // Base classes, the types components share (FormFieldContext, DialogContext), theme
        // options, and property metadata, which ServiceCollectionExtensions registers.
        AddDirectory(files, Path.Combine(infrastructure, "Base"), Path.Combine(vibe, "Base"));
        AddDirectory(
            files,
            Path.Combine(infrastructure, "Components"),
            Path.Combine(vibe, "Components")
        );
        AddDirectory(
            files,
            Path.Combine(infrastructure, "Configuration"),
            Path.Combine(vibe, "Configuration")
        );
        AddDirectory(
            files,
            Path.Combine(infrastructure, "Metadata"),
            Path.Combine(vibe, "Metadata")
        );

        var services = Path.Combine(infrastructure, "Services");
        var targetServices = Path.Combine(vibe, "Services");
        foreach (var subService in new[] { "Dialog", "Theme", "Toast" })
        {
            AddDirectory(
                files,
                Path.Combine(services, subService),
                Path.Combine(targetServices, subService)
            );
        }

        // Helpers that bring no component dependencies with them.
        foreach (
            var helper in new[] { "LucideIcons.cs", "FormValidators.cs", "DataTableExporter.cs" }
        )
        {
            AddFile(files, Path.Combine(services, helper), Path.Combine(targetServices, helper));
        }

        // Optional: ChartDataBuilder depends on chart component types.
        if (selection.Charts)
        {
            AddFile(
                files,
                Path.Combine(services, ChartDataBuilder),
                Path.Combine(targetServices, ChartDataBuilder)
            );
        }

        AddDirectory(files, Path.Combine(infrastructure, "Enums"), Path.Combine(vibe, "Enums"));

        if (selection.Themes)
        {
            AddDirectory(
                files,
                Path.Combine(infrastructure, "Themes"),
                Path.Combine(vibe, "Themes")
            );
        }

        AddFile(
            files,
            Path.Combine(infrastructure, "ServiceCollectionExtensions.cs"),
            Path.Combine(vibe, "ServiceCollectionExtensions.cs")
        );

        if (selection.FluentValidation)
        {
            files.AddRange(FluentValidationAdapterInstaller.Plan());
        }

        // CSS foundation. vibe-preflight.css is copied so it can be opted into, but it is not
        // linked by default (see ProjectSetupGuidance) because it restyles native HTML.
        var css = Path.Combine(templatePath, "wwwroot", "css");
        var targetCss = Path.Combine("wwwroot", "css");
        var stylesheets = selection.IncludeBaseStylesheet
            ? new[] { BaseStylesheet, "vibe-preflight.css", "vibe-utilities.css" }
            : new[] { "vibe-preflight.css", "vibe-utilities.css" };
        foreach (var stylesheet in stylesheets)
        {
            AddFile(files, Path.Combine(css, stylesheet), Path.Combine(targetCss, stylesheet));
        }

        // Interop scripts, without the chart and theme scripts unless those parts are selected.
        var scripts = Path.Combine(templatePath, "wwwroot", "js");
        if (Directory.Exists(scripts))
        {
            foreach (
                var script in Directory
                    .EnumerateFiles(scripts, "*.js", SearchOption.TopDirectoryOnly)
                    .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            )
            {
                var fileName = Path.GetFileName(script);
                var excluded =
                    (
                        !selection.Charts
                        && fileName.Equals(ChartScript, StringComparison.OrdinalIgnoreCase)
                    )
                    || (
                        !selection.Themes
                        && fileName.Equals(ThemeScript, StringComparison.OrdinalIgnoreCase)
                    );
                if (!excluded)
                {
                    files.Add(new PlannedFile(script, Path.Combine("wwwroot", "js", fileName)));
                }
            }
        }

        return files;
    }

    /// <summary>
    /// What an initialised project already has, so a refresh keeps the choices init made.
    /// </summary>
    public static InfrastructureSelection SelectionOf(
        string projectPath,
        bool addFluentValidation
    ) =>
        new(
            Themes: Directory.Exists(Path.Combine(projectPath, "Vibe", "Themes"))
                || File.Exists(Path.Combine(projectPath, "wwwroot", "js", ThemeScript)),
            Charts: File.Exists(Path.Combine(projectPath, "Vibe", "Services", ChartDataBuilder))
                || File.Exists(Path.Combine(projectPath, "wwwroot", "js", ChartScript)),
            FluentValidation: addFluentValidation
                || Directory.Exists(
                    Path.Combine(projectPath, FluentValidationAdapterInstaller.TargetDirectory)
                ),
            IncludeBaseStylesheet: false
        );

    public static async Task CopyAsync(string projectPath, IEnumerable<PlannedFile> files)
    {
        foreach (var file in files)
        {
            var target = Path.Combine(projectPath, file.Target);
            var directory = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(target, await File.ReadAllTextAsync(file.Source));
        }
    }

    public static string FindTemplatePath()
    {
        var assemblyLocation =
            Path.GetDirectoryName(typeof(InfrastructureInstaller).Assembly.Location)
            ?? AppContext.BaseDirectory;

        var possiblePaths = new[]
        {
            // Development: relative to the CLI (or test) project's bin/<config>/<tfm>.
            Path.GetFullPath(Path.Combine(assemblyLocation, "..", "..", "..", "Templates")),
            // Packaged next to the executable.
            Path.Combine(assemblyLocation, "Templates"),
            // Installed tool: the package root, three levels above tools/net10.0/any. The same
            // relative path as development, so it is covered by the first entry.
            Path.Combine(AppContext.BaseDirectory, "Templates"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Templates")),
        };

        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                return path;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not find Templates directory. Please ensure Vibe.UI.CLI is installed correctly. "
                + $"Searched locations: {string.Join(", ", possiblePaths)}"
        );
    }

    private static void AddDirectory(
        List<PlannedFile> files,
        string sourceDirectory,
        string targetDirectory
    )
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return;
        }

        foreach (
            var file in Directory
                .GetFiles(sourceDirectory, "*", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
        )
        {
            files.Add(
                new PlannedFile(
                    file,
                    Path.Combine(targetDirectory, Path.GetRelativePath(sourceDirectory, file))
                )
            );
        }
    }

    private static void AddFile(List<PlannedFile> files, string source, string target)
    {
        if (File.Exists(source))
        {
            files.Add(new PlannedFile(source, target));
        }
    }
}
