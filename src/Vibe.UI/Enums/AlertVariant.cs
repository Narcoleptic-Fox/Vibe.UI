namespace Vibe.UI.Enums;

/// <summary>
/// Defines the visual style variants for alert and notification components.
/// Each variant communicates a different level of severity or type of message to the user.
/// </summary>
public enum AlertVariant
{
    /// <summary>
    /// Default alert style.
    /// Used for general informational messages that don't fit into other categories.
    /// Provides a neutral appearance without strong semantic meaning.
    /// </summary>
    Default,

    /// <summary>
    /// Destructive alert style.
    /// Used for error messages, failed operations, or critical issues that require attention.
    /// Typically uses red or error colors to indicate problems.
    /// </summary>
    Destructive,

    /// <summary>
    /// Success alert style.
    /// Used to confirm successful operations or positive outcomes.
    /// Typically uses green colors to indicate completion or success.
    /// </summary>
    Success,

    /// <summary>
    /// Info alert style.
    /// Used for informational messages that provide helpful context or guidance.
    /// Typically uses blue colors to indicate informational content.
    /// </summary>
    Info,

    /// <summary>
    /// Warning alert style.
    /// Used for warning messages that indicate potential issues or important notices.
    /// Alerts the user to situations that need attention but aren't critical errors.
    /// Typically uses yellow or orange colors to indicate caution.
    /// </summary>
    Warning
}
