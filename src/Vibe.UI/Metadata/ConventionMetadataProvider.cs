using System.Reflection;
using System.Text;

namespace Vibe.UI.Metadata;

/// <summary>
/// The last source in the chain: derives a label from the property name, so every property has one.
/// <c>FirstName</c> becomes "First name", <c>UserID</c> becomes "User ID".
/// </summary>
public sealed class ConventionMetadataProvider : IPropertyMetadataProvider
{
    /// <inheritdoc />
    public int Order => PropertyMetadataOrder.Convention;

    /// <inheritdoc />
    public PropertyMetadata? GetMetadata(Type modelType, PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);
        return new PropertyMetadata { Label = ToLabel(property.Name) };
    }

    /// <summary>
    /// Splits a Pascal-case name into words in sentence case. A run of capitals is kept as an acronym.
    /// </summary>
    /// <param name="name">A property name.</param>
    public static string ToLabel(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var label = new StringBuilder(name.Length + 4);
        int start = 0;
        for (int i = 1; i <= name.Length; i++)
        {
            if (i < name.Length && !IsWordBoundary(name, i))
                continue;

            // Underscores separate words and are never part of one.
            string word = name[start..i].Trim('_');
            start = i;
            if (word.Length == 0)
                continue;
            bool acronym = word.Length > 1 && word.All(char.IsUpper);
            if (label.Length > 0)
                label.Append(' ').Append(acronym ? word : word.ToLowerInvariant());
            else
                label.Append(
                    acronym ? word : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()
                );
        }

        // A name made only of underscores has no words; show it as it is.
        return label.Length > 0 ? label.ToString() : name;
    }

    private static bool IsWordBoundary(string name, int index)
    {
        char previous = name[index - 1];
        char current = name[index];

        if (current == '_')
            return true;
        if (previous == '_')
            return true;
        if (char.IsDigit(current) != char.IsDigit(previous))
            return true;
        if (char.IsLower(previous) && char.IsUpper(current))
            return true;
        // The last capital of a run starts a new word when a lower-case letter follows: "HTMLParser".
        return char.IsUpper(previous)
            && char.IsUpper(current)
            && index + 1 < name.Length
            && char.IsLower(name[index + 1]);
    }
}
