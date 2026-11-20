using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Vibe.UI.Metadata;

/// <summary>
/// Reads the attributes an app may already have on its models: <see cref="DisplayAttribute"/>,
/// <see cref="RequiredAttribute"/>, the length and range attributes, <see cref="DisplayFormatAttribute"/>
/// and <see cref="DataTypeAttribute"/>.
/// </summary>
public sealed class DataAnnotationsMetadataProvider : IPropertyMetadataProvider
{
    /// <inheritdoc />
    public int Order => PropertyMetadataOrder.DataAnnotations;

    /// <inheritdoc />
    public PropertyMetadata? GetMetadata(Type modelType, PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);

        DisplayAttribute? display = First<DisplayAttribute>(property);
        StringLengthAttribute? stringLength = First<StringLengthAttribute>(property);
        RangeAttribute? range = First<RangeAttribute>(property);

        var metadata = new PropertyMetadata
        {
            // GetName and friends resolve a ResourceType when one is set, so localised labels work.
            Label =
                display?.GetName()
                ?? First<DisplayNameAttribute>(property)?.DisplayName,
            Description = display?.GetDescription(),
            Placeholder = display?.GetPrompt(),
            Required = property.IsDefined(typeof(RequiredAttribute)) ? true : null,
            MinLength =
                Positive(stringLength?.MinimumLength)
                ?? First<MinLengthAttribute>(property)?.Length,
            MaxLength =
                Positive(stringLength?.MaximumLength)
                ?? Positive(First<MaxLengthAttribute>(property)?.Length),
            Minimum = range?.Minimum,
            Maximum = range?.Maximum,
            Format = First<DisplayFormatAttribute>(property)?.DataFormatString,
            // EmailAddress, Phone, Url and CreditCard all derive from DataTypeAttribute.
            DataType = DataTypeOf(property),
        };

        return metadata == PropertyMetadata.Empty ? null : metadata;
    }

    // GetCustomAttribute<T> throws when a property carries two attributes assignable to T, which is
    // ordinary for DataTypeAttribute. The throw would also escape the cache and recur on every render.
    private static T? First<T>(PropertyInfo property)
        where T : Attribute => property.GetCustomAttributes<T>(inherit: true).FirstOrDefault();

    // An explicit [DataType] states intent; [EmailAddress] and its siblings are validators first.
    private static DataType? DataTypeOf(PropertyInfo property)
    {
        DataTypeAttribute[] all = [.. property.GetCustomAttributes<DataTypeAttribute>(inherit: true)];
        return (all.FirstOrDefault(a => a.GetType() == typeof(DataTypeAttribute)) ?? all.FirstOrDefault())?.DataType;
    }

    // StringLength reports 0 for "no minimum" and MaxLength reports -1 for "no maximum".
    private static int? Positive(int? value) => value > 0 ? value : null;
}
