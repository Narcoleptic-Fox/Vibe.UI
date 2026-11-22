using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Vibe.UI.FluentValidation;

/// <summary>
/// Runs a FluentValidation validator for the enclosing <see cref="EditForm"/> and writes its errors
/// into the form's <see cref="EditContext"/>, the same place <c>DataAnnotationsValidator</c> writes.
/// Place it inside the form: <c>&lt;FluentValidator /&gt;</c>.
/// </summary>
/// <remarks>
/// Validation is synchronous. A validator with async rules throws
/// <see cref="AsyncValidatorInvokedSynchronouslyException"/>.
/// </remarks>
public sealed class FluentValidator : ComponentBase, IDisposable
{
    private ValidationMessageStore? _messages;
    private EditContext? _subscribedTo;
    private IValidator _validator = default!;

    [CascadingParameter]
    private EditContext? CurrentEditContext { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    /// <summary>
    /// The validator to run. When omitted, the <c>IValidator&lt;T&gt;</c> registered for the form's
    /// model type, or for its nearest base type, is used.
    /// </summary>
    [Parameter]
    public IValidator? Validator { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (CurrentEditContext is null)
            throw new InvalidOperationException(
                $"{nameof(FluentValidator)} requires a cascading parameter of type {nameof(EditContext)}. "
                    + $"Use it inside an {nameof(EditForm)}."
            );

        // A form with no validator would look valid and submit anything, so refuse to start.
        _validator =
            Validator
            ?? ValidatorDescriptorSource.Resolve(Services, CurrentEditContext.Model.GetType())
            ?? throw new InvalidOperationException(
                $"No FluentValidation validator is registered for {CurrentEditContext.Model.GetType().Name}. "
                    + $"Register an IValidator<{CurrentEditContext.Model.GetType().Name}> or pass the {nameof(Validator)} parameter."
            );

        if (!ReferenceEquals(CurrentEditContext, _subscribedTo))
        {
            Unsubscribe();
            _subscribedTo = CurrentEditContext;
            _messages = new ValidationMessageStore(CurrentEditContext);
            CurrentEditContext.OnValidationRequested += ValidateModel;
            CurrentEditContext.OnFieldChanged += ValidateField;
        }
    }

    private void ValidateModel(object? sender, ValidationRequestedEventArgs e)
    {
        EditContext edit = _subscribedTo!;
        _messages!.Clear();
        Write(edit, _validator.Validate(new ValidationContext<object>(edit.Model)));
    }

    private void ValidateField(object? sender, FieldChangedEventArgs e)
    {
        EditContext edit = _subscribedTo!;
        // Rules are addressed by property name on the form's model. A field on some other object
        // (a nested model) has no rule path here, so it is checked when the whole form is.
        if (!ReferenceEquals(e.FieldIdentifier.Model, edit.Model))
            return;

        _messages!.Clear(e.FieldIdentifier);
        var context = ValidationContext<object>.CreateWithOptions(
            edit.Model,
            options => options.IncludeProperties(e.FieldIdentifier.FieldName)
        );
        Write(edit, _validator.Validate(context));
    }

    private void Write(EditContext edit, ValidationResult result)
    {
        foreach (ValidationFailure failure in result.Errors)
        {
            // A nested or indexed path ("Address.City", "Items[0].Name") names a field that no input
            // on this model owns. Attach it to the model itself so the form is still invalid and a
            // ValidationSummary shows the message, instead of blocking submit with nothing on screen.
            bool onModel =
                !string.IsNullOrEmpty(failure.PropertyName)
                && failure.PropertyName.IndexOfAny(['.', '[']) < 0;
            FieldIdentifier field = onModel
                ? edit.Field(failure.PropertyName)
                : new FieldIdentifier(edit.Model, string.Empty);
            _messages!.Add(field, failure.ErrorMessage);
        }
        edit.NotifyValidationStateChanged();
    }

    private void Unsubscribe()
    {
        if (_subscribedTo is null)
            return;
        _subscribedTo.OnValidationRequested -= ValidateModel;
        _subscribedTo.OnFieldChanged -= ValidateField;
        _messages!.Clear();
        _subscribedTo = null;
    }

    /// <inheritdoc />
    public void Dispose() => Unsubscribe();
}
