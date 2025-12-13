namespace Vibe.UI.Enums;

/// <summary>
/// Defines the visual style variants for input components.
/// Each variant provides a different visual treatment for text inputs, textareas, and similar form controls.
/// </summary>
public enum InputVariant
{
    /// <summary>
    /// Text input variant.
    /// Minimal styling with no visible border or background in the default state.
    /// Typically shows a bottom border or underline to indicate the input field.
    /// </summary>
    Text,

    /// <summary>
    /// Filled input variant.
    /// Features a filled background color to distinguish the input field.
    /// Provides good contrast while maintaining a modern, clean appearance.
    /// </summary>
    Filled,

    /// <summary>
    /// Outlined input variant.
    /// Features a visible border around the entire input field.
    /// Provides clear boundaries and is the most traditional input appearance.
    /// </summary>
    Outlined
}
