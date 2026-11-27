using Microsoft.AspNetCore.Components.Forms;

namespace Vibe.UI.Components;

/// <summary>
/// What a <see cref="FormField{TValue}"/> tells the control inside it: the id its label points at,
/// the ids of its description and error message, and whether the field is invalid or required.
/// </summary>
/// <remarks>
/// The built-in inputs read it, and so can a control of your own: take it as a
/// <c>[CascadingParameter] FormFieldContext? Field</c>, put <see cref="ControlId"/> on the element
/// that takes focus, and the label, description and message are wired to it.
/// </remarks>
public sealed class FormFieldContext
{
    internal FormFieldContext(
        string controlId,
        string? describedBy,
        string? errorId,
        bool isInvalid,
        bool isRequired,
        FieldIdentifier? validatedField
    )
    {
        ControlId = controlId;
        DescribedBy = describedBy;
        ErrorId = errorId;
        IsInvalid = isInvalid;
        IsRequired = isRequired;
        ValidatedField = validatedField;
    }

    /// <summary>The id the field's label points at. Put it on the element that takes focus.</summary>
    public string ControlId { get; }

    /// <summary>The ids for <c>aria-describedby</c>: the description and, while invalid, the message. Null when there is neither.</summary>
    public string? DescribedBy { get; }

    /// <summary>The id of the element showing the message, for <c>aria-errormessage</c>. Null while there is none.</summary>
    public string? ErrorId { get; }

    /// <summary>Whether the field currently has a validation message or was marked as in error.</summary>
    public bool IsInvalid { get; }

    /// <summary>Whether the field shows the required marker.</summary>
    public bool IsRequired { get; }

    /// <summary>The form field whose messages this wrapper shows, when it is inside a form and was given <c>For</c>.</summary>
    public FieldIdentifier? ValidatedField { get; }
}
