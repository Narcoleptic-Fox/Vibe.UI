namespace Vibe.UI.Components;

/// <summary>
/// What a radio item needs from the group around it, without knowing the group's value type.
/// An item written <c>Value="true"</c> inside a group bound to <c>bool?</c> is compiled as an item
/// of <c>bool</c>, so the two cannot find each other through a shared type argument.
/// </summary>
internal interface IRadioGroupContext
{
    /// <summary>The name shared by every radio input in the group.</summary>
    string Name { get; }

    /// <summary>Whether the whole group is disabled.</summary>
    bool Disabled { get; }

    /// <summary>Whether <paramref name="itemValue"/> is the group's current value.</summary>
    bool IsSelected(object? itemValue);

    /// <summary>Makes <paramref name="itemValue"/> the group's value and reports the change.</summary>
    Task SelectAsync(object? itemValue);
}
