namespace Vibe.UI.Components;

/// <summary>
/// One choice in a <c>Combobox</c>: the value it stands for and the text shown.
/// </summary>
/// <typeparam name="TValue">The type of the value the combobox binds.</typeparam>
public class ComboboxOption<TValue>
{
    /// <summary>
    /// Gets or sets the text shown for the option, and matched against what the user types.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value reported when the option is picked.
    /// </summary>
    public TValue? Value { get; set; }

    /// <summary>
    /// Gets or sets whether the option is left out of the list.
    /// </summary>
    public bool Disabled { get; set; }
}
