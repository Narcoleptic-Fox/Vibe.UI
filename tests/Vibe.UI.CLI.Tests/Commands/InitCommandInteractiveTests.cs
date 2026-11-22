using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using Vibe.UI.CLI.Commands;
using Vibe.UI.CLI.Tests.Helpers;
using Xunit;

namespace Vibe.UI.CLI.Tests.Commands;

/// <summary>
/// Drives InitCommand through its prompts. The base color is only selectable interactively
/// (--yes always means Slate), so these are the only tests that reach the color scheme output.
/// </summary>
[Collection("SpectreConsole")]
public class InitCommandInteractiveTests : IDisposable
{
    // The prompt offers the default first, then every other palette in themes/*.json by name.
    // Read from the same definitions the CLI is generated from, so this cannot drift from it.
    private static readonly string[] PromptOrder =
    [
        "Slate",
        .. Palettes().Select(palette => palette.Name).Where(name => name != "Slate"),
    ];

    private readonly string _testProjectPath;
    private readonly IAnsiConsole _originalConsole;
    private readonly TestConsole _console;

    public InitCommandInteractiveTests()
    {
        _testProjectPath = Path.Combine(
            Path.GetTempPath(),
            $"vibe-init-interactive-{Guid.NewGuid()}"
        );
        Directory.CreateDirectory(_testProjectPath);
        File.WriteAllText(
            Path.Combine(_testProjectPath, "Test.csproj"),
            @"<Project Sdk=""Microsoft.NET.Sdk"" />"
        );

        _originalConsole = AnsiConsole.Console;
        _console = new TestConsole();
        _console.Profile.Capabilities.Interactive = true;
        _console.Profile.Capabilities.Ansi = true;
        AnsiConsole.Console = _console;
    }

    public static IEnumerable<object[]> SelectablePalettes() =>
        Palettes().Where(palette => palette.Name != "Slate").Select(palette => new object[] { palette.Name });

    [Theory]
    [MemberData(nameof(SelectablePalettes))]
    public async Task ExecuteAsync_SelectedBaseColor_AppendsThatPaletteFromItsDefinition(string baseColor)
    {
        var palette = Palettes().Single(candidate => candidate.Name == baseColor);
        QueuePromptAnswers(componentDirectory: "Components/vibe", baseColor);

        var result = await RunInitAsync();

        result.Should().Be(0);
        var css = await ReadVibeBaseCssAsync();
        var scheme = css[css.IndexOf($"Color scheme: {baseColor}", StringComparison.Ordinal)..];
        var darkStart = scheme.IndexOf(".dark {", StringComparison.Ordinal);
        darkStart.Should().BeGreaterThan(0);

        // Every token of the definition, in both modes, and nothing the definition does not have.
        foreach (var (name, value) in palette.Light)
        {
            scheme[..darkStart].Should().Contain($"--vibe-{name}: {value};");
        }

        foreach (var (name, value) in palette.Dark)
        {
            scheme[darkStart..].Should().Contain($"--vibe-{name}: {value};");
        }

        scheme.Split("--vibe-").Length.Should().Be(1 + palette.Light.Count + palette.Dark.Count);
    }

    [Fact]
    public async Task ExecuteAsync_SlateBaseColor_LeavesBaseCssUntouched()
    {
        QueuePromptAnswers(componentDirectory: "Components/vibe", baseColor: "Slate");

        var result = await RunInitAsync();

        result.Should().Be(0);
        var css = await ReadVibeBaseCssAsync();
        css.Should().NotContain("Color scheme:");
        css.Should().Contain("--vibe-primary: fixture-default;");
    }

    [Fact]
    public async Task ExecuteAsync_CustomComponentDirectory_IsCreatedAndSaved()
    {
        QueuePromptAnswers(componentDirectory: "Shared/Ui", baseColor: "Slate");

        var result = await RunInitAsync();

        result.Should().Be(0);
        Directory.Exists(Path.Combine(_testProjectPath, "Shared", "Ui")).Should().BeTrue();
        var config = await File.ReadAllTextAsync(Path.Combine(_testProjectPath, "vibe.json"));
        config.Should().Contain("Shared/Ui");
    }

    [Fact]
    public async Task ExecuteAsync_ComponentDirectoryOutsideProject_IsRejected()
    {
        QueuePromptAnswers(componentDirectory: "../outside", baseColor: "Slate");

        var act = RunInitAsync;

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*within the project directory*");
        File.Exists(Path.Combine(_testProjectPath, "vibe.json")).Should().BeFalse();
    }

    private sealed record Palette(
        string Name,
        IReadOnlyList<(string Name, string Value)> Light,
        IReadOnlyList<(string Name, string Value)> Dark);

    // The built-in palettes: every definition except the stylesheet default.
    private static IReadOnlyList<Palette> Palettes()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Vibe.slnx")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the tests run from inside the repository");

        static (string, string)[] Tokens(System.Text.Json.JsonElement mode) =>
            mode.EnumerateObject().Select(token => (token.Name, token.Value.GetString()!)).ToArray();

        return Directory
            .EnumerateFiles(Path.Combine(directory!.FullName, "themes"), "*.json")
            .Where(file => Path.GetFileNameWithoutExtension(file) != "default")
            .OrderBy(Path.GetFileNameWithoutExtension, StringComparer.Ordinal)
            .Select(file =>
            {
                using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(file));
                var root = document.RootElement;
                return new Palette(
                    root.GetProperty("name").GetString()!,
                    Tokens(root.GetProperty("light")),
                    Tokens(root.GetProperty("dark")));
            })
            .ToArray();
    }

    private void QueuePromptAnswers(string componentDirectory, string baseColor)
    {
        _console.Input.PushTextWithEnter(componentDirectory);

        var downPresses = Array.IndexOf(PromptOrder, baseColor);
        downPresses.Should().BeGreaterThanOrEqualTo(0, "the color must be one the prompt offers");
        for (var press = 0; press < downPresses; press++)
        {
            _console.Input.PushKey(ConsoleKey.DownArrow);
        }

        _console.Input.PushKey(ConsoleKey.Enter);
    }

    private Task<int> RunInitAsync()
    {
        var settings = new InitCommand.Settings
        {
            SkipPrompts = false,
            ProjectPath = _testProjectPath,
        };
        var context = new CommandContext(
            Array.Empty<string>(),
            new TestRemainingArguments(),
            "init",
            null
        );

        return new InitCommand().ExecuteAsync(context, settings);
    }

    private Task<string> ReadVibeBaseCssAsync() =>
        File.ReadAllTextAsync(Path.Combine(_testProjectPath, "wwwroot", "css", "vibe-base.css"));

    public void Dispose()
    {
        AnsiConsole.Console = _originalConsole;
        _console.Dispose();

        if (Directory.Exists(_testProjectPath))
        {
            Directory.Delete(_testProjectPath, true);
        }
    }
}
