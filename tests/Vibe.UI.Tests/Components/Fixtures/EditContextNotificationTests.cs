using AngleSharp.Dom;
using Microsoft.AspNetCore.Components.Forms;
using Order = Vibe.UI.Tests.Components.Fixtures.FormBindingFixture.Order;

namespace Vibe.UI.Tests.Components.Fixtures;

/// <summary>
/// Charter rule 4, one validation path: a typed input bound inside a form tells the
/// <see cref="EditContext"/> its field changed, after the model holds the new value. Each test
/// starts from a field the form has already found invalid and checks that the change made
/// through the input clears the message. That only happens if the form was told, and told after
/// the model was written.
/// </summary>
public class EditContextNotificationTests : TestBase
{
    private IRenderedComponent<FormBindingFixture> cut = default!;

    private EditContext Context => cut.Instance.Context;

    private Order Model => cut.Instance.Model;

    private void RenderInvalid(Order? model = null)
    {
        cut = Render<FormBindingFixture>(p => p.Add(f => f.Model, model ?? new Order()));
        Validate();
    }

    // Validating re-renders the form, so it has to run where rendering runs.
    private bool Validate() => cut.InvokeAsync(() => Context.Validate()).GetAwaiter().GetResult();

    private string[] Messages(string property) =>
        Context.GetValidationMessages(new FieldIdentifier(Model, property)).ToArray();

    private bool IsModified(string property) =>
        Context.IsModified(new FieldIdentifier(Model, property));

    private void ShouldNowBeValidAndModified(string property)
    {
        IsModified(property).ShouldBeTrue($"{property} should be marked modified");
        Messages(property)
            .ShouldBeEmpty($"{property} should have been validated with its new value");
    }

    private IElement Item(string css, string label) =>
        cut.FindAll(css).Single(i => i.TextContent.Trim() == label);

    [Fact]
    public void Input_notifies_when_the_text_is_committed()
    {
        RenderInvalid();
        Messages(nameof(Order.Name)).ShouldNotBeEmpty();

        cut.Find("#name").Change("Ada");

        Model.Name.ShouldBe("Ada");
        ShouldNowBeValidAndModified(nameof(Order.Name));
    }

    [Fact]
    public void Input_does_not_notify_on_each_key()
    {
        RenderInvalid();

        cut.Find("#name").Input("A");

        // The binding follows every key; the form hears once the text is committed.
        Model.Name.ShouldBe("A");
        IsModified(nameof(Order.Name)).ShouldBeFalse();
    }

    [Fact]
    public void Select_notifies()
    {
        RenderInvalid();
        Messages(nameof(Order.Country)).ShouldNotBeEmpty();

        cut.Find("#country").Change("no");

        Model.Country.ShouldBe("no");
        ShouldNowBeValidAndModified(nameof(Order.Country));
    }

    [Fact]
    public void Combobox_notifies_when_an_option_is_picked_and_when_it_is_cleared()
    {
        RenderInvalid();
        Messages(nameof(Order.Framework)).ShouldNotBeEmpty();

        cut.Find("#framework").Focus();
        Item(".combobox-option", "Blazor").Click();

        Model.Framework.ShouldBe("blazor");
        ShouldNowBeValidAndModified(nameof(Order.Framework));

        cut.Find("#framework").Input(string.Empty);

        Model.Framework.ShouldBeNull();
        Messages(nameof(Order.Framework))
            .ShouldNotBeEmpty("clearing should have been validated too");
    }

    [Fact]
    public void RadioGroup_notifies()
    {
        RenderInvalid();
        Messages(nameof(Order.Plan)).ShouldNotBeEmpty();

        Item(".vibe-radio-group-item", "Pro").QuerySelector("input")!.Change(true);

        Model.Plan.ShouldBe("pro");
        ShouldNowBeValidAndModified(nameof(Order.Plan));
    }

    [Fact]
    public void ToggleGroup_notifies_for_one_value_and_when_it_is_cleared()
    {
        RenderInvalid();
        Messages(nameof(Order.Alignment)).ShouldNotBeEmpty();

        Item(".vibe-toggle-group-item", "Right").Click();

        Model.Alignment.ShouldBe("right");
        ShouldNowBeValidAndModified(nameof(Order.Alignment));

        Item(".vibe-toggle-group-item", "Right").Click();

        Model.Alignment.ShouldBeNull();
        Messages(nameof(Order.Alignment))
            .ShouldNotBeEmpty("clearing should have been validated too");
    }

    [Fact]
    public void ToggleGroup_notifies_for_a_list_under_the_lists_own_field()
    {
        RenderInvalid();
        Messages(nameof(Order.Tags)).ShouldNotBeEmpty();

        Item(".vibe-toggle-group-item", "Urgent").Click();

        Model.Tags.ShouldBe(["urgent"]);
        ShouldNowBeValidAndModified(nameof(Order.Tags));
        IsModified(nameof(Order.Alignment)).ShouldBeFalse();
    }

    private static Order ValidOrder() =>
        new()
        {
            Name = "Ada",
            Quantity = 2,
            Seats = 3,
            Country = "no",
            Framework = "blazor",
            Plan = "pro",
            Alignment = "left",
            Tags = ["urgent"],
        };

    private IElement Quantity => cut.Find(".vibe-form-field input");

    [Fact]
    public void Text_that_is_not_a_number_stops_the_form_validating_and_leaves_the_model_alone()
    {
        RenderInvalid(ValidOrder());
        Validate().ShouldBeTrue();

        Quantity.Change("abc");

        Model.Quantity.ShouldBe(2);
        Context
            .Validate()
            .ShouldBeFalse("the form would otherwise submit 2 while the box says abc");
        Messages(nameof(Order.Quantity)).ShouldBe(["Enter a valid number."]);

        Quantity.Change("5");

        Model.Quantity.ShouldBe(5);
        Validate().ShouldBeTrue();
        Messages(nameof(Order.Quantity)).ShouldBeEmpty();
    }

    [Fact]
    public void The_message_is_said_once_inside_a_form_field_and_once_outside_one()
    {
        RenderInvalid(ValidOrder());

        Quantity.Change("abc");
        cut.Find("#seats").Change("xyz");

        // Quantity sits in a FormField that shows the field's messages; Seats stands alone and
        // says it itself. Two boxes, two messages: neither is silent and neither is doubled.
        cut.FindAll("*")
            .Count(e => e.ChildElementCount == 0 && e.TextContent.Trim() == "Enter a valid number.")
            .ShouldBe(2);
        cut.Find(".vibe-form-field").TextContent.Split("Enter a valid number.").Length.ShouldBe(2);
    }

    [Fact]
    public void Removing_the_input_takes_its_message_with_it()
    {
        RenderInvalid(ValidOrder());
        Quantity.Change("abc");
        Validate().ShouldBeFalse();

        cut.Render(p => p.Add(f => f.Model, Model).Add(f => f.ShowQuantity, false));

        Validate().ShouldBeTrue();
    }

    [Fact]
    public void A_value_set_from_outside_replaces_the_bad_text_and_its_message()
    {
        RenderInvalid(ValidOrder());
        Quantity.Change("abc");
        Validate().ShouldBeFalse();

        Model.Quantity = 7;
        cut.Render(p => p.Add(f => f.Model, Model));

        Quantity.GetAttribute("value").ShouldBe("7");
        Validate().ShouldBeTrue();
    }

    [Fact]
    public void Outside_a_form_the_inputs_work_as_before()
    {
        var bare = Render<NullableBindingFixture>();

        bare.Find("#framework").Focus();
        bare.FindAll(".combobox-option").Single().Click();

        bare.Instance.Framework.ShouldBe("blazor");
    }

    [Fact]
    public void Outside_a_form_a_binding_that_names_no_model_field_still_works()
    {
        // A static member is not something a FieldIdentifier can describe. With no form there is
        // nothing to tell, so the binding must not be inspected at all.
        StaticBindingFixture.Shared = null;
        var bare = Render<StaticBindingFixture>();

        bare.Find("#shared").Change("x");

        StaticBindingFixture.Shared.ShouldBe("x");
    }

    [Fact]
    public void When_an_EditForm_gets_another_model_the_bad_text_and_its_message_go()
    {
        RenderInvalid(ValidOrder());
        Quantity.Change("abc");
        var firstContext = Context;
        var firstModel = Model;

        // EditForm rebuilds its children for a new context, so this is a fresh input.
        cut.Render(p => p.Add(f => f.Model, ValidOrder()));

        Context.ShouldNotBeSameAs(firstContext);
        Quantity.GetAttribute("value").ShouldBe("2");
        Validate().ShouldBeTrue();
        firstContext
            .GetValidationMessages(new FieldIdentifier(firstModel, nameof(Order.Quantity)))
            .ShouldBeEmpty("the form that was replaced should not keep the message");
    }

    [Fact]
    public void Under_a_context_cascaded_by_hand_the_message_follows_the_input_to_a_new_context()
    {
        var firstModel = ValidOrder();
        var firstContext = new EditContext(firstModel);
        var hand = Render<CascadedContextFixture>(p =>
            p.Add(f => f.Model, firstModel).Add(f => f.Context, firstContext)
        );
        hand.Find("#quantity").Change("abc");
        firstContext
            .GetValidationMessages(new FieldIdentifier(firstModel, nameof(Order.Quantity)))
            .ShouldBe(["Enter a valid number."]);

        // Same quantity, so the same input keeps the text the user typed: still not a number.
        var secondModel = ValidOrder();
        var secondContext = new EditContext(secondModel);
        hand.Render(p => p.Add(f => f.Model, secondModel).Add(f => f.Context, secondContext));

        hand.Find("#quantity").GetAttribute("value").ShouldBe("abc");
        secondContext
            .GetValidationMessages(new FieldIdentifier(secondModel, nameof(Order.Quantity)))
            .ShouldBe(["Enter a valid number."], "the new form must not submit while the box says abc");
        firstContext
            .GetValidationMessages(new FieldIdentifier(firstModel, nameof(Order.Quantity)))
            .ShouldBeEmpty("the context left behind should not keep the message");

        // And it still clears from the new one.
        hand.Find("#quantity").Change("5");
        secondModel.Quantity.ShouldBe(5);
        secondContext.GetValidationMessages().ShouldBeEmpty();
    }

    [Fact]
    public void An_input_bound_by_hand_inside_a_form_works_and_names_no_field()
    {
        RenderInvalid(ValidOrder());

        cut.Find("#manual").Change("4");
        cut.Instance.Manual.ShouldBe(4);

        cut.Find("#manual").Change("abc");

        // It has no field to hang a message on, so it says it itself and the form is not held.
        cut.Instance.Manual.ShouldBe(4);
        cut.FindAll("*")
            .Count(e => e.ChildElementCount == 0 && e.TextContent.Trim() == "Enter a valid number.")
            .ShouldBe(1);
        Validate().ShouldBeTrue();
    }
}
