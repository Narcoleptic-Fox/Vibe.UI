using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Vibe.UI.Metadata;

namespace Vibe.UI.Tests.Metadata;

/// <summary>Shaped like a CRISP base model: an inherited, init-only identifier.</summary>
public abstract record FixtureBase
{
    public Guid Id { get; init; }
}

/// <summary>Shaped like a CRISP command: an immutable record with init-only properties and no attributes.</summary>
public sealed record FixtureCommand : FixtureBase
{
    public string FirstName { get; init; } = "";
    public int Quantity { get; init; }
    public int? OptionalCount { get; init; }
    public FixtureRole Role { get; init; }
    public string Computed => FirstName.ToUpperInvariant();
}

public enum FixtureRole
{
    Viewer,
    Editor,
}

/// <summary>An app that already describes its model with DataAnnotations.</summary>
public sealed class AnnotatedModel
{
    [Display(
        Name = "Email address",
        Description = "Where we send receipts",
        Prompt = "you@example.com"
    )]
    [Required]
    [StringLength(120, MinimumLength = 5)]
    [EmailAddress]
    public string Email { get; set; } = "";

    [DisplayName("Unit price")]
    [Range(1, 500)]
    [DisplayFormat(DataFormatString = "C2")]
    public decimal Price { get; set; }

    [MaxLength(40)]
    [MinLength(2)]
    public string Code { get; set; } = "";

    public string Plain { get; set; } = "";

    // EmailAddressAttribute derives from DataTypeAttribute, so this property carries two of them.
    [EmailAddress]
    [DataType(DataType.Password)]
    public string TwoDataTypes { get; set; } = "";

    [EmailAddress]
    [Phone]
    public string TwoDerivedDataTypes { get; set; } = "";
}

public struct FixtureStruct
{
    public int Count { get; set; }
}

/// <summary>
/// Stands in for the optional FluentValidation package: it answers what the spike read from a
/// validator (display name, required, maximum length) and counts how often it is asked.
/// </summary>
public sealed class FakeValidatorProvider : IPropertyMetadataProvider
{
    public int Order { get; init; } = PropertyMetadataOrder.Validator;

    public int Calls { get; private set; }

    public PropertyMetadata? Answer { get; init; } =
        new()
        {
            Label = "From validator",
            Required = true,
            MaxLength = 100,
        };

    public PropertyMetadata? GetMetadata(Type modelType, PropertyInfo property)
    {
        Calls++;
        return Answer;
    }
}
