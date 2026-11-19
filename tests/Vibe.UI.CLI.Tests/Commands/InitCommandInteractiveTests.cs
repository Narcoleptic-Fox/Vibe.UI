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
    // Order must match the SelectionPrompt in InitCommand: the test picks by arrow-key count.
    private static readonly string[] PromptOrder =
    {
        "Slate",
        "Gray",
        "Zinc",
        "Neutral",
        "Stone",
        "Blue",
    };

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

    [Theory]
    [InlineData("Gray", "hsl(0 0% 14.9%)", "hsl(0 0% 96.1%)")]
    [InlineData("Zinc", "hsl(240 5.9% 10%)", "hsl(240 4.8% 95.9%)")]
    [InlineData("Neutral", "hsl(0 0% 14.9%)", "hsl(0 0% 96.1%)")]
    [InlineData("Stone", "hsl(24 9.8% 10%)", "hsl(60 9.1% 97.8%)")]
    [InlineData("Blue", "hsl(221.2 83.2% 53.3%)", "hsl(210 40% 96.1%)")]
    public async Task ExecuteAsync_SelectedBaseColor_AppendsThatColorScheme(
        string baseColor,
        string expectedLightPrimary,
        string expectedLightSecondary
    )
    {
        QueuePromptAnswers(componentDirectory: "Components/vibe", baseColor);

        var result = await RunInitAsync();

        result.Should().Be(0);
        var css = await ReadVibeBaseCssAsync();
        css.Should().Contain($"Color scheme: {baseColor}");
        css.Should().Contain($"--vibe-primary: {expectedLightPrimary};");
        css.Should().Contain($"--vibe-secondary: {expectedLightSecondary};");
        css.Should().Contain(".dark {");
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
