using System.ComponentModel;
using System.Xml.Linq;
using Spectre.Console;
using Spectre.Console.Cli;
using Vibe.UI.CLI.Infrastructure;
using Vibe.UI.CLI.Models;
using Vibe.UI.CLI.Services;

namespace Vibe.UI.CLI.Commands;

public class InitCommand : AsyncCommand<InitCommand.Settings>
{
    // The stylesheet's own colors are what a project gets without a color scheme, and choosing
    // this name leaves them in place. Every other choice appends that palette's tokens.
    private const string DefaultBaseColor = Vibe.UI.Services.Theme.ThemePaletteData.Default;

    // The default first, then the rest of the built-in palettes from themes/*.json.
    private static readonly string[] BaseColorChoices =
    [
        DefaultBaseColor,
        .. Vibe.UI.Services.Theme.ThemePaletteData.Names.Where(name => name != DefaultBaseColor),
    ];

    public const string WithFluentValidationOption = "--with-fluentvalidation";

    public class Settings : CommandSettings
    {
        [Description("Skip confirmation prompts")]
        [CommandOption("-y|--yes")]
        [DefaultValue(false)]
        public bool SkipPrompts { get; init; }

        [Description("Project directory path")]
        [CommandOption("-p|--path")]
        [DefaultValue(".")]
        public string ProjectPath { get; init; } = ".";

        [Description("Minimal infrastructure only")]
        [CommandOption("--minimal")]
        [DefaultValue(false)]
        public bool Minimal { get; init; }

        [Description("Skip theme system")]
        [CommandOption("--no-theme")]
        [DefaultValue(false)]
        public bool NoTheme { get; init; }

        [Description("Include Chart.js support")]
        [CommandOption("--with-charts")]
        [DefaultValue(false)]
        public bool WithCharts { get; init; }

        [Description(
            "Copy the FluentValidation adapter (FluentValidator, AddVibeFluentValidation) and add the FluentValidation package reference"
        )]
        [CommandOption(WithFluentValidationOption)]
        [DefaultValue(false)]
        public bool WithFluentValidation { get; init; }

        [Description("Add Vibe.UI.CSS package reference for build-time CSS generation")]
        [CommandOption("--with-css")]
        [DefaultValue(false)]
        public bool WithCss { get; init; }
    }

    public Task<int> ExecuteAsync(CommandContext context, Settings settings) =>
        ExecuteAsync(context, settings, CancellationToken.None);

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        AnsiConsole.MarkupLine("[blue]Initializing Vibe.UI in your project...[/]\n");

        var configService = new ConfigService();
        var projectService = new ProjectService();
        var requestedProjectPath = Path.GetFullPath(settings.ProjectPath);
        var topology = await projectService.DetectProjectTopologyAsync(requestedProjectPath);
        var target = ResolveInitTarget(topology, requestedProjectPath, settings.SkipPrompts);

        AnsiConsole.WriteLine($"Detected project type: {topology.DisplayName}");
        if (!PathsEqual(requestedProjectPath, target.ProjectPath))
        {
            AnsiConsole.WriteLine($"Installing into {target.Description}: {target.ProjectPath}");
        }

        // Check if already initialized
        var vibeDir = Path.Combine(target.ProjectPath, "Vibe");
        if (Directory.Exists(vibeDir))
        {
            if (!settings.SkipPrompts)
            {
                if (
                    !AnsiConsole.Confirm(
                        "Vibe.UI infrastructure already exists. Do you want to overwrite?"
                    )
                )
                {
                    return 0;
                }
            }
        }

        // Select component directory
        var componentDir = "Components/vibe";
        if (!settings.SkipPrompts)
        {
            componentDir = AnsiConsole.Ask(
                "Where should components be installed?",
                "Components/vibe"
            );
        }
        ValidateProjectRelativePath(target.ProjectPath, componentDir, "components directory");

        // Select base color (shadcn-style)
        var baseColor = DefaultBaseColor;
        if (!settings.SkipPrompts)
        {
            baseColor = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Which [green]base color[/] would you like to use?")
                    .AddChoices(BaseColorChoices)
            );
        }

        // Create configuration
        var config = new Models.VibeConfig
        {
            ProjectType = GetConfigProjectType(topology, target.ProjectPath),
            Theme = "both",
            ComponentsDirectory = componentDir,
            CssVariables = true,
        };

        string? csprojPath = null;
        var vibeCssConfigured = false;
        FluentValidationAdapterInstaller.Result? fluentValidation = null;

        await AnsiConsole
            .Status()
            .StartAsync(
                "Setting up Vibe.UI infrastructure...",
                async ctx =>
                {
                    // Save configuration
                    await configService.SaveConfigAsync(target.ProjectPath, config);

                    ctx.Status("Copying infrastructure files...");

                    // Copy infrastructure files (includes CSS foundation files)
                    await CopyInfrastructureAsync(
                        target.ProjectPath,
                        settings.Minimal,
                        settings.NoTheme,
                        settings.WithCharts
                    );

                    ctx.Status("Updating root _Imports.razor...");
                    await RazorImportsService.EnsureVibeImportsAsync(
                        target.ProjectPath,
                        includeComponents: false
                    );

                    if (settings.WithFluentValidation)
                    {
                        ctx.Status("Copying the FluentValidation adapter...");
                        fluentValidation = await FluentValidationAdapterInstaller.InstallAsync(
                            target.ProjectPath,
                            FindCsprojFile(target.ProjectPath)
                        );
                    }

                    ctx.Status("Creating component directory...");

                    // Create components directory
                    Directory.CreateDirectory(Path.Combine(target.ProjectPath, componentDir));

                    ctx.Status("Applying color scheme to CSS...");

                    // Update vibe-base.css with selected color scheme
                    await ApplyColorSchemeAsync(target.ProjectPath, baseColor);

                    // Add Vibe.UI.CSS package reference (only if --with-css is specified)
                    // This is opt-in because the Vibe.UI.CSS package may not be published to NuGet yet
                    if (settings.WithCss)
                    {
                        ctx.Status("Configuring Vibe.UI.CSS package reference...");
                        csprojPath =
                            topology.IsBlazorWebApp
                            && !string.IsNullOrWhiteSpace(topology.ServerProjectFile)
                                ? topology.ServerProjectFile
                                : FindCsprojFile(target.ProjectPath);

                        if (csprojPath != null)
                        {
                            var scanRoot = topology.IsBlazorWebApp
                                ? "$(MSBuildProjectDirectory)/.."
                                : null;

                            vibeCssConfigured = await AddVibeCssToProjectAsync(
                                csprojPath,
                                scanRoot
                            );
                        }
                    }
                }
            );

        AnsiConsole.MarkupLine("\n[green]✓[/] Vibe.UI initialized successfully!");
        AnsiConsole.WriteLine(
            $"Infrastructure copied to {Path.Combine(target.ProjectPath, "Vibe")}"
        );
        AnsiConsole.WriteLine(
            $"CSS foundation files copied to {Path.Combine(target.ProjectPath, "wwwroot", "css")}"
        );
        AnsiConsole.WriteLine($"Color scheme: {baseColor}");

        if (fluentValidation is not null)
        {
            AnsiConsole.WriteLine(
                $"FluentValidation adapter copied to {fluentValidation.AdapterDirectory}"
            );
            AnsiConsole.MarkupLine(
                fluentValidation.PackageReferenceAdded
                    ? $"[grey]FluentValidation {fluentValidation.PackageVersion} added to {Path.GetFileName(fluentValidation.ProjectFile)}[/]"
                    : $"[grey]{Path.GetFileName(fluentValidation.ProjectFile)} already references FluentValidation; left as it is. The adapter is tested with {fluentValidation.PackageVersion}.[/]"
            );
            AnsiConsole.MarkupLine(
                "[grey]Call services.AddVibeFluentValidation() and register your validators, in the client as well as on the server.[/]"
            );
        }

        if (vibeCssConfigured)
        {
            AnsiConsole.MarkupLine(
                $"[grey]Vibe.UI.CSS configured in {Path.GetFileName(csprojPath)}[/]"
            );
        }
        else if (settings.WithCss && csprojPath == null)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]Warning:[/] No .csproj file found. Run [yellow]dotnet add package Vibe.UI.CSS --version {CliVersion.Current}[/] manually."
            );
        }

        AnsiConsole.MarkupLine($"\n[blue]Next steps:[/]");
        var nextSteps = ProjectSetupGuidance.BuildNextSteps(
            topology,
            target.ProjectPath,
            settings.WithCss
        );
        for (var index = 0; index < nextSteps.Count; index++)
        {
            AnsiConsole.WriteLine($"  {index + 1}. {nextSteps[index]}");
        }

        return 0;
    }

    private async Task CopyInfrastructureAsync(
        string projectPath,
        bool minimal,
        bool noTheme,
        bool withCharts
    )
    {
        // Get the template path (either from package or development)
        var templatePath = GetTemplatePath();
        var infrastructurePath = Path.Combine(templatePath, "Infrastructure");

        if (!Directory.Exists(infrastructurePath))
        {
            throw new DirectoryNotFoundException(
                $"Infrastructure templates not found at: {infrastructurePath}"
            );
        }

        var targetVibeDir = Path.Combine(projectPath, "Vibe");

        // Copy Base/ folder (always required - includes ClassBuilder)
        await CopyDirectoryAsync(
            Path.Combine(infrastructurePath, "Base"),
            Path.Combine(targetVibeDir, "Base")
        );

        // Copy Components/ support types (always required). These are the non-component
        // types that components share, such as FormFieldContext and DialogContext. No single
        // component owns them, so they travel with the infrastructure rather than with
        // `vibe add`.
        await CopyDirectoryAsync(
            Path.Combine(infrastructurePath, "Components"),
            Path.Combine(targetVibeDir, "Components")
        );

        // Copy Configuration/ folder (needed for theme options)
        await CopyDirectoryAsync(
            Path.Combine(infrastructurePath, "Configuration"),
            Path.Combine(targetVibeDir, "Configuration")
        );

        // Copy Metadata/ folder (always required - ServiceCollectionExtensions registers it)
        await CopyDirectoryAsync(
            Path.Combine(infrastructurePath, "Metadata"),
            Path.Combine(targetVibeDir, "Metadata")
        );

        // Copy Services/
        var servicesSourceDir = Path.Combine(infrastructurePath, "Services");
        var servicesTargetDir = Path.Combine(targetVibeDir, "Services");
        Directory.CreateDirectory(servicesTargetDir);

        // Sub-services used by the core library.
        await CopyDirectoryAsync(
            Path.Combine(servicesSourceDir, "Dialog"),
            Path.Combine(servicesTargetDir, "Dialog")
        );

        await CopyDirectoryAsync(
            Path.Combine(servicesSourceDir, "Theme"),
            Path.Combine(servicesTargetDir, "Theme")
        );

        await CopyDirectoryAsync(
            Path.Combine(servicesSourceDir, "Toast"),
            Path.Combine(servicesTargetDir, "Toast")
        );

        // Helpers (only copy the ones that don't introduce extra component dependencies by default).
        await CopyFileIfExistsAsync(
            Path.Combine(servicesSourceDir, "LucideIcons.cs"),
            Path.Combine(servicesTargetDir, "LucideIcons.cs")
        );

        await CopyFileIfExistsAsync(
            Path.Combine(servicesSourceDir, "FormValidators.cs"),
            Path.Combine(servicesTargetDir, "FormValidators.cs")
        );

        await CopyFileIfExistsAsync(
            Path.Combine(servicesSourceDir, "DataTableExporter.cs"),
            Path.Combine(servicesTargetDir, "DataTableExporter.cs")
        );

        // Charts are optional; ChartDataBuilder depends on chart component types.
        if (withCharts)
        {
            await CopyFileIfExistsAsync(
                Path.Combine(servicesSourceDir, "ChartDataBuilder.cs"),
                Path.Combine(servicesTargetDir, "ChartDataBuilder.cs")
            );
        }

        // Copy Enums/ folder (always required)
        await CopyDirectoryAsync(
            Path.Combine(infrastructurePath, "Enums"),
            Path.Combine(targetVibeDir, "Enums")
        );

        // Copy Themes/ folder (unless --no-theme)
        if (!noTheme)
        {
            await CopyDirectoryAsync(
                Path.Combine(infrastructurePath, "Themes"),
                Path.Combine(targetVibeDir, "Themes")
            );
        }

        // Copy ServiceCollectionExtensions.cs
        var serviceExtensionsSource = Path.Combine(
            infrastructurePath,
            "ServiceCollectionExtensions.cs"
        );
        var serviceExtensionsTarget = Path.Combine(targetVibeDir, "ServiceCollectionExtensions.cs");
        if (File.Exists(serviceExtensionsSource))
        {
            await File.WriteAllTextAsync(
                serviceExtensionsTarget,
                await File.ReadAllTextAsync(serviceExtensionsSource)
            );
        }

        // Copy CSS foundation files to wwwroot/css/
        var cssTemplatePath = Path.Combine(templatePath, "wwwroot", "css");
        var cssTargetPath = Path.Combine(projectPath, "wwwroot", "css");
        Directory.CreateDirectory(cssTargetPath);

        // vibe-preflight.css is copied so it is available to opt into, but it is not
        // linked by default (see ProjectSetupGuidance) because it restyles native HTML.
        var cssFiles = new[] { "vibe-base.css", "vibe-preflight.css", "vibe-utilities.css" };
        foreach (var cssFile in cssFiles)
        {
            var cssSource = Path.Combine(cssTemplatePath, cssFile);
            var cssTarget = Path.Combine(cssTargetPath, cssFile);
            if (File.Exists(cssSource))
            {
                await File.WriteAllTextAsync(cssTarget, await File.ReadAllTextAsync(cssSource));
            }
        }

        // Copy JavaScript files
        var jsTemplatePath = Path.Combine(templatePath, "wwwroot", "js");
        var jsTargetPath = Path.Combine(projectPath, "wwwroot", "js");
        Directory.CreateDirectory(jsTargetPath);

        if (Directory.Exists(jsTemplatePath))
        {
            foreach (
                var jsSource in Directory
                    .EnumerateFiles(jsTemplatePath, "*.js", SearchOption.TopDirectoryOnly)
                    .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            )
            {
                var fileName = Path.GetFileName(jsSource);

                if (
                    (
                        !withCharts
                        && fileName.Equals("vibe-chart.js", StringComparison.OrdinalIgnoreCase)
                    )
                    || (
                        noTheme
                        && fileName.Equals("vibe-theme.js", StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    continue;
                }

                await CopyFileIfExistsAsync(jsSource, Path.Combine(jsTargetPath, fileName));
            }
        }
    }

    private static async Task CopyFileIfExistsAsync(string source, string target)
    {
        if (File.Exists(source))
        {
            await File.WriteAllTextAsync(target, await File.ReadAllTextAsync(source));
        }
    }

    private async Task CopyDirectoryAsync(string sourceDir, string targetDir)
    {
        if (!Directory.Exists(sourceDir))
            return;

        Directory.CreateDirectory(targetDir);

        // Copy all files
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var targetFile = Path.Combine(targetDir, relativePath);
            var targetFileDir = Path.GetDirectoryName(targetFile);

            if (!string.IsNullOrEmpty(targetFileDir))
            {
                Directory.CreateDirectory(targetFileDir);
            }

            await File.WriteAllTextAsync(targetFile, await File.ReadAllTextAsync(file));
        }
    }

    private string GetTemplatePath()
    {
        var assemblyLocation =
            Path.GetDirectoryName(typeof(InitCommand).Assembly.Location)
            ?? AppContext.BaseDirectory;

        // Try multiple possible paths for Templates directory
        var possiblePaths = new[]
        {
            // 1. Development mode: relative to CLI project
            Path.GetFullPath(Path.Combine(assemblyLocation, "..", "..", "..", "Templates")),
            // 2. Packaged with CLI in Templates folder (adjacent to executable)
            Path.Combine(assemblyLocation, "Templates"),
            // 3. Dotnet global tool: Templates folder in package root (../../.. from tools/net10.0/any)
            Path.GetFullPath(Path.Combine(assemblyLocation, "..", "..", "..", "Templates")),
            // 4. Using AppContext.BaseDirectory
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
            $"Could not find Templates directory. Please ensure Vibe.UI.CLI is installed correctly. "
                + $"Searched locations: {string.Join(", ", possiblePaths)}"
        );
    }

    /// <summary>
    /// Applies the selected color scheme by appending color-specific CSS variables
    /// to the end of vibe-base.css. This allows the color scheme to override defaults.
    /// </summary>
    private async Task ApplyColorSchemeAsync(string projectPath, string baseColor)
    {
        var vibeBaseCssPath = Path.Combine(projectPath, "wwwroot", "css", "vibe-base.css");

        if (!File.Exists(vibeBaseCssPath))
        {
            // If vibe-base.css doesn't exist, nothing to modify
            return;
        }

        // The default keeps the stylesheet's own colors: nothing to append.
        if (baseColor == DefaultBaseColor)
        {
            return;
        }

        var colorOverrides = GetColorSchemeOverrides(baseColor);

        // Append color scheme overrides to vibe-base.css
        var existingContent = await File.ReadAllTextAsync(vibeBaseCssPath);

        // Check if overrides are already present
        if (existingContent.Contains($"/* Color scheme: {baseColor} */"))
        {
            return;
        }

        await File.AppendAllTextAsync(vibeBaseCssPath, $"\n\n{colorOverrides}");
    }

    /// <summary>
    /// Builds the CSS that switches vibe-base.css to a built-in palette. The colors come from
    /// the same generated table the theme service uses (themes/*.json), so the CLI and the
    /// library cannot disagree about what a base color looks like.
    /// </summary>
    private static string GetColorSchemeOverrides(string baseColor)
    {
        var light = Vibe.UI.Services.Theme.ThemePaletteData.Find(baseColor, dark: false);
        var dark = Vibe.UI.Services.Theme.ThemePaletteData.Find(baseColor, dark: true);
        if (light is null || dark is null)
        {
            throw new ArgumentException(
                $"Unknown base color: {baseColor}. Use one of "
                    + string.Join(", ", Vibe.UI.Services.Theme.ThemePaletteData.Names) + "."
            );
        }

        static string Block(KeyValuePair<string, string>[] tokens) =>
            string.Concat(tokens.Select(token => $"  --vibe-{token.Key}: {token.Value};\n"));

        return "/* ============================================\n"
            + $"   Color scheme: {baseColor}\n"
            + "   Generated by Vibe.UI CLI\n"
            + "   ============================================ */\n\n"
            + ":root {\n"
            + Block(light)
            + "}\n\n"
            + ".dark {\n"
            + Block(dark)
            + "}";
    }

    private static void ValidateProjectRelativePath(
        string projectPath,
        string relativePath,
        string description
    )
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException("Project path cannot be empty.", nameof(projectPath));
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException($"{description} cannot be empty.", nameof(relativePath));
        }

        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException($"{description} must be a relative path.");
        }

        var projectFullPath = Path.GetFullPath(projectPath);
        var candidateFullPath = Path.GetFullPath(Path.Combine(projectFullPath, relativePath));

        var projectPrefix =
            projectFullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!candidateFullPath.StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{description} must be within the project directory."
            );
        }
    }

    private static InitProjectTarget ResolveInitTarget(
        ProjectTopology topology,
        string requestedProjectPath,
        bool skipPrompts
    )
    {
        if (!topology.HasServerProject || !topology.HasClientProject)
        {
            return new InitProjectTarget(topology.ProjectPath ?? requestedProjectPath, "project");
        }

        const string clientChoice = "Client project (recommended for Interactive WebAssembly/Auto)";
        const string serverChoice = "Server project (static SSR or Interactive Server)";

        var selectedChoice = clientChoice;
        if (!skipPrompts)
        {
            selectedChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Where should Vibe.UI source components be installed?")
                    .AddChoices(new[] { clientChoice, serverChoice })
            );
        }

        if (
            selectedChoice == serverChoice
            && !string.IsNullOrWhiteSpace(topology.ServerProjectPath)
        )
        {
            return new InitProjectTarget(topology.ServerProjectPath, "server project");
        }

        if (!string.IsNullOrWhiteSpace(topology.ClientProjectPath))
        {
            return new InitProjectTarget(topology.ClientProjectPath, "client project");
        }

        return new InitProjectTarget(topology.ProjectPath ?? requestedProjectPath, "project");
    }

    private static string GetConfigProjectType(ProjectTopology topology, string targetProjectPath)
    {
        if (topology.IsBlazorWebApp)
        {
            if (
                !string.IsNullOrWhiteSpace(topology.ClientProjectPath)
                && PathsEqual(targetProjectPath, topology.ClientProjectPath)
            )
            {
                return "Blazor Web App Client";
            }

            return "Blazor Web App";
        }

        return topology.DisplayName;
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            StringComparison.OrdinalIgnoreCase
        );

    private sealed record InitProjectTarget(string ProjectPath, string Description);

    /// <summary>
    /// Finds the .csproj file in the project directory.
    /// </summary>
    private static string? FindCsprojFile(string projectPath)
    {
        var csprojFiles = Directory.GetFiles(
            projectPath,
            "*.csproj",
            SearchOption.TopDirectoryOnly
        );

        if (csprojFiles.Length == 0)
        {
            // Try one level up (in case user is in a subdirectory)
            var parentDir = Directory.GetParent(projectPath)?.FullName;
            if (parentDir != null)
            {
                csprojFiles = Directory.GetFiles(
                    parentDir,
                    "*.csproj",
                    SearchOption.TopDirectoryOnly
                );
            }
        }

        return csprojFiles.Length switch
        {
            0 => null,
            1 => csprojFiles[0],
            _ => csprojFiles.FirstOrDefault(f =>
                !Path.GetFileName(f).Contains("Test", StringComparison.OrdinalIgnoreCase)
            ) ?? csprojFiles[0],
        };
    }

    /// <summary>
    /// Adds the Vibe.UI.CSS package reference and build configuration to a project file.
    /// </summary>
    private static async Task<bool> AddVibeCssToProjectAsync(
        string csprojPath,
        string? scanRoot = null
    )
    {
        try
        {
            var doc = XDocument.Load(csprojPath);
            var root = doc.Root;

            if (root == null)
                return false;

            var ns = root.GetDefaultNamespace();
            var modified = false;

            // Check if Vibe.UI.CSS is already referenced
            var existingReference = root.Descendants(ns + "PackageReference")
                .FirstOrDefault(pr => pr.Attribute("Include")?.Value == "Vibe.UI.CSS");

            if (existingReference == null)
            {
                // Find or create ItemGroup for PackageReferences
                var packageItemGroup = root.Descendants(ns + "ItemGroup")
                    .FirstOrDefault(ig => ig.Elements(ns + "PackageReference").Any());

                if (packageItemGroup == null)
                {
                    packageItemGroup = new XElement(ns + "ItemGroup");
                    root.Add(packageItemGroup);
                }

                // Add Vibe.UI.CSS package reference
                var vibeCssReference = new XElement(
                    ns + "PackageReference",
                    new XAttribute("Include", "Vibe.UI.CSS"),
                    new XAttribute("Version", CliVersion.Current),
                    new XAttribute("PrivateAssets", "all")
                );

                packageItemGroup.Add(vibeCssReference);
                modified = true;
            }

            var vibeCssPropertyGroup = root.Elements(ns + "PropertyGroup")
                .FirstOrDefault(group =>
                    group
                        .Elements()
                        .Any(element =>
                            element.Name.LocalName.StartsWith("VibeCss", StringComparison.Ordinal)
                        )
                );

            if (vibeCssPropertyGroup == null)
            {
                vibeCssPropertyGroup = new XElement(
                    ns + "PropertyGroup",
                    new XComment(" Vibe.UI.CSS JIT Configuration ")
                );

                var firstPropertyGroup = root.Element(ns + "PropertyGroup");
                if (firstPropertyGroup != null)
                {
                    firstPropertyGroup.AddAfterSelf(vibeCssPropertyGroup);
                }
                else
                {
                    root.AddFirst(vibeCssPropertyGroup);
                }

                modified = true;
            }

            AddPropertyIfMissing("VibeCssEnabled", "true");
            AddPropertyIfMissing("VibeCssOutput", "wwwroot/css/Vibe.UI.CSS");
            AddPropertyIfMissing("VibeCssIncludeBase", "true");

            if (!string.IsNullOrWhiteSpace(scanRoot))
            {
                AddPropertyIfMissing("VibeCssScanRoot", scanRoot);
            }

            if (modified)
            {
                await using var stream = File.Create(csprojPath);
                await doc.SaveAsync(stream, SaveOptions.None, CancellationToken.None);
            }

            return true;

            void AddPropertyIfMissing(string propertyName, string value)
            {
                if (root.Descendants(ns + propertyName).Any())
                {
                    return;
                }

                vibeCssPropertyGroup.Add(new XElement(ns + propertyName, value));
                modified = true;
            }
        }
        catch
        {
            // If we can't modify the csproj, just return false
            return false;
        }
    }
}
