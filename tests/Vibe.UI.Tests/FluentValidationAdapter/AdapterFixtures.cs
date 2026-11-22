using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Vibe.UI.Tests.FluentValidationAdapter;

public abstract record OrderBase
{
    public Guid Id { get; init; }
}

public sealed record OrderAddress
{
    public string City { get; init; } = "";
}

/// <summary>Shaped like a CRISP command: an immutable record whose rules live in a validator.</summary>
public sealed record Order : OrderBase
{
    public string Name { get; init; } = "";
    public string Code { get; init; } = "";
    public string Email { get; init; } = "";
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public decimal Discount { get; init; }
    public bool IsActive { get; init; }
    public bool? Confirmed { get; init; }
    public string Nickname { get; init; } = "";
    public string Phone { get; init; } = "";
    public string Notes { get; init; } = "";
    public string Title { get; init; } = "";
    public string Username { get; init; } = "";
    public string Unvalidated { get; init; } = "";
    public string InBlock { get; init; } = "";
    public string UnlessActive { get; init; } = "";
    public int Percent { get; init; }
    public string? Reference { get; init; }

    [StringLength(500)]
    [Display(Name = "From the attribute")]
    public string Summary { get; init; } = "";

    public OrderAddress Address { get; init; } = new();
}

public sealed class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).Length(3, 10);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Quantity).InclusiveBetween(1, 10);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0m).LessThanOrEqualTo(999m);
        RuleFor(x => x.Discount).GreaterThan(0m);
        RuleFor(x => x.IsActive).NotNull();
        RuleFor(x => x.Confirmed).NotNull();
        RuleFor(x => x.Nickname).NotEmpty().When(x => x.IsActive);
        RuleFor(x => x.Phone)
            .NotEmpty()
            .When(x => x.IsActive, ApplyConditionTo.CurrentValidator)
            .MaximumLength(20);
        RuleSet("Admin", () => RuleFor(x => x.Notes).NotEmpty());
        RuleFor(x => x.Title).NotEmpty().WithName("Job title");
        RuleFor(x => x.Username).NotEmpty().WithName("User name");
        RuleFor(x => x.Summary).MaximumLength(200);
        When(x => x.IsActive, () => RuleFor(x => x.InBlock).NotEmpty());
        RuleFor(x => x.UnlessActive).NotEmpty().Unless(x => x.IsActive);
        RuleFor(x => x.Percent).ExclusiveBetween(0, 100);
        RuleFor(x => x.Reference).NotNull();
    }
}

/// <summary>Used by one test only, so its construction count is not shared with tests running in parallel.</summary>
public sealed class CountingOrderValidator : AbstractValidator<Order>
{
    public static int Constructed;

    public CountingOrderValidator()
    {
        Interlocked.Increment(ref Constructed);
        RuleFor(x => x.Name).NotEmpty();
    }
}

/// <summary>A validator registered for a base type, reached through a derived model.</summary>
public sealed class OrderBaseValidator : AbstractValidator<OrderBase>
{
    public OrderBaseValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class NestedOrderValidator : AbstractValidator<Order>
{
    public NestedOrderValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.Address.City).NotEmpty().WithMessage("City is required.");
    }
}

/// <summary>A rule on the model as a whole, which FluentValidation reports with an empty property name.</summary>
public sealed class WholeOrderValidator : AbstractValidator<Order>
{
    public int Runs;

    public WholeOrderValidator() =>
        RuleFor(x => x)
            .Must(order =>
            {
                Runs++;
                return order.Quantity > 0;
            })
            .WithMessage("An order needs at least one item.");
}

public sealed class AsyncOrderValidator : AbstractValidator<Order>
{
    public AsyncOrderValidator() =>
        RuleFor(x => x.Name).MustAsync((_, _) => Task.FromResult(false));
}

/// <summary>Cannot be constructed by the container, because nothing registers its dependency.</summary>
public sealed class UnconstructibleValidator : AbstractValidator<Order>
{
    public UnconstructibleValidator(IDisposable neverRegistered) => _ = neverRegistered;
}
