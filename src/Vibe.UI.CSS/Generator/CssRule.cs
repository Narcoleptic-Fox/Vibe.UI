namespace Vibe.UI.CSS.Generator;

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
    /// <summary>
    /// Base-level rules (order: 0)
    /// </summary>
    public const int Base = 0;

    /// <summary>
    /// Layout rules for display and positioning (order: 100)
    /// </summary>
    public const int Layout = 100;

    /// <summary>
    /// Flexbox layout rules (order: 200)
    /// </summary>
    public const int Flexbox = 200;

    /// <summary>
    /// Grid layout rules (order: 300)
    /// </summary>
    public const int Grid = 300;

    /// <summary>
    /// Spacing rules for margin and padding (order: 400)
    /// </summary>
    public const int Spacing = 400;

    /// <summary>
    /// Sizing rules for width and height (order: 500)
    /// </summary>
    public const int Sizing = 500;

    /// <summary>
    /// Typography rules for text styling (order: 600)
    /// </summary>
    public const int Typography = 600;

    /// <summary>
    /// Background styling rules (order: 700)
    /// </summary>
    public const int Background = 700;

    /// <summary>
    /// Border and outline rules (order: 800)
    /// </summary>
    public const int Border = 800;

    /// <summary>
    /// Visual effects like shadows and transforms (order: 900)
    /// </summary>
    public const int Effects = 900;

    /// <summary>
    /// Interactivity rules for cursor and pointer events (order: 1000)
    /// </summary>
    public const int Interactivity = 1000;

    /// <summary>
    /// State variant rules like hover:, focus:, active: (order: 2000)
    /// </summary>
    public const int StateVariants = 2000;

    /// <summary>
    /// Responsive variant rules like sm:, md:, lg: (order: 3000)
    /// </summary>
    public const int ResponsiveVariants = 3000;
}

