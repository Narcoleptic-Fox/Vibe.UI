namespace Vibe.CSS.Generator;

/// <summary>
/// Represents a single CSS rule with selector and declarations.
/// </summary>
public record CssRule
{
    /// <summary>
    /// The CSS selector (e.g., ".vibe-flex", ".vibe-bg-red-500:hover")
    /// </summary>
    public required string Selector { get; init; }

    /// <summary>
    /// The CSS declarations (e.g., "display: flex;")
    /// </summary>
    public required string Declarations { get; init; }

    /// <summary>
    /// Optional media query wrapper (e.g., "@media (min-width: 768px)")
    /// </summary>
    public string? MediaQuery { get; init; }

    /// <summary>
    /// Sort order for proper cascade (lower = earlier in output)
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Render the rule as CSS
    /// </summary>
    public string ToCss()
    {
        var rule = $"{Selector} {{ {Declarations} }}";

        if (!string.IsNullOrEmpty(MediaQuery))
        {
            return $"{MediaQuery} {{ {rule} }}";
        }

        return rule;
    }
}

/// <summary>
/// CSS rule ordering categories for proper cascade
/// </summary>
public static class CssOrder
{
    public const int Base = 0;
    public const int Layout = 100;
    public const int Flexbox = 200;
    public const int Grid = 300;
    public const int Spacing = 400;
    public const int Sizing = 500;
    public const int Typography = 600;
    public const int Background = 700;
    public const int Border = 800;
    public const int Effects = 900;
    public const int Interactivity = 1000;
    public const int StateVariants = 2000;      // hover:, focus:, etc.
    public const int ResponsiveVariants = 3000; // sm:, md:, lg:, etc.
}
