namespace Vibe.UI.CLI.Models;

public class VibeConfig
{
    public string ProjectType { get; set; } = "Blazor";
    public string Theme { get; set; } = "light";
    public string ComponentsDirectory { get; set; } = "Components";
    public bool CssVariables { get; set; } = true;
    public string? Tailwind { get; set; }
    public Dictionary<string, string>? Aliases { get; set; }

    /// <summary>
    /// The CLI version that last wrote the project's Vibe/ infrastructure, set by
    /// <c>vibe init</c> and <c>vibe update --infrastructure</c>. Null for projects initialised
    /// before it was recorded.
    /// </summary>
    public string? InfrastructureVersion { get; set; }
}
