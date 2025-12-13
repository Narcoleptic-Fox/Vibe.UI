namespace Vibe.UI.Docs.Models;

/// <summary>
/// Defines the type of a component property for the PropTweaker.
/// </summary>
public enum PropType
{
    /// <summary>Boolean toggle (true/false)</summary>
    Boolean,

    /// <summary>Enum selection from predefined values</summary>
    Enum,

    /// <summary>Text string input</summary>
    String,

    /// <summary>Numeric input</summary>
    Number,

    /// <summary>Color picker</summary>
    Color
}

/// <summary>
/// Defines a component property that can be tweaked in the live preview.
/// </summary>
public class PropDefinition
{
    /// <summary>
    /// The name of the property (e.g., "Variant", "Size", "Disabled").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The type of property control to render.
    /// </summary>
    public PropType Type { get; set; }

    /// <summary>
    /// The current value of the property.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// The default value of the property.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// For Enum types, the available options.
    /// </summary>
    public List<string> Options { get; set; } = new();

    /// <summary>
    /// Brief description of the property.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// For Number types, the minimum value.
    /// </summary>
    public double? Min { get; set; }

    /// <summary>
    /// For Number types, the maximum value.
    /// </summary>
    public double? Max { get; set; }

    /// <summary>
    /// For Number types, the step increment.
    /// </summary>
    public double? Step { get; set; }

    /// <summary>
    /// Creates a Boolean prop definition.
    /// </summary>
    public static PropDefinition Boolean(string name, bool defaultValue = false, string? description = null)
    {
        return new PropDefinition
        {
            Name = name,
            Type = PropType.Boolean,
            Value = defaultValue,
            DefaultValue = defaultValue,
            Description = description
        };
    }

    /// <summary>
    /// Creates an Enum prop definition.
    /// </summary>
    public static PropDefinition Enum<TEnum>(string name, TEnum defaultValue, string? description = null) where TEnum : struct, System.Enum
    {
        var options = System.Enum.GetNames<TEnum>().ToList();
        return new PropDefinition
        {
            Name = name,
            Type = PropType.Enum,
            Value = defaultValue.ToString(),
            DefaultValue = defaultValue.ToString(),
            Options = options,
            Description = description
        };
    }

    /// <summary>
    /// Creates a String prop definition.
    /// </summary>
    public static PropDefinition String(string name, string defaultValue = "", string? description = null)
    {
        return new PropDefinition
        {
            Name = name,
            Type = PropType.String,
            Value = defaultValue,
            DefaultValue = defaultValue,
            Description = description
        };
    }

    /// <summary>
    /// Creates a Number prop definition.
    /// </summary>
    public static PropDefinition Number(string name, double defaultValue = 0, double? min = null, double? max = null, double? step = null, string? description = null)
    {
        return new PropDefinition
        {
            Name = name,
            Type = PropType.Number,
            Value = defaultValue,
            DefaultValue = defaultValue,
            Min = min,
            Max = max,
            Step = step,
            Description = description
        };
    }
}
