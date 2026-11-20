using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Vibe.UI.Metadata;

/// <summary>
/// Model profiles: a way to describe a model's properties with lambdas at startup, without putting
/// attributes on a type the client does not own.
/// </summary>
public sealed class VibeMetadataOptions
{
    private readonly Dictionary<(Type Model, string Property), PropertyMetadata> _profiles = [];

    /// <summary>Describes properties of <typeparamref name="TModel"/>. A profile also applies to types derived from it.</summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="configure">Describes the properties.</param>
    public VibeMetadataOptions Model<TModel>(Action<ModelProfileBuilder<TModel>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(new ModelProfileBuilder<TModel>(this));
        return this;
    }

    internal PropertyMetadata? Find(Type modelType, string propertyName)
    {
        // The most derived profile is the most specific, so it is laid over its base types' profiles.
        PropertyMetadata? found = null;
        for (Type? type = modelType; type is not null; type = type.BaseType)
        {
            if (_profiles.TryGetValue((type, propertyName), out PropertyMetadata? profile))
                found = found is null ? profile : found.OverlayOn(profile);
        }
        return found;
    }

    internal void Update(
        Type modelType,
        PropertyInfo property,
        Func<PropertyMetadata, PropertyMetadata> change
    )
    {
        var key = (modelType, property.Name);
        _profiles[key] = change(_profiles.GetValueOrDefault(key, PropertyMetadata.Empty));
    }
}

/// <summary>Selects the properties of a model to describe.</summary>
/// <typeparam name="TModel">The model type.</typeparam>
public sealed class ModelProfileBuilder<TModel>
{
    private readonly VibeMetadataOptions _options;

    internal ModelProfileBuilder(VibeMetadataOptions options) => _options = options;

    /// <summary>Starts describing one property.</summary>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">The property, as <c>x =&gt; x.Name</c>.</param>
    public PropertyProfileBuilder<TModel, TValue> Property<TValue>(
        Expression<Func<TModel, TValue>> property
    ) => new(_options, PropertyExpression.GetDirectProperty(property));
}

/// <summary>Describes one property. Each call sets one facet and leaves the others to lower layers.</summary>
/// <typeparam name="TModel">The model type.</typeparam>
/// <typeparam name="TValue">The property type.</typeparam>
public sealed class PropertyProfileBuilder<TModel, TValue>
{
    private readonly VibeMetadataOptions _options;
    private readonly PropertyInfo _property;

    internal PropertyProfileBuilder(VibeMetadataOptions options, PropertyInfo property)
    {
        _options = options;
        _property = property;
    }

    /// <summary>Sets the label.</summary>
    /// <param name="label">The label text.</param>
    public PropertyProfileBuilder<TModel, TValue> Label(string label) =>
        Set(m => m with { Label = label });

    /// <summary>Sets the help text.</summary>
    /// <param name="description">The help text.</param>
    public PropertyProfileBuilder<TModel, TValue> Description(string description) =>
        Set(m => m with { Description = description });

    /// <summary>Sets the placeholder.</summary>
    /// <param name="placeholder">The placeholder text.</param>
    public PropertyProfileBuilder<TModel, TValue> Placeholder(string placeholder) =>
        Set(m => m with { Placeholder = placeholder });

    /// <summary>Marks the property as required, or explicitly as optional.</summary>
    /// <param name="required">Whether a value must be supplied.</param>
    public PropertyProfileBuilder<TModel, TValue> Required(bool required = true) =>
        Set(m => m with { Required = required });

    /// <summary>Sets the minimum string length.</summary>
    /// <param name="length">The minimum length.</param>
    public PropertyProfileBuilder<TModel, TValue> MinLength(int length) =>
        Set(m => m with { MinLength = length });

    /// <summary>Sets the maximum string length.</summary>
    /// <param name="length">The maximum length.</param>
    public PropertyProfileBuilder<TModel, TValue> MaxLength(int length) =>
        Set(m => m with { MaxLength = length });

    /// <summary>Sets the allowed range, in the property's own type.</summary>
    /// <param name="minimum">The smallest allowed value.</param>
    /// <param name="maximum">The largest allowed value.</param>
    public PropertyProfileBuilder<TModel, TValue> Range(TValue minimum, TValue maximum) =>
        Set(m => m with { Minimum = minimum, Maximum = maximum });

    /// <summary>Sets the display format string.</summary>
    /// <param name="format">A format string such as <c>C0</c>.</param>
    public PropertyProfileBuilder<TModel, TValue> Format(string format) =>
        Set(m => m with { Format = format });

    /// <summary>Sets the semantic kind of the value.</summary>
    /// <param name="dataType">The kind, such as <see cref="DataType.EmailAddress"/>.</param>
    public PropertyProfileBuilder<TModel, TValue> DataType(DataType dataType) =>
        Set(m => m with { DataType = dataType });

    /// <summary>Chooses the component that renders this property.</summary>
    /// <typeparam name="TControl">The component type.</typeparam>
    public PropertyProfileBuilder<TModel, TValue> Control<TControl>()
        where TControl : IComponent => Set(m => m with { Control = typeof(TControl) });

    private PropertyProfileBuilder<TModel, TValue> Set(
        Func<PropertyMetadata, PropertyMetadata> change
    )
    {
        _options.Update(typeof(TModel), _property, change);
        return this;
    }
}

/// <summary>The source that serves model profiles from <see cref="VibeMetadataOptions"/>.</summary>
public sealed class ProfileMetadataProvider(VibeMetadataOptions options) : IPropertyMetadataProvider
{
    /// <inheritdoc />
    public int Order => PropertyMetadataOrder.Profile;

    /// <inheritdoc />
    public PropertyMetadata? GetMetadata(Type modelType, PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(modelType);
        ArgumentNullException.ThrowIfNull(property);
        return options.Find(modelType, property.Name);
    }
}
