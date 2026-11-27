using FluentValidation;

namespace Vibe.UI.Tests.Components.Fixtures;

/// <summary>Shaped like a CRISP <c>ModifyCommand</c>: an inherited, init-only identifier.</summary>
public abstract record StockCommand
{
    public Guid Id { get; init; }
}

/// <summary>Shaped like a CRISP command: every property init-only, no attributes.</summary>
public sealed record UpdateStock : StockCommand
{
    public string Name { get; init; } = "";

    public string? Description { get; init; }

    public string? Owner { get; init; }

    public int Quantity { get; init; }

    public bool IsActive { get; init; }

    // Shaped like a CRISP filter: yes, no, or not asked.
    public bool? Shipped { get; init; }
}

/// <summary>The rules live in a validator, as they do for a CRISP command.</summary>
public sealed class UpdateStockValidator : AbstractValidator<UpdateStock>
{
    public UpdateStockValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Item name is required.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be at least 1.");
    }
}
