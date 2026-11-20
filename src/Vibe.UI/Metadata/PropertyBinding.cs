using Microsoft.AspNetCore.Components.Forms;

namespace Vibe.UI.Metadata;

/// <summary>
/// One property on one model instance: its current value, its metadata and the
/// <see cref="FieldIdentifier"/> that an <see cref="EditContext"/> knows it by.
/// </summary>
/// <typeparam name="TValue">The type of the property's value.</typeparam>
public sealed class PropertyBinding<TValue>
{
    internal PropertyBinding(
        object model,
        PropertyAccessor<TValue> accessor,
        PropertyMetadata metadata
    )
    {
        Model = model;
        Accessor = accessor;
        Metadata = metadata;
        Field = new FieldIdentifier(model, accessor.Property.Name);
    }

    /// <summary>The instance that owns the property.</summary>
    public object Model { get; }

    /// <summary>The cached accessor shared by every binding to this property.</summary>
    public PropertyAccessor<TValue> Accessor { get; }

    /// <summary>The resolved metadata for the property.</summary>
    public PropertyMetadata Metadata { get; }

    /// <summary>Identifies the field to an <see cref="EditContext"/>, for validation messages and change notification.</summary>
    public FieldIdentifier Field { get; }

    /// <summary>
    /// Gets or sets the property's value on <see cref="Model"/>. Setting writes in place, including
    /// to an <c>init</c> property, so the model instance and its <see cref="EditContext"/> stay the same.
    /// </summary>
    public TValue Value
    {
        get => Accessor.GetValue(Model);
        set => Accessor.SetValue(Model, value);
    }
}
