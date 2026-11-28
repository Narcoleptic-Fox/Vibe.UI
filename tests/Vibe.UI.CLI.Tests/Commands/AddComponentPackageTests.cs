using System.Xml.Linq;
using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using Vibe.UI.CLI.Commands;
using Vibe.UI.CLI.Models;
using Vibe.UI.CLI.Services;
using Vibe.UI.CLI.Tests.Helpers;
using Xunit;

namespace Vibe.UI.CLI.Tests.Commands;

/// <summary>
/// A component copied as source can need a package the app does not have. <c>VibeGrid</c> wraps
/// Microsoft's QuickGrid, so <c>vibe add vibegrid</c> has to leave the project referencing it,
/// or the copied file does not compile.
/// </summary>
[Collection("SpectreConsole")]
public class AddComponentPackageTests : IDisposable
{
    private const string QuickGrid = ComponentPackages.QuickGrid;

    private readonly string _root;
    private readonly string _projectFile;
    private readonly IAnsiConsole _originalConsole;
    private readonly TestConsole _console;

    public AddComponentPackageTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"vibe-add-package-{Guid.NewGuid()}");
        Directory.CreateDirectory(_root);
        _projectFile = Path.Combine(_root, "App.csproj");
        _originalConsole = AnsiConsole.Console;
        _console = new TestConsole();
        _console.Profile.Width = 400;
        AnsiConsole.Console = _console;
    }

    [Fact]
    public async Task Adding_the_grid_references_QuickGrid_at_the_version_the_library_is_built_with()
    {
        await InitializeAsync(PlainProject);

        (await AddAsync("vibegrid")).Should().Be(0);

        var reference = Items(_projectFile, "PackageReference").Should().ContainSingle().Subject;
        reference.Attribute("Version")!.Value.Should().Be(ComponentPackages.VersionOf(QuickGrid));
        File.Exists(Path.Combine(_root, "Components", "VibeGrid.razor")).Should().BeTrue();
        _console.Output.Should().Contain(QuickGrid);
    }

    [Fact]
    public void The_version_written_is_the_one_in_the_librarys_own_project()
    {
        // Recorded at build time. If the two could differ, a copied grid would compile against a
        // QuickGrid the library was never tested with.
        var libraryProject = Path.Combine(RepositoryRoot(), "src", "Vibe.UI", "Vibe.UI.csproj");

        var inLibrary = Items(libraryProject, "PackageReference").Should().ContainSingle().Subject;

        ComponentPackages.VersionOf(QuickGrid).Should().Be(inLibrary.Attribute("Version")!.Value);
    }

    [Fact]
    public async Task Adding_it_twice_references_QuickGrid_once()
    {
        await InitializeAsync(PlainProject);

        (await AddAsync("vibegrid")).Should().Be(0);
        var afterFirst = await File.ReadAllTextAsync(_projectFile);
        (await AddAsync("vibegrid", overwrite: true)).Should().Be(0);

        (await File.ReadAllTextAsync(_projectFile)).Should().Be(afterFirst);
        Items(_projectFile, "PackageReference").Should().ContainSingle();
    }

    [Fact]
    public async Task A_version_the_app_already_chose_is_left_alone()
    {
        await InitializeAsync(
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <PackageReference Include="Microsoft.AspNetCore.Components.QuickGrid" Version="10.0.1" />
              </ItemGroup>
            </Project>
            """
        );
        var before = await File.ReadAllTextAsync(_projectFile);

        (await AddAsync("vibegrid")).Should().Be(0);

        (await File.ReadAllTextAsync(_projectFile)).Should().Be(before);
    }

    [Fact]
    public async Task A_component_that_needs_no_package_does_not_touch_the_project_file()
    {
        await InitializeAsync(PlainProject);
        var before = await File.ReadAllBytesAsync(_projectFile);

        (await AddAsync("button")).Should().Be(0);

        (await File.ReadAllBytesAsync(_projectFile)).Should().Equal(before);
    }

    [Fact]
    public async Task Under_central_package_management_the_version_goes_in_the_packages_file()
    {
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
        await InitializeAsync(
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <PackageReference Include="Microsoft.AspNetCore.Components.Web" />
              </ItemGroup>
            </Project>
            """
        );

        (await AddAsync("vibegrid")).Should().Be(0);

        Items(_projectFile, "PackageReference")
            .Should()
            .ContainSingle()
            .Which.Attribute("Version")
            .Should()
            .BeNull("central package management rejects a Version on a PackageReference");
        Items(packagesFile, "PackageVersion")
            .Should()
            .ContainSingle()
            .Which.Attribute("Version")!
            .Value.Should()
            .Be(ComponentPackages.VersionOf(QuickGrid));
    }

    [Fact]
    public async Task Central_package_management_with_no_packages_file_is_refused_with_nothing_written()
    {
        await InitializeAsync(
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
            </Project>
            """
        );
        var before = await File.ReadAllBytesAsync(_projectFile);

        (await AddAsync("vibegrid")).Should().Be(1);

        (await File.ReadAllBytesAsync(_projectFile)).Should().Equal(before);
        File.Exists(Path.Combine(_root, "Components", "VibeGrid.razor"))
            .Should()
            .BeFalse(
                "a grid that cannot be given its package must not be copied in to fail the build"
            );
        _console.Output.Should().Contain("Directory.Packages.props").And.Contain(QuickGrid);
    }

    [Fact]
    public async Task With_no_project_file_the_grid_is_refused_with_nothing_copied()
    {
        await InitializeAsync(project: null);

        (await AddAsync("vibegrid")).Should().Be(1);

        File.Exists(Path.Combine(_root, "Components", "VibeGrid.razor")).Should().BeFalse();
        _console.Output.Should().Contain(".csproj").And.Contain(QuickGrid);
    }

    private const string PlainProject = """
        <Project Sdk="Microsoft.NET.Sdk.Web">
          <ItemGroup>
            <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="10.0.0" />
          </ItemGroup>
        </Project>
        """;

    private async Task InitializeAsync(string? project)
    {
        if (project is not null)
        {
            await File.WriteAllTextAsync(_projectFile, project);
        }

        // The infrastructure files the add command looks for, and a configuration.
        var vibeBase = Path.Combine(_root, "Vibe", "Base");
        Directory.CreateDirectory(vibeBase);
        await File.WriteAllTextAsync(Path.Combine(vibeBase, "ClassBuilder.cs"), "// placeholder");
        await File.WriteAllTextAsync(Path.Combine(vibeBase, "VibeComponent.cs"), "// placeholder");
        await new ConfigService().SaveConfigAsync(
            _root,
            new VibeConfig
            {
                ProjectType = "Blazor WebAssembly",
                Theme = "light",
                ComponentsDirectory = "Components",
                CssVariables = true,
            }
        );
    }

    private Task<int> AddAsync(string component, bool overwrite = false) =>
        new AddCommand().ExecuteAsync(
            new CommandContext([], new TestRemainingArguments(), "add", null),
            new AddCommand.Settings
            {
                Component = component,
                SkipPrompts = true,
                Overwrite = overwrite,
                ProjectPath = _root,
            }
        );

    private static IReadOnlyList<XElement> Items(string file, string itemName) =>
        XDocument
            .Load(file)
            .Descendants(itemName)
            .Where(item => item.Attribute("Include")?.Value == QuickGrid)
            .ToList();

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Vibe.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException(
                "Vibe.slnx was not found above the test output."
            );
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
