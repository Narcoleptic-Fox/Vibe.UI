using System.Xml.Linq;
using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using Vibe.UI.CLI.Commands;
using Vibe.UI.CLI.Services;
using Vibe.UI.CLI.Tests.Helpers;
using Xunit;

namespace Vibe.UI.CLI.Tests.Commands;

/// <summary>
/// <c>vibe update --infrastructure</c>, the staleness note on component updates, and central
/// package management. The infrastructure templates here are the test stubs, so these check
/// which files are written and which are left alone; that a refreshed project has every file and
/// compiles is proven by scripts/Validate-VendoredComponents.ps1.
/// </summary>
[Collection("SpectreConsole")]
public class UpdateInfrastructureTests : IDisposable
{
    private readonly string _root;
    private readonly IAnsiConsole _originalConsole;
    private readonly TestConsole _console;

    public UpdateInfrastructureTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"vibe-update-infra-{Guid.NewGuid()}");
        Directory.CreateDirectory(_root);
        _originalConsole = AnsiConsole.Console;
        _console = new TestConsole();
        _console.Profile.Width = 400;
        AnsiConsole.Console = _console;
    }

    [Fact]
    public async Task Infrastructure_RestoresInfrastructureAndLeavesComponentsAndChoicesAlone()
    {
        await InitAsync();

        // The user's choices: a components directory and a colour, and an edited component.
        await SetComponentsDirectoryAsync("Components/mine");
        var component = await AddComponentAsync("button");
        await File.AppendAllTextAsync(component, "\n@* edited by the user *@");
        var componentBefore = await File.ReadAllTextAsync(component);

        // An older or damaged shape: an edited base class, a missing metadata file.
        var baseClass = Path.Combine(_root, "Vibe", "Base", "VibeComponent.cs");
        var baseClassTemplate = await File.ReadAllTextAsync(baseClass);
        await File.WriteAllTextAsync(baseClass, "// old");
        var metadata = Path.Combine(_root, "Vibe", "Metadata", "PropertyMetadataResolver.cs");
        File.Delete(metadata);

        var baseCss = Path.Combine(_root, "wwwroot", "css", "vibe-base.css");
        await File.AppendAllTextAsync(baseCss, "\n/* Color scheme: chosen */");
        var baseCssBefore = await File.ReadAllTextAsync(baseCss);

        var result = await UpdateAsync(infrastructure: true, skipPrompts: true);

        result.Should().Be(0);
        (await File.ReadAllTextAsync(baseClass)).Should().Be(baseClassTemplate);
        File.Exists(metadata).Should().BeTrue();
        (await File.ReadAllTextAsync(component)).Should().Be(componentBefore);
        (await File.ReadAllTextAsync(baseCss)).Should().Be(baseCssBefore);
        var config = await new ConfigService().LoadConfigAsync(_root);
        config!.ComponentsDirectory.Should().Be("Components/mine");
        config.InfrastructureVersion.Should().Be(InfrastructureStamp.CurrentVersion);
    }

    [Fact]
    public async Task Infrastructure_WithoutTheAdapter_DoesNotAddIt()
    {
        var projectFile = await InitAsync();

        await UpdateAsync(infrastructure: true, skipPrompts: true);

        Directory.Exists(Path.Combine(_root, "Vibe", "FluentValidation")).Should().BeFalse();
        FluentValidationItems(projectFile, "PackageReference").Should().BeEmpty();
    }

    [Fact]
    public async Task Infrastructure_WithTheAdapterPresent_RefreshesIt()
    {
        await InitAsync(withFluentValidation: true);
        var validator = Path.Combine(_root, "Vibe", "FluentValidation", "FluentValidator.cs");
        var template = await File.ReadAllTextAsync(validator);
        await File.WriteAllTextAsync(validator, "// old");

        await UpdateAsync(infrastructure: true, skipPrompts: true);

        (await File.ReadAllTextAsync(validator)).Should().Be(template);
    }

    [Fact]
    public async Task InfrastructureWithFluentValidation_AddsTheAdapterToAnInitialisedProject()
    {
        var projectFile = await InitAsync();

        var result = await UpdateAsync(
            infrastructure: true,
            skipPrompts: true,
            withFluentValidation: true
        );

        result.Should().Be(0);
        Directory
            .GetFiles(Path.Combine(_root, "Vibe", "FluentValidation"))
            .Select(Path.GetFileName)
            .Should()
            .BeEquivalentTo(FluentValidationAdapterInstaller.SourceFiles);
        FluentValidationItems(projectFile, "PackageReference")
            .Should()
            .ContainSingle()
            .Which.Attribute("Version")!
            .Value.Should()
            .Be(FluentValidationAdapterInstaller.PackageVersion);
        (await File.ReadAllTextAsync(Path.Combine(_root, "_Imports.razor")))
            .Should()
            .Contain(FluentValidationAdapterInstaller.RazorUsing);
    }

    [Fact]
    public async Task Infrastructure_Declined_ChangesNothing()
    {
        await InitAsync();
        await AddComponentAsync("button");
        await File.WriteAllTextAsync(
            Path.Combine(_root, "Vibe", "Base", "VibeComponent.cs"),
            "// old"
        );
        File.Delete(Path.Combine(_root, "Vibe", "Metadata", "PropertyMetadataResolver.cs"));
        var before = Snapshot();

        _console.Profile.Capabilities.Interactive = true;
        _console.Input.PushTextWithEnter("n");
        var result = await UpdateAsync(
            infrastructure: true,
            skipPrompts: false,
            withFluentValidation: true
        );

        result.Should().Be(0);
        Snapshot().Should().BeEquivalentTo(before);
        _console.Output.Should().Contain("Nothing changed.");
    }

    [Fact]
    public async Task Infrastructure_ListsTheFilesItReplacesBeforeAsking()
    {
        await InitAsync();

        await UpdateAsync(infrastructure: true, skipPrompts: true);

        _console.Output.Should().Contain("Replace");
        _console.Output.Should().Contain(Path.Combine("Vibe", "Base", "VibeComponent.cs"));
        _console.Output.Should().Contain("vibe.json");
    }

    [Fact]
    public async Task WithFluentValidationAlone_IsRefused()
    {
        await InitAsync();

        var result = await UpdateAsync(
            infrastructure: false,
            skipPrompts: true,
            withFluentValidation: true
        );

        result.Should().Be(1);
        Directory.Exists(Path.Combine(_root, "Vibe", "FluentValidation")).Should().BeFalse();
    }

    [Fact]
    public async Task InfrastructureWithAComponentName_IsRefused()
    {
        await InitAsync();
        var before = Snapshot();

        var result = await UpdateAsync(
            infrastructure: true,
            skipPrompts: true,
            component: "button"
        );

        result.Should().Be(1);
        Snapshot().Should().BeEquivalentTo(before);
    }

    [Theory]
    [InlineData("0.9.0", "Run vibe update --infrastructure")]
    [InlineData(null, "has no version stamp")]
    [InlineData("not-a-version", "has no version stamp")]
    [InlineData("999.0.0", "newer than this CLI")]
    public async Task ComponentUpdate_WhenTheInfrastructureIsNotCurrent_SaysSoOnce(
        string? stamp,
        string expected
    )
    {
        await InitAsync();
        await AddComponentAsync("button");
        await AddComponentAsync("badge");
        await SetStampAsync(stamp);

        var result = await UpdateAsync(infrastructure: false, skipPrompts: true);

        result.Should().Be(0);
        Occurrences(_console.Output, expected).Should().Be(1);
        if (stamp == "999.0.0")
        {
            _console.Output.Should().NotContain("Run vibe update --infrastructure");
        }
    }

    [Fact]
    public async Task ComponentUpdate_WhenTheInfrastructureIsCurrent_SaysNothingAboutIt()
    {
        await InitAsync();
        await AddComponentAsync("button");

        var result = await UpdateAsync(
            infrastructure: false,
            skipPrompts: true,
            component: "button"
        );

        result.Should().Be(0);
        _console.Output.Should().NotContain("infrastructure");
    }

    [Fact]
    public async Task ComponentUpdate_DoesNotRefreshTheInfrastructure()
    {
        await InitAsync();
        await AddComponentAsync("button");
        await SetStampAsync("0.9.0");
        var baseClass = Path.Combine(_root, "Vibe", "Base", "VibeComponent.cs");
        await File.WriteAllTextAsync(baseClass, "// old");

        await UpdateAsync(infrastructure: false, skipPrompts: true, component: "button");

        (await File.ReadAllTextAsync(baseClass)).Should().Be("// old");
    }

    [Fact]
    public async Task Init_RecordsTheInfrastructureVersion()
    {
        await InitAsync();

        var config = await new ConfigService().LoadConfigAsync(_root);
        config!.InfrastructureVersion.Should().Be(InfrastructureStamp.CurrentVersion);
    }

    [Fact]
    public async Task CentralPackageManagement_PutsTheVersionInDirectoryPackagesProps()
    {
        var projectFile = await WriteProjectAsync();
        var packagesFile = Path.Combine(_root, CentralPackageManagement.PackagesFileName);
        await File.WriteAllTextAsync(
            packagesFile,
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="Microsoft.AspNetCore.Components.Web" Version="10.0.0" />
              </ItemGroup>
            </Project>
            """
        );
        await File.WriteAllTextAsync(
            projectFile,
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <PackageReference Include="Microsoft.AspNetCore.Components.Web" />
              </ItemGroup>
            </Project>
            """
        );

        await InitAsync(withFluentValidation: true, writeProject: false);

        FluentValidationItems(projectFile, "PackageReference")
            .Should()
            .ContainSingle()
            .Which.Attribute("Version")
            .Should()
            .BeNull("central package management rejects a Version on a PackageReference");
        FluentValidationItems(packagesFile, "PackageVersion")
            .Should()
            .ContainSingle()
            .Which.Attribute("Version")!
            .Value.Should()
            .Be(FluentValidationAdapterInstaller.PackageVersion);
    }

    [Fact]
    public async Task CentralPackageManagement_KeepsAVersionTheAppAlreadySet()
    {
        var projectFile = await WriteProjectAsync();
        var packagesFile = Path.Combine(_root, CentralPackageManagement.PackagesFileName);
        await File.WriteAllTextAsync(
            packagesFile,
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="FluentValidation" Version="11.0.0" />
              </ItemGroup>
            </Project>
            """
        );

        await InitAsync(withFluentValidation: true, writeProject: false);

        FluentValidationItems(packagesFile, "PackageVersion")
            .Should()
            .ContainSingle()
            .Which.Attribute("Version")!
            .Value.Should()
            .Be("11.0.0");
        FluentValidationItems(projectFile, "PackageReference").Should().ContainSingle();
    }

    [Fact]
    public async Task CentralPackageManagement_WithNoPackagesFile_RefusesBeforeWritingAnything()
    {
        var projectFile = await WriteProjectAsync();
        await File.WriteAllTextAsync(
            projectFile,
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
            </Project>
            """
        );
        var before = Snapshot();

        var act = () => InitAsync(withFluentValidation: true, writeProject: false);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Directory.Packages.props*PackageVersion Include=\"FluentValidation\"*");
        Snapshot().Should().BeEquivalentTo(before);
    }

    [Theory]
    [InlineData("1.0.0-beta", "1.0.0-beta", 0)]
    [InlineData("1.0.0-beta", "1.0.0-beta.2", -1)]
    [InlineData("1.0.0-beta.2", "1.0.0-beta.10", -1)]
    [InlineData("1.0.0-beta.10", "1.0.0", -1)]
    [InlineData("1.0.0-alpha", "1.0.0-beta", -1)]
    [InlineData("1.0.0-beta.1", "1.0.0-beta.rc", -1)]
    [InlineData("1.0.0", "1.0.1", -1)]
    [InlineData("1.2.0", "1.10.0", -1)]
    [InlineData("2.0.0", "1.9.9", 1)]
    [InlineData("1.0.0-beta+abc", "1.0.0-beta+def", 0)]
    public void SemanticVersion_OrdersByPrecedence(string left, string right, int expected)
    {
        SemanticVersion.Compare(left, right).Should().Be(expected);
        SemanticVersion.Compare(right, left).Should().Be(-expected);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("v1.0.0")]
    [InlineData("1.0.0-")]
    [InlineData("1.0.0-beta..1")]
    public void SemanticVersion_IsNullForWhatIsNotAVersion(string text)
    {
        SemanticVersion.Compare(text, "1.0.0").Should().BeNull();
    }

    private async Task<string> WriteProjectAsync()
    {
        var projectFile = Path.Combine(_root, "App.csproj");
        await File.WriteAllTextAsync(
            projectFile,
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="10.0.0" />
              </ItemGroup>
            </Project>
            """
        );
        return projectFile;
    }

    private async Task<string> InitAsync(
        bool withFluentValidation = false,
        bool writeProject = true
    )
    {
        var projectFile = writeProject
            ? await WriteProjectAsync()
            : Path.Combine(_root, "App.csproj");
        var result = await new InitCommand().ExecuteAsync(
            new CommandContext([], new TestRemainingArguments(), "init", null),
            new InitCommand.Settings
            {
                SkipPrompts = true,
                ProjectPath = _root,
                WithFluentValidation = withFluentValidation,
            }
        );
        result.Should().Be(0);
        return projectFile;
    }

    private async Task<string> AddComponentAsync(string name)
    {
        var config = await new ConfigService().LoadConfigAsync(_root);
        await new ComponentService().InstallComponentAsync(
            _root,
            config!.ComponentsDirectory,
            name
        );
        return Directory
            .GetFiles(Path.Combine(_root, config.ComponentsDirectory), "*.razor")
            .Single(path =>
                Path.GetFileNameWithoutExtension(path)
                    .Equals(name, StringComparison.OrdinalIgnoreCase)
            );
    }

    private Task<int> UpdateAsync(
        bool infrastructure,
        bool skipPrompts,
        bool withFluentValidation = false,
        string? component = null
    ) =>
        new UpdateCommand().ExecuteAsync(
            new CommandContext([], new TestRemainingArguments(), "update", null),
            new UpdateCommand.Settings
            {
                Component = component,
                SkipPrompts = skipPrompts,
                ProjectPath = _root,
                Infrastructure = infrastructure,
                WithFluentValidation = withFluentValidation,
            }
        );

    private async Task SetComponentsDirectoryAsync(string directory)
    {
        var configService = new ConfigService();
        var config = await configService.LoadConfigAsync(_root);
        var oldDirectory = Path.Combine(_root, config!.ComponentsDirectory);
        var newDirectory = Path.Combine(_root, directory);
        Directory.CreateDirectory(Path.GetDirectoryName(newDirectory)!);
        Directory.Move(oldDirectory, newDirectory);
        config.ComponentsDirectory = directory;
        await configService.SaveConfigAsync(_root, config);
    }

    private async Task SetStampAsync(string? version)
    {
        var configService = new ConfigService();
        var config = await configService.LoadConfigAsync(_root);
        config!.InfrastructureVersion = version;
        await configService.SaveConfigAsync(_root, config);
    }

    /// <summary>Every file under the project with its content.</summary>
    private Dictionary<string, string> Snapshot() =>
        Directory
            .GetFiles(_root, "*", SearchOption.AllDirectories)
            .ToDictionary(path => Path.GetRelativePath(_root, path), File.ReadAllText);

    private static IReadOnlyList<XElement> FluentValidationItems(string file, string itemName) =>
        XDocument
            .Load(file)
            .Descendants(itemName)
            .Where(item =>
                item.Attribute("Include")?.Value == FluentValidationAdapterInstaller.PackageId
            )
            .ToList();

    private static int Occurrences(string text, string value)
    {
        var count = 0;
        for (
            var index = text.IndexOf(value, StringComparison.Ordinal);
            index >= 0;
            index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal)
        )
        {
            count++;
        }

        return count;
    }

    public void Dispose()
    {
        AnsiConsole.Console = _originalConsole;
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }
}
