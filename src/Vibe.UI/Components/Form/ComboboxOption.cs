namespace Vibe.UI.Components;

/// <summary>
/// What a <see cref="Combobox{TValue}"/> reads from an option. It is covariant, so a list of
/// <c>ComboboxOption&lt;string&gt;</c> is accepted by a combobox bound to a <c>string?</c> field
/// without stating <c>TValue</c>.
/// </summary>
/// <typeparam name="TValue">The type of the option's value.</typeparam>
public interface IComboboxOption<out TValue>
{
    /// <summary>Gets the text shown for the option, and matched against what the user types.</summary>
    string Label { get; }

    /// <summary>Gets the value reported when the option is chosen.</summary>
    TValue? Value { get; }

    /// <summary>Gets whether the option is shown but cannot be chosen.</summary>
    bool Disabled { get; }
}

/// <summary>
/// One choice in a <c>Combobox</c>: the value it stands for and the text shown.
/// </summary>
/// <typeparam name="TValue">The type of the value the combobox binds.</typeparam>
public class ComboboxOption<TValue> : IComboboxOption<TValue>
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
