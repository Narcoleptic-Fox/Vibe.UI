using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;

namespace Vibe.UI.Metadata;

/// <summary>
/// Turns a property expression into what a component needs: metadata, an accessor, and for a model
/// instance a <see cref="PropertyBinding{TValue}"/>. Results are cached per model type and property.
/// </summary>
public interface IPropertyMetadataResolver
{
    /// <summary>Resolves the metadata of a property through the provider chain.</summary>
    /// <param name="modelType">The type the property is reached through.</param>
    /// <param name="property">The property, taken from an expression.</param>
    PropertyMetadata GetMetadata(Type modelType, PropertyInfo property);

    /// <summary>Resolves the metadata of the property in <c>x =&gt; x.Name</c>. This is the form a table column uses.</summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">The property, as <c>x =&gt; x.Name</c>.</param>
    PropertyMetadata GetMetadata<TModel, TValue>(Expression<Func<TModel, TValue>> property);

    /// <summary>Returns the cached accessor for a property.</summary>
    /// <typeparam name="TValue">The type to read and write values as.</typeparam>
    /// <param name="property">The property, taken from an expression.</param>
    PropertyAccessor<TValue> GetAccessor<TValue>(PropertyInfo property);

    /// <summary>Binds the property in <c>() =&gt; model.Name</c> on its model instance. This is the form an input uses.</summary>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">The property, as <c>() =&gt; model.Name</c>.</param>
    PropertyBinding<TValue> Bind<TValue>(Expression<Func<TValue>> property);
}

/// <inheritdoc cref="IPropertyMetadataResolver" />
public sealed class PropertyMetadataResolver : IPropertyMetadataResolver
{
    private readonly IPropertyMetadataProvider[] _providers;

    // One resolver serves every circuit on Interactive Server, so both caches are concurrent.
    // Keys use the property name, taken from the expression's PropertyInfo, so an inherited property
    // reached through a derived type gets its own entry and its own profile lookup.
    private readonly ConcurrentDictionary<
        (Type Model, Type Declaring, string Name),
        PropertyMetadata
    > _metadata = new();
    private readonly ConcurrentDictionary<(PropertyInfo Property, Type Value), object> _accessors =
        new();

    /// <summary>Creates a resolver over the given sources.</summary>
    /// <param name="providers">The metadata sources, in any order.</param>
    public PropertyMetadataResolver(IEnumerable<IPropertyMetadataProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        _providers = [.. providers.OrderBy(provider => provider.Order)];
    }

    /// <inheritdoc />
    public PropertyMetadata GetMetadata(Type modelType, PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(modelType);
        ArgumentNullException.ThrowIfNull(property);
        return _metadata.GetOrAdd(
            (modelType, property.DeclaringType!, property.Name),
            static (_, state) => state.Resolver.Resolve(state.ModelType, state.Property),
            (Resolver: this, ModelType: modelType, Property: property)
        );
    }

    /// <inheritdoc />
    public PropertyMetadata GetMetadata<TModel, TValue>(
        Expression<Func<TModel, TValue>> property
    ) => GetMetadata(typeof(TModel), PropertyExpression.GetDirectProperty(property));

    /// <inheritdoc />
    public PropertyAccessor<TValue> GetAccessor<TValue>(PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);
        return (PropertyAccessor<TValue>)
            _accessors.GetOrAdd(
                (property, typeof(TValue)),
                static key => new PropertyAccessor<TValue>(key.Property)
            );
    }

    /// <inheritdoc />
    public PropertyBinding<TValue> Bind<TValue>(Expression<Func<TValue>> property)
    {
        var info = (PropertyInfo)PropertyExpression.GetPropertyAccess(property).Member;
        // The framework already knows how to find the owning instance of an accessor expression.
        object model = FieldIdentifier.Create(property).Model;
        return new PropertyBinding<TValue>(
            model,
            GetAccessor<TValue>(info),
            GetMetadata(model.GetType(), info)
        );
    }

    private PropertyMetadata Resolve(Type modelType, PropertyInfo property)
    {
        // Providers are sorted highest priority first, so each one only fills what is still unanswered.
        PropertyMetadata resolved = PropertyMetadata.Empty;
        foreach (IPropertyMetadataProvider provider in _providers)
        {
            if (provider.GetMetadata(modelType, property) is { } metadata)
                resolved = resolved.OverlayOn(metadata);
        }
        return resolved;
    }
}
