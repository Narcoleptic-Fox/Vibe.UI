using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using Vibe.UI.CLI.Services;

namespace Vibe.UI.CLI.Commands;

public class ListCommand : Command<ListCommand.Settings>
{
    /// <summary>
    /// The option that switches to machine-readable output. Program.cs reads it too, to keep
    /// the banner out of that output.
    /// </summary>
    public const string NamesOption = "--names";

    public class Settings : CommandSettings
    {
        [Description("Print only the component names, one per line, for scripts")]
        [CommandOption(NamesOption)]
        [DefaultValue(false)]
        public bool Names { get; init; }
    }

    public int Execute(CommandContext context) =>
        Execute(context, new Settings(), CancellationToken.None);

    public int Execute(CommandContext context, Settings settings) =>
        Execute(context, settings, CancellationToken.None);

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var componentService = new ComponentService();
        var components = componentService.GetAvailableComponents();

        if (settings.Names)
        {
            foreach (var name in components.Select(c => c.Name).Order(StringComparer.Ordinal))
            {
                Console.Out.WriteLine(name);
            }

            return 0;
        }

        AnsiConsole.MarkupLine("[blue]Available Vibe.UI Components:[/]\n");

        // Group by category
        var grouped = components.GroupBy(c => c.Category).OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            AnsiConsole.MarkupLine($"[yellow]{group.Key}[/]");

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Component")
                .AddColumn("Description");

            foreach (var component in group.OrderBy(c => c.Name))
            {
                table.AddRow(
                    $"[green]{component.Name}[/]",
                    component.Description ?? "");
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        AnsiConsole.MarkupLine($"[grey]Total: {components.Count} components available[/]");
        // Escape the square brackets in the usage example to prevent Spectre.Console from parsing them as markup
        AnsiConsole.MarkupLine($"\n[blue]Usage:[/] vibe add [[component-name]]");

        return 0;
    }
}
