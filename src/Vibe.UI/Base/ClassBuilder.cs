using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vibe.UI.Base;

/// <summary>
/// Provides a fluent API for building CSS class strings with support for conditional classes,
/// enum-based variants, and lazy evaluation. This builder is immutable and thread-safe.
/// </summary>
/// <remarks>
/// <para>
/// The ClassBuilder follows an immutable pattern where each method call returns a new instance
/// with the modified state, making it safe to use across multiple threads and enabling method chaining.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// protected override string ComponentClass => new ClassBuilder()
///     .Add("vibe-button")
///     .AddVariant("vibe-button", Variant)
///     .AddVariant("vibe-button", Size)
///     .AddIf("vibe-button-full-width", FullWidth)
///     .AddIf("vibe-button-loading", Loading)
///     .AddIf("vibe-button-icon-only", Icon != null &amp;&amp; ChildContent == null)
///     .AddClass(UserProvidedClass)
///     .Build();
/// </code>
/// </para>
/// </remarks>
public readonly struct ClassBuilder : IEquatable<ClassBuilder>
{
    private readonly List<string> _classes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassBuilder"/> struct.
    /// </summary>
    private ClassBuilder(List<string> classes)
    {
        _classes = classes;
    }

    /// <summary>
    /// Adds a CSS class to the builder.
    /// </summary>
    /// <param name="className">The CSS class name to add. If null or whitespace, it will be ignored.</param>
    /// <returns>A new <see cref="ClassBuilder"/> instance with the class added.</returns>
    /// <example>
    /// <code>
    /// var builder = new ClassBuilder()
    ///     .Add("vibe-button")
    ///     .Add("vibe-button-primary");
    /// </code>
    /// </example>
    public ClassBuilder Add(string? className)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            return this;
        }

        var newClasses = GetClassesList();
        newClasses.Add(className);
        return new ClassBuilder(newClasses);
    }

    /// <summary>
    /// Conditionally adds a CSS class to the builder based on a boolean condition.
    /// </summary>
    /// <param name="className">The CSS class name to add if the condition is true.</param>
    /// <param name="condition">The condition that determines whether the class should be added.</param>
    /// <returns>A new <see cref="ClassBuilder"/> instance with the class added if the condition is true.</returns>
    /// <example>
    /// <code>
    /// var builder = new ClassBuilder()
    ///     .Add("vibe-button")
    ///     .AddIf("vibe-button-disabled", IsDisabled)
    ///     .AddIf("vibe-button-loading", IsLoading);
    /// </code>
    /// </example>
    public ClassBuilder AddIf(string? className, bool condition)
    {
        if (!condition || string.IsNullOrWhiteSpace(className))
        {
            return this;
        }

        return Add(className);
    }

    /// <summary>
    /// Adds a CSS class to the builder based on a lazy-evaluated condition.
    /// The condition function is only evaluated if the builder is being constructed.
    /// </summary>
    /// <param name="className">The CSS class name to add if the condition evaluates to true.</param>
    /// <param name="condition">A function that returns the condition determining whether the class should be added.</param>
    /// <returns>A new <see cref="ClassBuilder"/> instance with the class added if the condition evaluates to true.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="condition"/> is null.</exception>
    /// <example>
    /// <code>
    /// var builder = new ClassBuilder()
    ///     .Add("vibe-button")
    ///     .AddWhen("vibe-button-icon-only", () => Icon != null &amp;&amp; ChildContent == null);
    /// </code>
    /// </example>
    public ClassBuilder AddWhen(string? className, Func<bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        return AddIf(className, condition());
    }

    /// <summary>
    /// Adds a CSS class based on an enum variant value.
    /// The class is constructed as "{prefix}-{enumValue}" where the enum value is converted to lowercase.
    /// </summary>
    /// <typeparam name="TEnum">The enum type. Must be an enum.</typeparam>
    /// <param name="prefix">The prefix for the CSS class (e.g., "vibe-button").</param>
    /// <param name="value">The enum value to append to the prefix.</param>
    /// <returns>A new <see cref="ClassBuilder"/> instance with the variant class added.</returns>
    /// <exception cref="ArgumentException">Thrown when <typeparamref name="TEnum"/> is not an enum type.</exception>
    /// <example>
    /// <code>
    /// public enum ButtonVariant { Primary, Secondary, Destructive }
    /// public enum ButtonSize { Small, Medium, Large }
    ///
    /// var builder = new ClassBuilder()
    ///     .Add("vibe-button")
    ///     .AddVariant("vibe-button", ButtonVariant.Primary)  // Adds "vibe-button-primary"
    ///     .AddVariant("vibe-button", ButtonSize.Large);      // Adds "vibe-button-large"
    /// </code>
    /// </example>
    public ClassBuilder AddVariant<TEnum>(string? prefix, TEnum value) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return this;
        }

        var enumValue = value.ToString().ToLowerInvariant();
        var className = $"{prefix}-{enumValue}";
        return Add(className);
    }

    /// <summary>
    /// Adds a user-provided CSS class to the builder.
    /// This is useful for allowing consumers to pass custom classes to components.
    /// </summary>
    /// <param name="userClass">The user-provided CSS class. If null or whitespace, it will be ignored.</param>
    /// <returns>A new <see cref="ClassBuilder"/> instance with the user class added.</returns>
    /// <example>
    /// <code>
    /// // In a component
    /// [Parameter]
    /// public string? Class { get; set; }
    ///
    /// protected override string ComponentClass => new ClassBuilder()
    ///     .Add("vibe-button")
    ///     .AddVariant("vibe-button", Variant)
    ///     .AddClass(Class);  // Adds user-provided classes
    /// </code>
    /// </example>
    public ClassBuilder AddClass(string? userClass)
    {
        return Add(userClass);
    }

    /// <summary>
    /// Builds and returns the final CSS class string.
    /// Multiple classes are separated by spaces, and empty/whitespace values are filtered out.
    /// </summary>
    /// <returns>A space-separated string of CSS classes, or an empty string if no classes were added.</returns>
    /// <example>
    /// <code>
    /// var classString = new ClassBuilder()
    ///     .Add("vibe-button")
    ///     .AddIf("vibe-button-disabled", true)
    ///     .Build();  // Returns "vibe-button vibe-button-disabled"
    /// </code>
    /// </example>
    public string Build()
    {
        if (_classes == null || _classes.Count == 0)
        {
            return string.Empty;
        }

        // Filter out any null or whitespace entries that might have been added
        var validClasses = _classes.Where(c => !string.IsNullOrWhiteSpace(c));
        return string.Join(" ", validClasses);
    }

    /// <summary>
    /// Returns the CSS class string. This is equivalent to calling <see cref="Build"/>.
    /// </summary>
    /// <returns>A space-separated string of CSS classes.</returns>
    public override string ToString()
    {
        return Build();
    }

    /// <summary>
    /// Implicitly converts a <see cref="ClassBuilder"/> to a string by calling <see cref="Build"/>.
    /// This allows the builder to be used directly where a string is expected.
    /// </summary>
    /// <param name="builder">The <see cref="ClassBuilder"/> to convert.</param>
    /// <returns>A space-separated string of CSS classes.</returns>
    /// <example>
    /// <code>
    /// // Can be used directly without calling Build()
    /// string cssClass = new ClassBuilder()
    ///     .Add("vibe-button")
    ///     .AddVariant("vibe-button", Variant);
    /// </code>
    /// </example>
    public static implicit operator string(ClassBuilder builder)
    {
        return builder.Build();
    }

    /// <summary>
    /// Determines whether the specified <see cref="ClassBuilder"/> is equal to the current instance.
    /// Two builders are equal if they produce the same CSS class string.
    /// </summary>
    /// <param name="other">The <see cref="ClassBuilder"/> to compare with the current instance.</param>
    /// <returns>true if the specified builder is equal to the current instance; otherwise, false.</returns>
    public bool Equals(ClassBuilder other)
    {
        return Build() == other.Build();
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>true if the specified object is equal to the current instance; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return obj is ClassBuilder other && Equals(other);
    }

    /// <summary>
    /// Returns the hash code for this instance based on the built CSS class string.
    /// </summary>
    /// <returns>A hash code for the current instance.</returns>
    public override int GetHashCode()
    {
        return Build().GetHashCode(StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether two <see cref="ClassBuilder"/> instances are equal.
    /// </summary>
    /// <param name="left">The first builder to compare.</param>
    /// <param name="right">The second builder to compare.</param>
    /// <returns>true if the builders are equal; otherwise, false.</returns>
    public static bool operator ==(ClassBuilder left, ClassBuilder right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two <see cref="ClassBuilder"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first builder to compare.</param>
    /// <param name="right">The second builder to compare.</param>
    /// <returns>true if the builders are not equal; otherwise, false.</returns>
    public static bool operator !=(ClassBuilder left, ClassBuilder right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Gets a copy of the current classes list or creates a new one if null.
    /// This ensures immutability by creating a new list for each modification.
    /// </summary>
    private List<string> GetClassesList()
    {
        return _classes == null ? new List<string>() : new List<string>(_classes);
    }
}
