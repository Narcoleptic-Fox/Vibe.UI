using System.Globalization;

namespace Vibe.UI.Base;

/// <summary>
/// Turns the value an item was written with into the value type of the group around it.
/// </summary>
/// <remarks>
/// A group and its items are generic over the same type in intent, but not always in fact:
/// Razor gives an item the type of its own literal when it has one, so <c>Value="true"</c> inside a
/// group bound to <c>bool?</c> is an item of <c>bool</c>, and <c>Value="1"</c> inside a group of
/// <c>long</c> is an item of <c>int</c>. Groups therefore take item values as <c>object</c> and
/// convert them here.
/// </remarks>
internal static class ItemValue
{
    /// <summary>
    /// Converts <paramref name="itemValue"/> to <typeparamref name="TValue"/>.
    /// </summary>
    /// <param name="itemValue">The value the item carries, in whatever type it was compiled with.</param>
    /// <param name="groupName">The group component's name, for the error message.</param>
    /// <exception cref="InvalidOperationException">
    /// The value cannot be a <typeparamref name="TValue"/>. The author wrote it, so this is a mistake
    /// in the markup, not user input.
    /// </exception>
    public static TValue? Coerce<TValue>(object? itemValue, string groupName)
    {
        switch (itemValue)
        {
            case null:
                return default;
            case TValue typed:
                return typed;
        }

        var valueType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

        try
        {
            return valueType.IsEnum
                ? (TValue)Enum.ToObject(valueType, itemValue)
                : (TValue)Convert.ChangeType(itemValue, valueType, CultureInfo.InvariantCulture);
        }
        catch (Exception exception)
            when (exception
                    is InvalidCastException
                        or FormatException
                        or OverflowException
                        or ArgumentException
            )
        {
            throw new InvalidOperationException(
                $"An item has a value of type {itemValue.GetType().Name} ('{itemValue}'), which cannot be used in a "
                    + $"{groupName}<{typeof(TValue).Name}>. Give the item a value of the group's type.",
                exception
            );
        }
    }
}
