using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;

namespace Vibe.UI.Base;

/// <summary>
/// An input's link to the form it sits in: which field of the <see cref="EditContext"/> it edits,
/// telling the form when that field changed, and holding the one message an input can raise by
/// itself (text that does not convert to the bound type).
/// </summary>
/// <remarks>
/// Every typed input uses this, so there is one implementation of the rule. With no form around
/// the input, or no expression to name the field, it does nothing.
/// </remarks>
internal sealed class EditContextField : IDisposable
{
    private EditContext? _context;
    private FieldIdentifier? _field;
    private ValidationMessageStore? _store;
    private string? _parsingError;

    /// <summary>The field being edited, or null when the input is not bound inside a form.</summary>
    public FieldIdentifier? Field => _context is null ? null : _field;

    /// <summary>
    /// Call from <c>OnParametersSet</c> with the cascaded context and the expression that
    /// <c>@bind-Value</c> supplies.
    /// </summary>
    public void Update<TField>(EditContext? context, Expression<Func<TField>>? expression)
    {
        // Outside a form there is no field to name, and the binding may be to something a
        // FieldIdentifier cannot describe, such as a static member. Resolving costs under a
        // microsecond (the framework caches it), so inside a form it runs on every parameter set
        // and follows the model if the parent swaps it.
        FieldIdentifier? field =
            context is null || expression is null ? null : FieldIdentifier.Create(expression);

        if (ReferenceEquals(context, _context) && Nullable.Equals(field, _field))
        {
            return;
        }

        // The message belongs to the field it was raised for. Take it off the old one, and put it
        // on the new one if the input still holds the text that caused it.
        var parsingError = _parsingError;
        ClearStore();

        if (!ReferenceEquals(context, _context))
        {
            _store = null;
        }

        _context = context;
        _field = field;

        if (parsingError is not null)
        {
            SetParsingError(parsingError);
        }
    }

    /// <summary>Tells the form the field's value changed. Call after the new value is reported.</summary>
    public void NotifyChanged()
    {
        if (_context is not null && _field is { } field)
        {
            _context.NotifyFieldChanged(field);
        }
    }

    /// <summary>
    /// Records that the input holds text which is not a value, or with null that it no longer
    /// does. While a message is recorded the form does not validate, so it cannot submit the last
    /// good value in place of what the user sees.
    /// </summary>
    public void SetParsingError(string? message)
    {
        if (message == _parsingError && (message is null || _store is not null))
        {
            return;
        }

        ClearStore();
        _parsingError = message;

        if (message is null || _context is null || _field is not { } field)
        {
            return;
        }

        _store ??= new ValidationMessageStore(_context);
        _store.Add(field, message);
        _context.NotifyValidationStateChanged();
    }

    public void Dispose() => ClearStore();

    private void ClearStore()
    {
        if (_parsingError is null || _store is null || _context is null)
        {
            _parsingError = null;
            return;
        }

        _parsingError = null;
        _store.Clear();
        _context.NotifyValidationStateChanged();
    }
}
