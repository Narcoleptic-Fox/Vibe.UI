using Microsoft.AspNetCore.Components.Web;

namespace Vibe.UI.Components;

/// <summary>
/// What a toggle group needs from one of its items, without knowing the item's value type.
/// </summary>
internal interface IToggleGroupItem
{
    /// <summary>The id of the item's button, used for roving focus.</summary>
    string ElementId { get; }

    /// <summary>Whether the item, or the group around it, is disabled.</summary>
    bool IsEffectivelyDisabled { get; }

    /// <summary>The item's value, in whatever type the item was compiled with.</summary>
    object? ItemValue { get; }
}

/// <summary>
/// What a toggle item needs from the group around it, without knowing the group's value type.
/// An item's own type is not always the group's; see <see cref="Vibe.UI.Base.ItemValue"/>.
/// </summary>
internal interface IToggleGroupContext
{
    /// <summary>Whether the whole group is disabled.</summary>
    bool Disabled { get; }

    /// <summary>Whether <paramref name="itemValue"/> is pressed.</summary>
    bool IsSelected(object? itemValue);

    /// <summary>Presses or releases the item with <paramref name="itemValue"/> and reports it.</summary>
    Task ToggleAsync(object? itemValue);

    /// <summary>The tab index <paramref name="item"/> should render: 0 for the one tab stop.</summary>
    int GetItemTabIndex(IToggleGroupItem item);

    /// <summary>Records that <paramref name="item"/> has focus.</summary>
    void HandleItemFocus(IToggleGroupItem item);

    /// <summary>Moves focus between items for arrow, Home and End keys.</summary>
    Task HandleItemKeyDown(IToggleGroupItem item, KeyboardEventArgs args);

    /// <summary>Adds <paramref name="item"/> to the group.</summary>
    void RegisterItem(IToggleGroupItem item);

    /// <summary>Removes <paramref name="item"/> from the group.</summary>
    void RemoveItem(IToggleGroupItem item);

    /// <summary>Tells the group that <paramref name="item"/>'s value or disabled state changed.</summary>
    void NotifyItemChanged(IToggleGroupItem item);
}
