using System.Linq.Expressions;
using System.Reflection;

namespace Vibe.UI.Metadata;

/// <summary>
/// A compiled getter and setter for one property, usable with any instance of its declaring type.
/// Built once per property and value type, then cached by <see cref="IPropertyMetadataResolver"/>.
/// </summary>
/// <typeparam name="TValue">The type values are read and written as.</typeparam>
public sealed class PropertyAccessor<TValue>
{
    private readonly Func<object, TValue> _get;
    private readonly Action<object, TValue>? _set;

    internal PropertyAccessor(PropertyInfo property)
    {
        Property = property;
        Type declaringType = property.DeclaringType!;

        ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
        // Unbox yields a reference into the box, so a setter on a struct changes the boxed value itself.
        Expression typed = declaringType.IsValueType
            ? Expression.Unbox(instance, declaringType)
            : Expression.Convert(instance, declaringType);
        MemberExpression member = Expression.Property(typed, property);

        _get = Expression
            .Lambda<Func<object, TValue>>(ConvertTo(member, typeof(TValue)), instance)
            .Compile();

        if (property.SetMethod is { IsPublic: true })
        {
            // An init-only setter is an ordinary setter with a modreq, and expression trees do not
            // enforce it. That is what lets a form write to an immutable record in place.
            ParameterExpression value = Expression.Parameter(typeof(TValue), "value");
            _set = Expression
                .Lambda<Action<object, TValue>>(
                    Expression.Assign(member, ConvertTo(value, property.PropertyType)),
                    instance,
                    value
                )
                .Compile();
            IsInitOnly = property
                .SetMethod.ReturnParameter.GetRequiredCustomModifiers()
                .Any(modifier =>
                    modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit"
                );
        }
    }

    /// <summary>The property this accessor reads and writes.</summary>
    public PropertyInfo Property { get; }

    /// <summary>Whether the property has a public setter, including an <c>init</c> setter.</summary>
    public bool CanWrite => _set is not null;

    /// <summary>Whether the setter is declared <c>init</c>. Such a property can still be written through <see cref="SetValue"/>.</summary>
    public bool IsInitOnly { get; }

    /// <summary>Reads the property from <paramref name="model"/>.</summary>
    /// <param name="model">An instance of the property's declaring type.</param>
    public TValue GetValue(object model) => _get(model);

    /// <summary>Writes the property on <paramref name="model"/>, in place.</summary>
    /// <param name="model">An instance of the property's declaring type.</param>
    /// <param name="value">The value to assign.</param>
    /// <exception cref="InvalidOperationException">The property has no public setter.</exception>
    public void SetValue(object model, TValue value)
    {
        if (_set is null)
            throw new InvalidOperationException(
                $"Property {Property.DeclaringType?.Name}.{Property.Name} has no public setter and cannot be written."
            );
        _set(model, value);
    }

    private static Expression ConvertTo(Expression expression, Type type) =>
        expression.Type == type ? expression : Expression.Convert(expression, type);
}
