using System.Xml.Linq;
using FluentAssertions;
using Spectre.Console.Cli;
using Vibe.UI.CLI.Commands;
using Vibe.UI.CLI.Services;
using Vibe.UI.CLI.Tests.Helpers;
using Xunit;

namespace Vibe.UI.CLI.Tests.Commands;

/// <summary>
/// <c>vibe init --with-fluentvalidation</c>. These check where files and references land.
/// Whether the result compiles is proven by scripts/Validate-VendoredComponents.ps1.
/// </summary>
[Collection("SpectreConsole")]
public class InitCommandFluentValidationTests : IDisposable
{
    private static readonly string AdapterSourceDirectory = Path.Combine(
        FindRepositoryRoot(),
        "src",
        "Vibe.UI.FluentValidation"
    );

    private readonly string _root;
    private readonly InitCommand _command = new();

    public InitCommandFluentValidationTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"vibe-init-fv-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void SourceFiles_AreExactlyTheAdapterProjectsSourceFiles()
    {
        var actual = Directory
            .GetFiles(AdapterSourceDirectory, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal);

        FluentValidationAdapterInstaller
            .SourceFiles.Order(StringComparer.Ordinal)
            .Should()
            .Equal(actual);
    }

    [Fact]
    public void PackageVersion_IsTheVersionTheAdapterProjectReferences()
    {
        FluentValidationAdapterInstaller
            .PackageVersion.Should()
            .Be(AdapterFluentValidationVersion());
    }

    [Fact]
    public async Task WithoutTheOption_CopiesNoAdapterAndAddsNoReference()
    {
        var projectFile = await WriteProjectAsync(_root, "App.csproj", "Microsoft.NET.Sdk.Web");

        var result = await InitAsync(_root, withFluentValidation: false);

        result.Should().Be(0);
        Directory.Exists(Path.Combine(_root, "Vibe", "FluentValidation")).Should().BeFalse();
        FluentValidationReferences(projectFile).Should().BeEmpty();
        (await File.ReadAllTextAsync(Path.Combine(_root, "_Imports.razor")))
            .Should()
            .NotContain("Vibe.UI.FluentValidation");
    }

    [Fact]
    public async Task WithTheOption_CopiesEveryAdapterFileUnchanged()
    {
        await WriteProjectAsync(_root, "App.csproj", "Microsoft.NET.Sdk.Web");

        var result = await InitAsync(_root, withFluentValidation: true);

        result.Should().Be(0);
        var target = Path.Combine(_root, "Vibe", "FluentValidation");
        Directory
            .GetFiles(target)
            .Select(Path.GetFileName)
            .Should()
            .BeEquivalentTo(FluentValidationAdapterInstaller.SourceFiles);

        // Verbatim, like Metadata/: the namespaces stay Vibe.UI.FluentValidation and Vibe.UI.
        foreach (var fileName in FluentValidationAdapterInstaller.SourceFiles)
        {
            (await File.ReadAllTextAsync(Path.Combine(target, fileName)))
                .Should()
                .Be(
                    await File.ReadAllTextAsync(Path.Combine(AdapterSourceDirectory, fileName)),
                    fileName
                );
        }
    }

    [Fact]
    public async Task WithTheOption_AddsThePackageReferenceAndTheRazorUsing()
    {
        var projectFile = await WriteProjectAsync(_root, "App.csproj", "Microsoft.NET.Sdk.Web");

        await InitAsync(_root, withFluentValidation: true);

        FluentValidationReferences(projectFile)
            .Should()
            .ContainSingle()
            .Which.Attribute("Version")!
            .Value.Should()
            .Be(AdapterFluentValidationVersion());
        (await File.ReadAllTextAsync(Path.Combine(_root, "_Imports.razor")))
            .Should()
            .Contain("@using global::Vibe.UI.FluentValidation");
    }

    [Fact]
    public async Task WithTheOption_AddsOneLineToTheProjectFileAndLeavesTheRestAsItWas()
    {
        var projectFile = await WriteProjectAsync(_root, "App.csproj", "Microsoft.NET.Sdk.Web");
        var before = Lines(await File.ReadAllTextAsync(projectFile));

        await InitAsync(_root, withFluentValidation: true);

        var after = Lines(await File.ReadAllTextAsync(projectFile));
        var added =
            $"    <PackageReference Include=\"FluentValidation\" Version=\"{AdapterFluentValidationVersion()}\" />";
        after.Should().Equal([.. before[..3], added, .. before[3..]]);
    }

    [Fact]
    public async Task WithTheOption_InAProjectWithNoPackageReferences_AddsAnItemGroup()
    {
        var projectFile = Path.Combine(_root, "App.csproj");
        await File.WriteAllTextAsync(
            projectFile,
            "<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\">\n</Project>\n"
        );

        var result = await InitAsync(_root, withFluentValidation: true);

        result.Should().Be(0);
        FluentValidationReferences(projectFile).Should().ContainSingle();
    }

    [Fact]
    public async Task WithTheOption_InAHostedApp_InstallsIntoTheClientOnly()
    {
        var serverPath = Path.Combine(_root, "App");
        var clientPath = Path.Combine(_root, "App.Client");
        Directory.CreateDirectory(serverPath);
        Directory.CreateDirectory(clientPath);
        var serverProject = Path.Combine(serverPath, "App.csproj");
        await File.WriteAllTextAsync(
            serverProject,
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <ProjectReference Include="..\App.Client\App.Client.csproj" />
                <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="10.0.0" />
              </ItemGroup>
            </Project>
            """
        );
        var serverProjectBefore = await File.ReadAllTextAsync(serverProject);
        var clientProject = await WriteProjectAsync(
            clientPath,
            "App.Client.csproj",
            "Microsoft.NET.Sdk.BlazorWebAssembly"
        );

        var result = await InitAsync(_root, withFluentValidation: true);

        result.Should().Be(0);
        Directory
            .GetFiles(Path.Combine(clientPath, "Vibe", "FluentValidation"))
            .Should()
            .HaveCount(FluentValidationAdapterInstaller.SourceFiles.Count);
        FluentValidationReferences(clientProject).Should().ContainSingle();
        Directory.Exists(Path.Combine(serverPath, "Vibe")).Should().BeFalse();
        (await File.ReadAllTextAsync(serverProject)).Should().Be(serverProjectBefore);
    }

    [Fact]
    public async Task WithTheOption_LeavesAnExistingFluentValidationReferenceAlone()
    {
        var projectFile = Path.Combine(_root, "App.csproj");
        await File.WriteAllTextAsync(
            projectFile,
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <PackageReference Include="FluentValidation" Version="11.0.0" />
              </ItemGroup>
            </Project>
            """
        );

        await InitAsync(_root, withFluentValidation: true);

        FluentValidationReferences(projectFile)
            .Should()
            .ContainSingle()
            .Which.Attribute("Version")!
            .Value.Should()
            .Be("11.0.0");
    }

    [Fact]
    public async Task WithTheOption_RunTwice_AddsOneReferenceAndOneUsing()
    {
        var projectFile = await WriteProjectAsync(_root, "App.csproj", "Microsoft.NET.Sdk.Web");

        await InitAsync(_root, withFluentValidation: true);
        await InitAsync(_root, withFluentValidation: true);

        FluentValidationReferences(projectFile).Should().ContainSingle();
        var imports = await File.ReadAllTextAsync(Path.Combine(_root, "_Imports.razor"));
        imports
            .Split('\n')
            .Count(line => line.Trim() == FluentValidationAdapterInstaller.RazorUsing)
            .Should()
            .Be(1);
    }

    [Fact]
    public void PlanProjectChanges_WithoutAProjectFile_Refuses()
    {
        var act = () =>
            FluentValidationAdapterInstaller.PlanProjectChanges(_root, projectFile: null);

        act.Should().Throw<InvalidOperationException>().WithMessage("*.csproj*");
    }

    private async Task<int> InitAsync(string projectPath, bool withFluentValidation) =>
        await _command.ExecuteAsync(
            new CommandContext([], new TestRemainingArguments(), "init", null),
            new InitCommand.Settings
            {
                SkipPrompts = true,
                ProjectPath = projectPath,
                WithFluentValidation = withFluentValidation,
            }
        );

    private static async Task<string> WriteProjectAsync(
        string directory,
        string fileName,
        string sdk
    )
    {
        var path = Path.Combine(directory, fileName);
        await File.WriteAllTextAsync(
            path,
            $"""
            <Project Sdk="{sdk}">
              <ItemGroup>
                <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="10.0.0" />
              </ItemGroup>
            </Project>
            """
        );
        return path;
    }

    private static string[] Lines(string text) =>
        text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

    private static IReadOnlyList<XElement> FluentValidationReferences(string projectFile) =>
        XDocument
            .Load(projectFile)
            .Descendants("PackageReference")
            .Where(reference => reference.Attribute("Include")?.Value == "FluentValidation")
            .ToList();

    private static string AdapterFluentValidationVersion() =>
        XDocument
            .Load(Path.Combine(AdapterSourceDirectory, "Vibe.UI.FluentValidation.csproj"))
            .Descendants("PackageReference")
            .Single(reference => reference.Attribute("Include")?.Value == "FluentValidation")
            .Attribute("Version")!
            .Value;

    private static string FindRepositoryRoot()
    {
        for (
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            directory is not null;
            directory = directory.Parent
        )
        {
            if (File.Exists(Path.Combine(directory.FullName, "Vibe.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Could not find the repository root (Vibe.slnx).");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }
}
