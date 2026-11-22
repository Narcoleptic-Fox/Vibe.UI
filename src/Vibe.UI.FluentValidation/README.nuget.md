# Vibe.UI.FluentValidation

Connects [FluentValidation](https://docs.fluentvalidation.net) validators to Vibe.UI.

Requires the .NET 10 SDK. This is a beta release; APIs may change before stable 1.0.

It does two things:

1. **Metadata.** Vibe.UI reads required, length limits, ranges, the email data type and explicit
   display names (`WithName`) from your validators, so you do not repeat them on each input.
2. **Validation.** `<FluentValidator />` inside an `EditForm` runs the validator and writes its
   errors into the form's `EditContext`, the same place `DataAnnotationsValidator` writes.

## Setup

```csharp
builder.Services.AddVibeUI();
builder.Services.AddVibeFluentValidation();

// Register your validators as IValidator<T>, in the client as well as on the server.
builder.Services.AddScoped<IValidator<CreateUser>, CreateUserValidator>();
```

```razor
<EditForm Model="command" OnValidSubmit="Save">
    <FluentValidator />
    ...
</EditForm>
```

## What is read from a validator

| Rule | Metadata |
|---|---|
| `NotEmpty()`, `NotNull()` | Required. `NotNull()` on a non-nullable value type is ignored |
| `Length`, `MinimumLength`, `MaximumLength` | Minimum and maximum length |
| `InclusiveBetween`, `GreaterThanOrEqualTo`, `LessThanOrEqualTo` | Minimum and maximum |
| `EmailAddress()` | Email data type |
| `WithName("...")` | Label |

A rule under `When`, `Unless` or a rule set is not read, because it does not always apply.

## Limits

- Only rules on properties of the form's own model are shown beside an input. A failure on a nested
  or indexed path (`Address.City`, `Items[0].Name`) is reported at form level, so a
  `ValidationSummary` shows it.
- Validation is synchronous. A validator with async rules throws
  `AsyncValidatorInvokedSynchronouslyException`.
- This package works with the Vibe.UI package or project reference. Projects that copied Vibe.UI's
  source with `vibe init` cannot use it yet.
