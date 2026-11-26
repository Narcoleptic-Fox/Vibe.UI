using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using Vibe.UI.CLI.Services;

namespace Vibe.UI.CLI.Commands;

public class UpdateCommand : AsyncCommand<UpdateCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Update specific component")]
        [CommandArgument(0, "[component]")]
        public string? Component { get; init; }

        [Description("Skip confirmation prompts")]
        [CommandOption("-y|--yes")]
        [DefaultValue(false)]
        public bool SkipPrompts { get; init; }

        [Description("Project directory path")]
        [CommandOption("-p|--path")]
        [DefaultValue(".")]
        public string ProjectPath { get; init; } = ".";

        [Description(
            "Refresh the infrastructure vibe init copied (Vibe/, interop scripts, utility CSS), and the FluentValidation adapter if the project has it. Components, the components directory and the colour are left alone"
        )]
        [CommandOption(InfrastructureOption)]
        [DefaultValue(false)]
        public bool Infrastructure { get; init; }

        [Description("With --infrastructure: also add the FluentValidation adapter")]
        [CommandOption(InitCommand.WithFluentValidationOption)]
        [DefaultValue(false)]
        public bool WithFluentValidation { get; init; }
    }

    public const string InfrastructureOption = "--infrastructure";

    public Task<int> ExecuteAsync(CommandContext context, Settings settings) =>
        ExecuteAsync(context, settings, CancellationToken.None);

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var configService = new ConfigService();
        var componentService = new ComponentService();
        var projectService = new ProjectService();
        var requestedProjectPath = Path.GetFullPath(settings.ProjectPath);
        var projectPath = await projectService.ResolveInitializedProjectPathAsync(requestedProjectPath);

        if (!PathsEqual(requestedProjectPath, projectPath))
        {
            AnsiConsole.WriteLine($"Using initialized project: {projectPath}");
        }

        var config = await configService.LoadConfigAsync(projectPath);
        if (config == null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Vibe.UI is not initialized in this project.");
            return 1;
        }

        if (settings.WithFluentValidation && !settings.Infrastructure)
        {
            AnsiConsole.MarkupLine(
                $"[red]Error:[/] {InitCommand.WithFluentValidationOption} goes with {InfrastructureOption}: vibe update {InfrastructureOption} {InitCommand.WithFluentValidationOption}"
            );
            return 1;
        }

        if (settings.Infrastructure)
        {
            if (!string.IsNullOrEmpty(settings.Component))
            {
                AnsiConsole.MarkupLine(
                    $"[red]Error:[/] {InfrastructureOption} refreshes the infrastructure only; update {Markup.Escape(settings.Component)} on its own."
                );
                return 1;
            }

            return await UpdateInfrastructureAsync(configService, config, projectPath, settings);
        }

        // Components are updated from this CLI; the infrastructure they build on is only refreshed
        // when asked for, because the user owns the copied source. Say so once if it may be behind.
        var infrastructureNote = InfrastructureStamp.Message(config);
        if (infrastructureNote is not null)
        {
            AnsiConsole.MarkupLine($"[yellow]Note:[/] {Markup.Escape(infrastructureNote)}");
        }

        if (!string.IsNullOrEmpty(settings.Component))
        {
            // Update specific component
            AnsiConsole.MarkupLine($"[blue]Updating {settings.Component}...[/]");
            await componentService.InstallComponentAsync(
                projectPath,
                config.ComponentsDirectory,
                settings.Component,
                overwrite: true);
            await InstallMissingDependenciesAsync(
                componentService,
                projectPath,
                config.ComponentsDirectory,
                settings.Component);
            await RazorImportsService.EnsureVibeImportsAsync(projectPath, includeComponents: true);

            AnsiConsole.MarkupLine($"[green]✓[/] {settings.Component} updated successfully!");
        }
        else
        {
            // Update all components
            if (!settings.SkipPrompts)
            {
                if (!AnsiConsole.Confirm("Update all components?"))
                {
                    return 0;
                }
            }

            var installedComponents = componentService.GetInstalledComponents(
                projectPath,
                config.ComponentsDirectory);

            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[blue]Updating components[/]", maxValue: installedComponents.Count);

                    foreach (var component in installedComponents)
                    {
                        task.Increment(1);
                        await componentService.InstallComponentAsync(
                            projectPath,
                            config.ComponentsDirectory,
                            component,
                            overwrite: true);
                        await InstallMissingDependenciesAsync(
                            componentService,
                            projectPath,
                            config.ComponentsDirectory,
                            component);
                    }
                });

            if (installedComponents.Count > 0)
            {
                await RazorImportsService.EnsureVibeImportsAsync(projectPath, includeComponents: true);
            }

            AnsiConsole.MarkupLine($"\n[green]✓[/] All components updated successfully!");
        }

        return 0;
    }

    private static async Task<int> UpdateInfrastructureAsync(
        ConfigService configService,
        Models.VibeConfig config,
        string projectPath,
        Settings settings
    )
    {
        var selection = InfrastructureInstaller.SelectionOf(projectPath, settings.WithFluentValidation);

        // Everything that will be written is worked out first, so declining, or a project the
        // adapter cannot be added to, leaves the project as it was.
        var adapterChanges = selection.FluentValidation
            ? FluentValidationAdapterInstaller.PlanProjectChanges(
                projectPath,
                ProjectService.FindProjectFile(projectPath)
            )
            : null;
        var files = InfrastructureInstaller.Plan(InfrastructureInstaller.FindTemplatePath(), selection);

        var toWrite = InfrastructureUpdatePrompt.ChooseFilesToWrite(
            projectPath,
            files,
            adapterChanges,
            settings.SkipPrompts
        );
        if (toWrite is null)
        {
            AnsiConsole.MarkupLine("Nothing changed.");
            return 0;
        }

        await InfrastructureInstaller.CopyAsync(projectPath, toWrite);
        if (adapterChanges is not null)
        {
            await FluentValidationAdapterInstaller.ApplyProjectChangesAsync(adapterChanges);
        }

        InfrastructureStamp.Write(config);
        await configService.SaveConfigAsync(projectPath, config);

        AnsiConsole.MarkupLine(
            $"[green]✓[/] Infrastructure refreshed from CLI {InfrastructureStamp.CurrentVersion}: {toWrite.Count} files."
        );
        if (adapterChanges is not null)
        {
            InitCommand.WriteFluentValidationSummary(adapterChanges);
        }

        return 0;
    }

    /// <summary>
    /// A newer version of a component can need a component the project does not have yet.
    /// Those are added; ones already present are left alone, since the user may have edited them.
    /// </summary>
    private static async Task InstallMissingDependenciesAsync(
        ComponentService componentService,
        string projectPath,
        string componentsDirectory,
        string componentName)
    {
        // Anything in the components directory that the registry does not know is the user's own.
        if (componentService.GetComponent(componentName) == null)
        {
            return;
        }

        foreach (var dependency in componentService.GetRequiredComponents(componentName))
        {
            await componentService.InstallComponentAsync(
                projectPath,
                componentsDirectory,
                dependency.Name,
                overwrite: false);
        }
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
}
