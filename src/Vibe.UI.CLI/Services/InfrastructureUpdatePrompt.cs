using Spectre.Console;

namespace Vibe.UI.CLI.Services;

/// <summary>
/// Decides which planned infrastructure files <c>vibe update --infrastructure</c> writes. For now
/// it is all or nothing, on the user's word: nothing records whether a copied file was edited,
/// so every file it would replace is listed and the user confirms. A per-file record of what the
/// CLI wrote would let this choose per file (skip or diff an edited one) without changing the
/// command around it.
/// </summary>
public static class InfrastructureUpdatePrompt
{
    /// <returns>The files to write, or null when the user declined.</returns>
    public static IReadOnlyList<PlannedFile>? ChooseFilesToWrite(
        string projectPath,
        IReadOnlyList<PlannedFile> files,
        FluentValidationAdapterInstaller.ProjectChanges? adapterChanges,
        bool skipPrompts
    )
    {
        var replaced = files
            .Where(file => File.Exists(Path.Combine(projectPath, file.Target)))
            .ToList();
        var added = files.Except(replaced).ToList();
        var edited = (adapterChanges?.EditedFiles().ToList() ?? [])
            // Records which CLI version wrote the infrastructure; nothing else in it changes.
            .Append(Path.Combine(projectPath, "vibe.json"))
            .ToList();

        AnsiConsole.MarkupLine(
            $"[blue]vibe update --infrastructure[/] will replace {replaced.Count} files and add {added.Count} in {Markup.Escape(projectPath)}."
        );
        AnsiConsole.MarkupLine(
            "[grey]Components, vibe.json's components directory and the colour in vibe-base.css are not touched.[/]"
        );
        WriteList("Replace", replaced.Select(file => file.Target));
        WriteList("Add", added.Select(file => file.Target));
        WriteList("Edit", edited.Select(file => Path.GetRelativePath(projectPath, file)));

        if (skipPrompts)
        {
            return files;
        }

        return AnsiConsole.Confirm(
            "Any change you made to the files listed under Replace will be lost. Continue?",
            defaultValue: false
        )
            ? files
            : null;
    }

    private static void WriteList(string heading, IEnumerable<string> paths)
    {
        var list = paths.ToList();
        if (list.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine($"\n[yellow]{heading}[/] ({list.Count})");
        foreach (var path in list)
        {
            AnsiConsole.WriteLine($"  {path}");
        }
    }
}
