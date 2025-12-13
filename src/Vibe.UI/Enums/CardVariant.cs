namespace Vibe.UI.Enums;

/// <summary>
/// Defines the visual style variants for card components.
/// Each variant provides a different visual treatment for content containers.
/// </summary>
public enum CardVariant
{
    /// <summary>
    /// Default card style.
    /// Standard card appearance with subtle styling.
    /// Provides a clean, neutral container for content without strong visual emphasis.
    /// </summary>
    Default,

    /// <summary>
    /// Elevated card style.
    /// Features a shadow or elevation effect to create depth and visual hierarchy.
    /// Makes the card appear to float above the page surface.
    /// </summary>
    Elevated,

    /// <summary>
    /// Outlined card style.
    /// Features a visible border without shadow or elevation.
    /// Provides clear boundaries while maintaining a flat, modern appearance.
    /// </summary>
    Outlined,

    /// <summary>
    /// Interactive card style.
    /// Designed for cards that respond to user interaction such as hover or click.
    /// Includes interactive states like hover effects to indicate clickability.
    /// Use when the entire card acts as a clickable element.
    /// </summary>
    Interactive
}
