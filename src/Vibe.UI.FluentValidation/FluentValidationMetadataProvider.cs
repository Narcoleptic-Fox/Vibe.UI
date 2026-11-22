using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using Vibe.UI.Metadata;

namespace Vibe.UI.FluentValidation;

/// <summary>
/// The validator layer of the metadata chain: reads required, limits, the email data type and an
/// explicit display name from the FluentValidation validator registered for a model.
/// </summary>
/// <remarks>
/// Only rules that always apply are read. A rule under <c>When</c>, <c>Unless</c> or a rule set
/// depends on the instance or the caller, and metadata is cached per type.
/// </remarks>
public sealed class FluentValidationMetadataProvider : IPropertyMetadataProvider
{
    private readonly ValidatorDescriptorSource _descriptors;

    internal FluentValidationMetadataProvider(ValidatorDescriptorSource descriptors) =>
        _descriptors = descriptors;

    /// <inheritdoc />
    public int Order => PropertyMetadataOrder.Validator;

    /// <inheritdoc />
    public PropertyMetadata? GetMetadata(Type modelType, PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(modelType);
        ArgumentNullException.ThrowIfNull(property);

        if (_descriptors.Find(modelType) is not { } descriptor)
            return null;

        IValidationRule[] rules =
        [
            .. descriptor.GetRulesForMember(property.Name).Where(rule => AlwaysApplies(rule)),
        ];
        if (rules.Length == 0)
            return null;

        IPropertyValidator[] validators =
        [
            .. rules
                .SelectMany(rule => rule.Components)
                .Where(component => !component.HasCondition && !component.HasAsyncCondition)
                .Select(component => component.Validator),
        ];

        ILengthValidator? length = validators.OfType<ILengthValidator>().FirstOrDefault();
        // ExclusiveBetween implements the same interface but allows neither end, so only the
        // inclusive form states limits an input can show.
        IBetweenValidator? between = validators
            .OfType<IBetweenValidator>()
            .FirstOrDefault(validator => validator is IInclusiveBetweenValidator);

        var metadata = new PropertyMetadata
        {
            Label = ExplicitDisplayName(rules),
            Required = validators.Any(validator => MeansRequired(validator, property.PropertyType))
                ? true
                : null,
            // The length validators report 0 for "no minimum" and -1 for "no maximum".
            MinLength = length is { Min: > 0 } ? length.Min : null,
            MaxLength = length is { Max: > 0 } ? length.Max : null,
            Minimum = between?.From ?? Bound(validators, Comparison.GreaterThanOrEqual),
            Maximum = between?.To ?? Bound(validators, Comparison.LessThanOrEqual),
            DataType = validators.Any(validator => validator is IEmailValidator)
                ? DataType.EmailAddress
                : null,
        };

        return metadata == PropertyMetadata.Empty ? null : metadata;
    }

    private static bool AlwaysApplies(IValidationRule rule) =>
        !rule.HasCondition
        && !rule.HasAsyncCondition
        && (
            rule.RuleSets is null
            || rule.RuleSets.All(set =>
                string.Equals(
                    set,
                    RulesetValidatorSelector.DefaultRuleSetName,
                    StringComparison.OrdinalIgnoreCase
                )
            )
        );

    // NotNull on a non-nullable value type can never fail, so it says nothing about the input.
    // NotEmpty on one (a Guid, a number) does mean "must not be the default value".
    private static bool MeansRequired(IPropertyValidator validator, Type propertyType) =>
        validator is INotEmptyValidator
        || validator is INotNullValidator
            && (!propertyType.IsValueType || Nullable.GetUnderlyingType(propertyType) is not null);

    // Only an inclusive bound against a constant is a limit an input can show. An exclusive bound,
    // or one against another property, has no single minimum or maximum.
    private static object? Bound(
        IEnumerable<IPropertyValidator> validators,
        Comparison comparison
    ) =>
        validators
            .OfType<IComparisonValidator>()
            .FirstOrDefault(validator =>
                validator.Comparison == comparison && validator.MemberToCompare is null
            )
            ?.ValueToCompare;

    // FluentValidation invents a display name for every rule by splitting the property name
    // ("IsActive" becomes "Is Active"), and its splitter is internal, so the invented name cannot
    // be compared exactly. A name that is only the property name re-spaced or re-cased is treated
    // as invented and left to the lower layers. That loses WithName("User name") on a property
    // called Username, and keeps every label in one style for the rest.
    private static string? ExplicitDisplayName(IEnumerable<IValidationRule> rules)
    {
        foreach (IValidationRule rule in rules)
        {
            string? name = rule.GetDisplayName(null);
            if (
                !string.IsNullOrWhiteSpace(name)
                && !string.Equals(
                    string.Concat(name.Where(c => !char.IsWhiteSpace(c))),
                    rule.PropertyName,
                    StringComparison.OrdinalIgnoreCase
                )
            )
                return name;
        }
        return null;
    }
}
