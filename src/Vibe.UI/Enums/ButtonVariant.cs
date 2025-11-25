namespace Vibe.UI.Enums;

/// <summary>
/// Defines the visual style variants for button components.
/// Each variant provides a different visual hierarchy and user interaction pattern.
/// </summary>
public enum ButtonVariant
{
    /// <summary>
    /// Primary button style.
    /// Used for the main call-to-action in a view. Should be used sparingly to maintain visual hierarchy.
    /// Typically features a solid background with high contrast.
    /// </summary>
    Primary,

    /// <summary>
    /// Secondary button style.
    /// Used for secondary actions that are important but not the primary focus.
    /// Provides a less prominent appearance than Primary.
    /// </summary>
    Secondary,

    /// <summary>
    /// Destructive button style.
    /// Used for actions that are irreversible or potentially dangerous, such as delete or remove operations.
    /// Typically uses red or warning colors to indicate caution.
    /// </summary>
    Destructive,

    /// <summary>
    /// Outline button style.
    /// Features a transparent background with a border.
    /// Useful for secondary actions or to reduce visual weight while maintaining button structure.
    /// </summary>
    Outline,

    /// <summary>
    /// Ghost button style.
    /// Minimal styling with no border or background in the default state.
    /// Provides the least visual weight while still indicating interactivity.
    /// </summary>
    Ghost,

    /// <summary>
    /// Link button style.
    /// Appears as a text link but maintains button semantics and accessibility.
    /// Used when you need button behavior but want the appearance of a hyperlink.
    /// </summary>
    Link
}
