namespace Vibe.UI.CLI.Models;

public class ComponentInfo
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string>? Dependencies { get; set; }

    /// <summary>
    /// Packages the component's source needs that are not Vibe.UI's own, by id. See
    /// <c>ComponentPackages</c>.
    /// </summary>
    public List<string>? Packages { get; set; }
    public List<string> Files { get; set; } = new();
    public string? Example { get; set; }
    public bool HasCss { get; set; } = true;
    public bool HasJavaScript { get; set; } = false;
}
