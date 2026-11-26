namespace Vibe.UI.CLI.Services;

public static class RazorImportsService
{
    private static readonly string[] InfrastructureUsings =
    [
        "@using global::Vibe.UI",
        "@using global::Vibe.UI.Base",
        "@using global::Vibe.UI.Enums"
    ];

    private const string ComponentsUsing = "@using global::Vibe.UI.Components";

    public static Task EnsureVibeImportsAsync(string projectPath, bool includeComponents) =>
        EnsureUsingsAsync(
            projectPath,
            includeComponents
                ? InfrastructureUsings.Append(ComponentsUsing).ToArray()
                : InfrastructureUsings);

    /// <summary>
    /// Appends each of <paramref name="requiredUsings"/> to the project's root _Imports.razor
    /// unless it is already there, creating the file if needed.
    /// </summary>
    public static async Task EnsureUsingsAsync(string projectPath, IReadOnlyList<string> requiredUsings)
    {
        var projectRoot = GetProjectRoot(projectPath);
        var importsPath = Path.Combine(projectRoot, "_Imports.razor");

        if (!File.Exists(importsPath))
        {
            await File.WriteAllTextAsync(
                importsPath,
                string.Join(Environment.NewLine, requiredUsings));
            return;
        }

        var existing = await File.ReadAllTextAsync(importsPath);
        var linesToAppend = requiredUsings
            .Where(line => existing.IndexOf(line, StringComparison.OrdinalIgnoreCase) < 0)
            .ToArray();

        if (linesToAppend.Length == 0)
        {
            return;
        }

        var separator = existing.EndsWith(Environment.NewLine, StringComparison.Ordinal)
            ? string.Empty
            : Environment.NewLine;

        await File.WriteAllTextAsync(
            importsPath,
            existing + separator + string.Join(Environment.NewLine, linesToAppend));
    }

    private static string GetProjectRoot(string projectPath)
    {
        var fullPath = Path.GetFullPath(projectPath);
        if (File.Exists(fullPath))
        {
            return Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory();
        }

        if (!Directory.Exists(fullPath))
        {
            return fullPath;
        }

        var projectFile = Directory.GetFiles(fullPath, "*.csproj", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();

        return projectFile == null
            ? fullPath
            : Path.GetDirectoryName(projectFile) ?? fullPath;
    }
}
