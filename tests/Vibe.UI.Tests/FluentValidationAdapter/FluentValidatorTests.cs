using FluentValidation;
using Microsoft.AspNetCore.Components.Forms;
using Vibe.UI.FluentValidation;
using Vibe.UI.Metadata;

namespace Vibe.UI.Tests.FluentValidationAdapter;

public class FluentValidatorTests : TestBase
{
    // bUnit replaces the whole parameter set on a re-render, so the same set is used both times.
    private static Action<ComponentParameterCollectionBuilder<CascadingValue<EditContext>>> Form(
        EditContext edit,
        IValidator? validator
    ) =>
        parameters =>
            parameters
                .Add(cascade => cascade.Value, edit)
                .AddChildContent<FluentValidator>(child =>
                {
                    if (validator is not null)
                        child.Add(v => v.Validator, validator);
                });

    private IRenderedComponent<CascadingValue<EditContext>> RenderIn(
        EditContext edit,
        IValidator? validator = null
    ) => Render(Form(edit, validator));

    private static Order ValidOrder() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Ada",
            Code = "ABC",
            Email = "ada@example.test",
            Quantity = 2,
            Price = 5m,
            Discount = 1m,
            Confirmed = true,
            Title = "Engineer",
            Username = "ada",
            Address = new OrderAddress { City = "London" },
        };

    [Fact]
    public void Validating_the_form_writes_each_error_to_its_field_and_reports_invalid()
    {
        Services.AddScoped<IValidator<Order>, NestedOrderValidator>();
        var order = new Order { Address = new OrderAddress { City = "London" } };
        var edit = new EditContext(order);
        RenderIn(edit);

        bool valid = edit.Validate();

        valid.ShouldBeFalse();
        edit.GetValidationMessages(edit.Field(nameof(Order.Name))).ShouldBe(["Name is required."]);
        edit.GetValidationMessages(edit.Field(nameof(Order.Email))).ShouldBeEmpty();
    }

    [Fact]
    public void Fixing_a_field_then_validating_again_removes_its_error()
    {
        Order order = ValidOrder() with { Name = "" };
        var edit = new EditContext(order);
        RenderIn(edit, new NestedOrderValidator());
        edit.Validate().ShouldBeFalse();
        edit.GetValidationMessages(edit.Field(nameof(Order.Name))).ShouldNotBeEmpty();

        // Same model instance and same EditContext: the record is written in place, as a form does.
        Services.GetRequiredService<IPropertyMetadataResolver>().Bind(() => order.Name).Value = "Ada";

        edit.Validate().ShouldBeTrue();
        edit.GetValidationMessages().ShouldBeEmpty();
    }

    [Fact]
    public void Fixing_a_field_then_changing_it_again_removes_its_error()
    {
        Order order = ValidOrder() with { Name = "" };
        var edit = new EditContext(order);
        RenderIn(edit, new NestedOrderValidator());
        FieldIdentifier name = edit.Field(nameof(Order.Name));
        edit.NotifyFieldChanged(name);
        edit.GetValidationMessages(name).ShouldNotBeEmpty();

        Services.GetRequiredService<IPropertyMetadataResolver>().Bind(() => order.Name).Value = "Ada";
        edit.NotifyFieldChanged(name);

        edit.GetValidationMessages(name).ShouldBeEmpty();
    }

    [Fact]
    public void Changing_a_field_validates_that_field_only()
    {
        var edit = new EditContext(new Order());
        RenderIn(edit, new OrderValidator());

        edit.NotifyFieldChanged(edit.Field(nameof(Order.Name)));

        edit.GetValidationMessages(edit.Field(nameof(Order.Name))).ShouldNotBeEmpty();
        // Email is also invalid on this model, but it was not the field that changed.
        edit.GetValidationMessages(edit.Field(nameof(Order.Email))).ShouldBeEmpty();
    }

    [Fact]
    public void A_failure_on_a_nested_path_is_reported_at_form_level_not_dropped()
    {
        var order = ValidOrder() with { Address = new OrderAddress { City = "" } };
        var edit = new EditContext(order);
        RenderIn(edit, new NestedOrderValidator());

        bool valid = edit.Validate();

        valid.ShouldBeFalse();
        edit.GetValidationMessages(new FieldIdentifier(order, string.Empty))
            .ShouldBe(["City is required."]);
        // GetValidationMessages() is what ValidationSummary renders.
        edit.GetValidationMessages().ShouldContain("City is required.");
    }

    [Fact]
    public void A_rule_on_the_whole_model_is_reported_at_form_level()
    {
        Order order = ValidOrder() with { Quantity = 0 };
        var edit = new EditContext(order);
        RenderIn(edit, new WholeOrderValidator());

        edit.Validate().ShouldBeFalse();

        edit.GetValidationMessages(new FieldIdentifier(order, string.Empty))
            .ShouldBe(["An order needs at least one item."]);
    }

    [Fact]
    public void Re_rendering_with_the_same_form_does_not_subscribe_twice()
    {
        var validator = new WholeOrderValidator();
        var edit = new EditContext(ValidOrder() with { Quantity = 0 });
        IRenderedComponent<CascadingValue<EditContext>> rendered = RenderIn(edit, validator);

        rendered.Render(Form(edit, validator));
        rendered.Render(Form(edit, validator));
        edit.Validate();

        // Two subscriptions would run the validator twice for one validation request.
        validator.Runs.ShouldBe(1);
        edit.GetValidationMessages().Count().ShouldBe(1);
    }

    [Fact]
    public void Replacing_the_form_moves_validation_to_the_new_one_and_clears_the_old()
    {
        var validator = new WholeOrderValidator();
        var first = new EditContext(ValidOrder() with { Quantity = 0 });
        var second = new EditContext(ValidOrder() with { Quantity = 0 });
        IRenderedComponent<CascadingValue<EditContext>> rendered = RenderIn(first, validator);
        first.Validate().ShouldBeFalse();

        rendered.Render(Form(second, validator));

        first.GetValidationMessages().ShouldBeEmpty();
        first.Validate().ShouldBeTrue();
        second.Validate().ShouldBeFalse();
        second.GetValidationMessages().ShouldBe(["An order needs at least one item."]);
    }

    [Fact]
    public void A_change_to_a_field_on_another_object_is_left_for_form_validation()
    {
        var order = ValidOrder();
        var edit = new EditContext(order);
        RenderIn(edit, new NestedOrderValidator());

        Should.NotThrow(() =>
            edit.NotifyFieldChanged(new FieldIdentifier(order.Address, nameof(OrderAddress.City)))
        );

        edit.GetValidationMessages().ShouldBeEmpty();
    }

    [Fact]
    public void The_validator_is_found_through_a_base_type_registration()
    {
        Services.AddScoped<IValidator<OrderBase>, OrderBaseValidator>();
        var edit = new EditContext(new Order());
        RenderIn(edit);

        edit.Validate().ShouldBeFalse();
        edit.GetValidationMessages(edit.Field(nameof(OrderBase.Id))).ShouldNotBeEmpty();
    }

    [Fact]
    public void A_form_with_no_validator_refuses_to_render()
    {
        var edit = new EditContext(new Order());

        InvalidOperationException error = Should.Throw<InvalidOperationException>(() =>
            RenderIn(edit)
        );

        error.Message.ShouldContain("IValidator<Order>");
    }

    [Fact]
    public void Outside_an_EditForm_it_refuses_to_render()
    {
        Should
            .Throw<InvalidOperationException>(() => Render<FluentValidator>())
            .Message.ShouldContain(nameof(EditContext));
    }

    [Fact]
    public void A_validator_with_async_rules_throws_instead_of_passing_silently()
    {
        var edit = new EditContext(ValidOrder());
        RenderIn(edit, new AsyncOrderValidator());

        Should.Throw<AsyncValidatorInvokedSynchronouslyException>(() => edit.Validate());
    }

    [Fact]
    public void Disposing_the_component_stops_validating_and_clears_its_messages()
    {
        var edit = new EditContext(new Order());
        IRenderedComponent<CascadingValue<EditContext>> rendered = RenderIn(
            edit,
            new NestedOrderValidator()
        );
        edit.Validate().ShouldBeFalse();

        rendered.FindComponent<FluentValidator>().Instance.Dispose();

        edit.GetValidationMessages().ShouldBeEmpty();
        edit.Validate().ShouldBeTrue();
    }
}
