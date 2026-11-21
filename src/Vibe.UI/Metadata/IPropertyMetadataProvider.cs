using System.Reflection;

namespace Vibe.UI.Metadata;

/// <summary>
/// One source of property metadata. Sources are consulted in ascending <see cref="Order"/>, and for
/// each facet the first source that answers wins.
/// </summary>
public interface IPropertyMetadataProvider
{
    /// <summary>
    /// The position of this source in the chain. Lower values win. See <see cref="PropertyMetadataOrder"/>.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Returns what this source knows about a property, or <c>null</c> when it knows nothing.
    /// The result is cached per model type and property, so it must not depend on an instance.
    /// </summary>
    /// <param name="modelType">The type the property was reached through, which may derive from its declaring type.</param>
    /// <param name="property">The property, taken from an expression.</param>
    PropertyMetadata? GetMetadata(Type modelType, PropertyInfo property);
}

/// <summary>
/// The fixed positions of the built-in metadata sources. A higher layer has a lower number.
/// Parameters given at the call site sit above all of these and are applied by the component.
/// </summary>
public static class PropertyMetadataOrder
{
    /// <summary>A model profile written with lambdas at startup.</summary>
    public const int Profile = 100;

    /// <summary>Reserved for a FluentValidation validator, supplied by an optional package.</summary>
    public const int Validator = 200;

    /// <summary>DataAnnotations attributes on the property.</summary>
    public const int DataAnnotations = 300;

    /// <summary>Convention: a label derived from the property name.</summary>
    public const int Convention = 400;
}
