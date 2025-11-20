using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using Vibe.UI.CLI.Services;

namespace Vibe.UI.CLI.Commands;

public class InitCommand : AsyncCommand<InitCommand.Settings>
{
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
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.MarkupLine("[blue]Initializing Vibe.UI in your project...[/]\n");

        var configService = new ConfigService();

        // Check if already initialized
        var vibeDir = Path.Combine(settings.ProjectPath, "Vibe");
        if (Directory.Exists(vibeDir))
        {
            if (!settings.SkipPrompts)
            {
                if (!AnsiConsole.Confirm("Vibe.UI infrastructure already exists. Do you want to overwrite?"))
                {
                    return 0;
                }
            }
        }

        // Select component directory
        var componentDir = "Components/vibe";
        if (!settings.SkipPrompts)
        {
            componentDir = AnsiConsole.Ask("Where should components be installed?", "Components/vibe");
        }

        // Select base color (shadcn-style)
        var baseColor = "Slate";
        if (!settings.SkipPrompts)
        {
            baseColor = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Which [green]base color[/] would you like to use?")
                    .AddChoices(new[] { "Slate", "Gray", "Zinc", "Neutral", "Stone", "Blue" })
            );
        }

        // Ask for global CSS file location
        var cssFile = "wwwroot/css/app.css";
        if (!settings.SkipPrompts)
        {
            cssFile = AnsiConsole.Ask("Where is your [green]global CSS file[/]?", "wwwroot/css/app.css");
        }

        // Create configuration
        var config = new Models.VibeConfig
        {
            ProjectType = "Blazor",
            Theme = "both",
            ComponentsDirectory = componentDir,
            CssVariables = true
        };

        await AnsiConsole.Status()
            .StartAsync("Setting up Vibe.UI infrastructure...", async ctx =>
            {
                // Save configuration
                await configService.SaveConfigAsync(settings.ProjectPath, config);

                ctx.Status("Copying infrastructure files...");

                // Copy infrastructure files
                await CopyInfrastructureAsync(settings.ProjectPath, settings.Minimal, settings.NoTheme, settings.WithCharts);

                ctx.Status("Creating component directory...");

                // Create components directory
                Directory.CreateDirectory(Path.Combine(settings.ProjectPath, componentDir));

                ctx.Status("Generating CSS theme variables...");

                // Generate and append CSS variables
                await GenerateThemeCssAsync(settings.ProjectPath, cssFile, baseColor);
            });

        AnsiConsole.MarkupLine("\n[green]✓[/] Vibe.UI initialized successfully!");
        AnsiConsole.MarkupLine($"[grey]Infrastructure copied to Vibe/ folder[/]");
        AnsiConsole.MarkupLine($"[grey]Theme variables added to {cssFile}[/]");
        AnsiConsole.MarkupLine($"\n[blue]Next steps:[/]");
        AnsiConsole.MarkupLine($"  1. Add [yellow]<ThemeToggle />[/] to your layout for light/dark mode");
        AnsiConsole.MarkupLine($"  2. Run [yellow]vibe add button[/] to add your first component");
        AnsiConsole.MarkupLine($"  3. Run [yellow]vibe list[/] to see all available components");

        return 0;
    }

    private async Task CopyInfrastructureAsync(string projectPath, bool minimal, bool noTheme, bool withCharts)
    {
        // Get the template path (either from package or development)
        var templatePath = GetTemplatePath();
        var infrastructurePath = Path.Combine(templatePath, "Infrastructure");

        if (!Directory.Exists(infrastructurePath))
        {
            throw new DirectoryNotFoundException($"Infrastructure templates not found at: {infrastructurePath}");
        }

        var targetVibeDir = Path.Combine(projectPath, "Vibe");

        // Copy Base/ folder (always required)
        await CopyDirectoryAsync(
            Path.Combine(infrastructurePath, "Base"),
            Path.Combine(targetVibeDir, "Base"));

        // Copy Services/ folder
        await CopyDirectoryAsync(
            Path.Combine(infrastructurePath, "Services"),
            Path.Combine(targetVibeDir, "Services"));

        // Copy Themes/ folder (unless --no-theme)
        if (!noTheme)
        {
            await CopyDirectoryAsync(
                Path.Combine(infrastructurePath, "Themes"),
                Path.Combine(targetVibeDir, "Themes"));
        }

        // Copy ServiceCollectionExtensions.cs
        var serviceExtensionsSource = Path.Combine(infrastructurePath, "ServiceCollectionExtensions.cs");
        var serviceExtensionsTarget = Path.Combine(targetVibeDir, "ServiceCollectionExtensions.cs");
        if (File.Exists(serviceExtensionsSource))
        {
            await File.WriteAllTextAsync(serviceExtensionsTarget, await File.ReadAllTextAsync(serviceExtensionsSource));
        }

        // Copy _Imports.razor
        var importsSource = Path.Combine(infrastructurePath, "_Imports.razor");
        var importsTarget = Path.Combine(targetVibeDir, "_Imports.razor");
        if (File.Exists(importsSource))
        {
            await File.WriteAllTextAsync(importsTarget, await File.ReadAllTextAsync(importsSource));
        }

        // Copy JavaScript files if needed
        if (withCharts)
        {
            var jsTemplatePath = Path.Combine(templatePath, "wwwroot", "js");
            var jsTargetPath = Path.Combine(projectPath, "wwwroot", "js");
            Directory.CreateDirectory(jsTargetPath);

            // Copy vibe-chart.js
            var chartSource = Path.Combine(jsTemplatePath, "vibe-chart.js");
            var chartTarget = Path.Combine(jsTargetPath, "vibe-chart.js");
            if (File.Exists(chartSource))
            {
                await File.WriteAllTextAsync(chartTarget, await File.ReadAllTextAsync(chartSource));
            }
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
        var assemblyLocation = Path.GetDirectoryName(typeof(InitCommand).Assembly.Location) ?? AppContext.BaseDirectory;

        // Try multiple possible paths for Templates directory
        var possiblePaths = new[]
        {
            // 1. Development mode: relative to CLI project
            Path.GetFullPath(Path.Combine(assemblyLocation, "..", "..", "..", "Templates")),

            // 2. Packaged with CLI in Templates folder (adjacent to executable)
            Path.Combine(assemblyLocation, "Templates"),

            // 3. Dotnet global tool: Templates folder in package root (../../.. from tools/net9.0/any)
            Path.GetFullPath(Path.Combine(assemblyLocation, "..", "..", "..", "Templates")),

            // 4. Using AppContext.BaseDirectory
            Path.Combine(AppContext.BaseDirectory, "Templates"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Templates"))
        };

        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                return path;
            }
        }

        throw new DirectoryNotFoundException(
            $"Could not find Templates directory. Please ensure Vibe.UI.CLI is installed correctly. " +
            $"Searched locations: {string.Join(", ", possiblePaths)}");
    }

    private async Task GenerateThemeCssAsync(string projectPath, string cssFile, string baseColor)
    {
        var cssFilePath = Path.Combine(projectPath, cssFile);
        var cssDirectory = Path.GetDirectoryName(cssFilePath);

        if (!string.IsNullOrEmpty(cssDirectory) && !Directory.Exists(cssDirectory))
        {
            Directory.CreateDirectory(cssDirectory);
        }

        var themeCss = GetThemeCssForColor(baseColor);

        // Check if file exists and if it already has vibe theme variables
        if (File.Exists(cssFilePath))
        {
            var existingContent = await File.ReadAllTextAsync(cssFilePath);
            if (existingContent.Contains("@layer vibe"))
            {
                // Already has vibe variables, skip
                return;
            }

            // Append to existing file
            await File.AppendAllTextAsync(cssFilePath, $"\n\n{themeCss}");
        }
        else
        {
            // Create new file
            await File.WriteAllTextAsync(cssFilePath, themeCss);
        }
    }

    private string GetThemeCssForColor(string baseColor)
    {
        var (lightColors, darkColors) = baseColor switch
        {
            "Slate" => (
                light: ("hsl(0 0% 100%)", "hsl(222.2 84% 4.9%)", "hsl(222.2 47.4% 11.2%)", "hsl(210 40% 96.1%)", "hsl(214.3 31.8% 91.4%)"),
                dark: ("hsl(222.2 84% 4.9%)", "hsl(210 40% 98%)", "hsl(217.2 32.6% 17.5%)", "hsl(217.2 32.6% 17.5%)", "hsl(215 20.2% 65.1%)")
            ),
            "Gray" => (
                light: ("hsl(0 0% 100%)", "hsl(0 0% 3.9%)", "hsl(0 0% 14.9%)", "hsl(0 0% 96.1%)", "hsl(0 0% 89.8%)"),
                dark: ("hsl(0 0% 3.9%)", "hsl(0 0% 98%)", "hsl(0 0% 14.9%)", "hsl(0 0% 14.9%)", "hsl(0 0% 63.9%)")
            ),
            "Zinc" => (
                light: ("hsl(0 0% 100%)", "hsl(240 10% 3.9%)", "hsl(240 5.9% 10%)", "hsl(240 4.8% 95.9%)", "hsl(240 5.9% 90%)"),
                dark: ("hsl(240 10% 3.9%)", "hsl(0 0% 98%)", "hsl(240 3.7% 15.9%)", "hsl(240 3.7% 15.9%)", "hsl(240 5% 64.9%)")
            ),
            "Neutral" => (
                light: ("hsl(0 0% 100%)", "hsl(0 0% 3.9%)", "hsl(0 0% 14.9%)", "hsl(0 0% 96.1%)", "hsl(0 0% 89.8%)"),
                dark: ("hsl(0 0% 3.9%)", "hsl(0 0% 98%)", "hsl(0 0% 14.9%)", "hsl(0 0% 14.9%)", "hsl(0 0% 63.9%)")
            ),
            "Stone" => (
                light: ("hsl(0 0% 100%)", "hsl(20 14.3% 4.1%)", "hsl(24 9.8% 10%)", "hsl(60 9.1% 97.8%)", "hsl(24 5.7% 82.9%)"),
                dark: ("hsl(20 14.3% 4.1%)", "hsl(60 9.1% 97.8%)", "hsl(24 9.8% 10%)", "hsl(24 9.8% 10%)", "hsl(24 5.4% 63.9%)")
            ),
            "Blue" => (
                light: ("hsl(0 0% 100%)", "hsl(222.2 84% 4.9%)", "hsl(221.2 83.2% 53.3%)", "hsl(210 40% 96.1%)", "hsl(214.3 31.8% 91.4%)"),
                dark: ("hsl(222.2 84% 4.9%)", "hsl(210 40% 98%)", "hsl(217.2 91.2% 59.8%)", "hsl(217.2 32.6% 17.5%)", "hsl(215 20.2% 65.1%)")
            ),
            _ => (
                light: ("hsl(0 0% 100%)", "hsl(222.2 84% 4.9%)", "hsl(222.2 47.4% 11.2%)", "hsl(210 40% 96.1%)", "hsl(214.3 31.8% 91.4%)"),
                dark: ("hsl(222.2 84% 4.9%)", "hsl(210 40% 98%)", "hsl(217.2 32.6% 17.5%)", "hsl(217.2 32.6% 17.5%)", "hsl(215 20.2% 65.1%)")
            )
        };

        return $@"/* ============================================
   VIBE.UI THEME VARIABLES
   Base color: {baseColor}
   Generated by Vibe.UI CLI
   ============================================ */

@layer vibe {{
  :root {{
    --vibe-background: {lightColors.Item1};
    --vibe-foreground: {lightColors.Item2};
    --vibe-card: {lightColors.Item1};
    --vibe-card-foreground: {lightColors.Item2};
    --vibe-popover: {lightColors.Item1};
    --vibe-popover-foreground: {lightColors.Item2};
    --vibe-primary: {lightColors.Item3};
    --vibe-primary-foreground: hsl(0 0% 100%);
    --vibe-secondary: {lightColors.Item4};
    --vibe-secondary-foreground: {lightColors.Item2};
    --vibe-muted: {lightColors.Item5};
    --vibe-muted-foreground: hsl(215.4 16.3% 46.9%);
    --vibe-accent: {lightColors.Item4};
    --vibe-accent-foreground: {lightColors.Item2};
    --vibe-destructive: hsl(0 84.2% 60.2%);
    --vibe-destructive-foreground: hsl(0 0% 100%);
    --vibe-border: hsl(214.3 31.8% 91.4%);
    --vibe-input: hsl(214.3 31.8% 91.4%);
    --vibe-ring: {lightColors.Item3};
    --vibe-radius: 0.5rem;
  }}

  .dark {{
    --vibe-background: {darkColors.Item1};
    --vibe-foreground: {darkColors.Item2};
    --vibe-card: {darkColors.Item3};
    --vibe-card-foreground: {darkColors.Item2};
    --vibe-popover: {darkColors.Item3};
    --vibe-popover-foreground: {darkColors.Item2};
    --vibe-primary: {darkColors.Item3};
    --vibe-primary-foreground: {darkColors.Item2};
    --vibe-secondary: {darkColors.Item4};
    --vibe-secondary-foreground: {darkColors.Item2};
    --vibe-muted: {darkColors.Item4};
    --vibe-muted-foreground: {darkColors.Item5};
    --vibe-accent: {darkColors.Item4};
    --vibe-accent-foreground: {darkColors.Item2};
    --vibe-destructive: hsl(0 62.8% 30.6%);
    --vibe-destructive-foreground: {darkColors.Item2};
    --vibe-border: {darkColors.Item4};
    --vibe-input: {darkColors.Item4};
    --vibe-ring: {darkColors.Item3};
  }}
}}";
    }
}
