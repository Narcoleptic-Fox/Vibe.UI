namespace Vibe.UI.Enums;

/// <summary>
/// Whether a toggle group holds one value or several.
/// </summary>
public enum ToggleGroupType
{
    /// <summary>At most one item is pressed. The group binds <c>Value</c>.</summary>
    Single,

    /// <summary>Any number of items are pressed. The group binds <c>Values</c>.</summary>
    Multiple,
}

/// <summary>
/// The direction a toggle group lays its items out in, and which arrow keys move between them.
/// </summary>
public enum ToggleGroupOrientation
{
    /// <summary>Items sit side by side. The default.</summary>
    Horizontal,

    /// <summary>Items are stacked.</summary>
    Vertical,
}

/// <summary>
/// The size of a toggle group's items.
/// </summary>
public enum ToggleGroupSize
{
    /// <summary>Compact items.</summary>
    Small,

    /// <summary>The default size.</summary>
    Default,

    /// <summary>Larger items.</summary>
    Large,
}
