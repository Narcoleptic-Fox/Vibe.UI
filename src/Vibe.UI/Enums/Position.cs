namespace Vibe.UI.Enums;

/// <summary>
/// Defines the positioning options for overlay components such as tooltips, popovers, and dropdowns.
/// Controls where the overlay element appears relative to its trigger or anchor element.
/// </summary>
public enum Position
{
    /// <summary>
    /// Top position.
    /// Positions the element above the anchor, centered horizontally.
    /// </summary>
    Top,

    /// <summary>
    /// Bottom position.
    /// Positions the element below the anchor, centered horizontally.
    /// </summary>
    Bottom,

    /// <summary>
    /// Left position.
    /// Positions the element to the left of the anchor, centered vertically.
    /// </summary>
    Left,

    /// <summary>
    /// Right position.
    /// Positions the element to the right of the anchor, centered vertically.
    /// </summary>
    Right,

    /// <summary>
    /// Top-left position.
    /// Positions the element above and aligned to the left edge of the anchor.
    /// </summary>
    TopLeft,

    /// <summary>
    /// Top-right position.
    /// Positions the element above and aligned to the right edge of the anchor.
    /// </summary>
    TopRight,

    /// <summary>
    /// Bottom-left position.
    /// Positions the element below and aligned to the left edge of the anchor.
    /// </summary>
    BottomLeft,

    /// <summary>
    /// Bottom-right position.
    /// Positions the element below and aligned to the right edge of the anchor.
    /// </summary>
    BottomRight
}
