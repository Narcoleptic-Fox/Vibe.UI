using System.ComponentModel.DataAnnotations;

namespace Vibe.UI.Metadata;

/// <summary>
/// What is known about a model property for display and input. Every facet is nullable: <c>null</c>
/// means "this source did not answer", which lets a lower layer in the chain answer instead.
/// </summary>
public sealed record PropertyMetadata
{
    /// <summary>A metadata value in which no facet is answered.</summary>
    public static PropertyMetadata Empty { get; } = new();

    /// <summary>The text shown beside an input and as a column title.</summary>
    public string? Label { get; init; }

    /// <summary>Longer help text shown under an input.</summary>
    public string? Description { get; init; }

    /// <summary>Placeholder text shown inside an empty input.</summary>
    public string? Placeholder { get; init; }

    /// <summary>Whether a value must be supplied.</summary>
    public bool? Required { get; init; }

    /// <summary>The minimum length of a string value.</summary>
    public int? MinLength { get; init; }

    /// <summary>The maximum length of a string value.</summary>
    public int? MaxLength { get; init; }

    /// <summary>The smallest allowed value, in the property's own type where the source knows it.</summary>
    public object? Minimum { get; init; }

    /// <summary>The largest allowed value, in the property's own type where the source knows it.</summary>
    public object? Maximum { get; init; }

    /// <summary>A format string for display, such as <c>C0</c> or <c>yyyy-MM-dd</c>.</summary>
    public string? Format { get; init; }

    /// <summary>The semantic kind of the value, such as an email address or a password.</summary>
    public DataType? DataType { get; init; }

    /// <summary>A component type that should render this property in place of the default for its type.</summary>
    public Type? Control { get; init; }

    /// <summary>
    /// Returns this metadata with every unanswered facet filled from <paramref name="lower"/>.
    /// Facets are merged one by one, so a source that sets only a label never hides another
    /// source's <see cref="Required"/> or <see cref="MaxLength"/>.
    /// </summary>
    /// <param name="lower">The lower-priority metadata to fall back to.</param>
    public PropertyMetadata OverlayOn(PropertyMetadata lower)
    {
        ArgumentNullException.ThrowIfNull(lower);
        return new PropertyMetadata
        {
            Label = Label ?? lower.Label,
            Description = Description ?? lower.Description,
            Placeholder = Placeholder ?? lower.Placeholder,
            Required = Required ?? lower.Required,
            MinLength = MinLength ?? lower.MinLength,
            MaxLength = MaxLength ?? lower.MaxLength,
            Minimum = Minimum ?? lower.Minimum,
            Maximum = Maximum ?? lower.Maximum,
            Format = Format ?? lower.Format,
            DataType = DataType ?? lower.DataType,
            Control = Control ?? lower.Control,
        };
    }
}
