using Spectre.Console;
using Spectre.Console.Cli;
using Vibe.UI.CLI.Commands;
using Vibe.UI.CLI.Infrastructure;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("vibe");

    config.AddCommand<InitCommand>("init")
        .WithDescription("Initialize Vibe.UI in your project")
        .WithExample(new[] { "init" });

    config.AddCommand<AddCommand>("add")
        .WithDescription("Add a component to your project")
        .WithExample(new[] { "add", "button" })
        .WithExample(new[] { "add", "dialog" });

    config.AddCommand<ListCommand>("list")
        .WithDescription("List all available components")
        .WithExample(new[] { "list" });

    config.AddCommand<UpdateCommand>("update")
        .WithDescription("Update Vibe.UI components")
        .WithExample(new[] { "update" });

    config.AddCommand<CssCommand>("css")
        .WithDescription("Generate CSS using Vibe.UI.CSS JIT engine")
        .WithExample(new[] { "css" })
        .WithExample(new[] { "css", ".", "-o", "wwwroot/css/Vibe.UI.CSS" })
        .WithExample(new[] { "css", "--watch" })
        .WithExample(new[] { "css", "--scan-only" });
});

// Display banner
AnsiConsole.Write(
    new FigletText("Vibe.UI")
        .Centered()
        .Color(Color.Blue));

AnsiConsole.MarkupLine($"[grey]v{CliVersion.Current}[/]\n");

return await app.RunAsync(args);

