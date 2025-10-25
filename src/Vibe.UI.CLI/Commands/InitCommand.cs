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
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.MarkupLine("[blue]Initializing Vibe.UI in your project...[/]\n");

        var projectService = new ProjectService();
        var configService = new ConfigService();

        // Check if already initialized
        if (configService.ConfigExists(settings.ProjectPath))
        {
            if (!settings.SkipPrompts)
            {
                if (!AnsiConsole.Confirm("Vibe.UI is already initialized. Do you want to reconfigure?"))
                {
                    return 0;
                }
            }
        }

        // Detect project type
        var projectType = await projectService.DetectProjectTypeAsync(settings.ProjectPath);

        AnsiConsole.MarkupLine($"[green]✓[/] Detected project type: [yellow]{projectType}[/]");

        // Select theme
        var theme = "light";
        if (!settings.SkipPrompts)
        {
            theme = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Which [green]theme[/] would you like to use?")
                    .AddChoices(new[] { "light", "dark", "both" }));
        }

        // Select component directory
        var componentDir = "Components";
        if (!settings.SkipPrompts)
        {
            componentDir = AnsiConsole.Ask("Where should components be installed?", "Components");
        }

        // Create configuration
        var config = new Models.VibeConfig
        {
            ProjectType = projectType,
            Theme = theme,
            ComponentsDirectory = componentDir,
            CssVariables = true
        };

        await AnsiConsole.Status()
            .StartAsync("Setting up configuration...", async ctx =>
            {
                // Save configuration
                await configService.SaveConfigAsync(settings.ProjectPath, config);
                ctx.Status("Installing dependencies...");

                // Add package reference
                await projectService.AddPackageReferenceAsync(settings.ProjectPath, "Vibe.UI");

                ctx.Status("Creating component directory...");

                // Create components directory
                Directory.CreateDirectory(Path.Combine(settings.ProjectPath, componentDir));

                ctx.Status("Copying theme files...");

                // Copy theme files
                await projectService.CopyThemeFilesAsync(settings.ProjectPath, theme);
            });

        AnsiConsole.MarkupLine("\n[green]✓[/] Vibe.UI initialized successfully!");
        AnsiConsole.MarkupLine($"[grey]Configuration saved to vibe.json[/]");
        AnsiConsole.MarkupLine($"\n[blue]Next steps:[/]");
        AnsiConsole.MarkupLine($"  1. Run [yellow]vibe add button[/] to add your first component");
        AnsiConsole.MarkupLine($"  2. Run [yellow]vibe list[/] to see all available components");

        return 0;
    }
}
