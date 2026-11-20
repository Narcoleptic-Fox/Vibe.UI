namespace Vibe.UI.Docs.Services;

/// <summary>
/// The install instructions the site shows, kept in one place so the home page and the guides
/// cannot drift apart. The 1.0.0-beta is not published to NuGet, so the tested path is to build
/// the CLI from the repository (the same steps as the README).
/// </summary>
public static class InstallCommands
{
    public const string RepositoryUrl = "https://github.com/Narcoleptic-Fox/Vibe.UI";

    public const string Clone = "git clone https://github.com/Narcoleptic-Fox/Vibe.UI.git";

    public const string PackCli = "dotnet pack src/Vibe.UI.CLI -c Release -o ./artifacts/nupkg";

    public const string InstallCliFromPackage =
        "dotnet tool install -g Vibe.UI.CLI --add-source ./artifacts/nupkg --version 1.0.0-beta";

    /// <summary>Clone, pack and install the CLI, one command per line.</summary>
    public const string CliFromSource =
        Clone + "\n" + "cd Vibe.UI\n" + PackCli + "\n" + InstallCliFromPackage;

    /// <summary>Reference the component library from a clone instead of copying components.</summary>
    public const string ReferenceLibrary =
        "dotnet add reference path/to/Vibe.UI/src/Vibe.UI/Vibe.UI.csproj";
}
