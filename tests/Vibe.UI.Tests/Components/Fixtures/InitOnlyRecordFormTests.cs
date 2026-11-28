using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Forms;
using Vibe.UI.Components;

namespace Vibe.UI.Tests.Components.Fixtures;

/// <summary>
/// B2.4: a form over an <c>init</c>-only record edits that record in place, and keeps one
/// <see cref="EditContext"/> for as long as the page hands it the same instance. A record also
/// has value equality, which a class model does not: two instances with the same values are
/// equal, and writing one in place changes its hash code. Neither may confuse the form.
/// </summary>
public class InitOnlyRecordFormTests : TestBase
{
    private IRenderedComponent<InitOnlyRecordFixture> page = default!;

    private void RenderForm(UpdateStock command) =>
        page = Render<InitOnlyRecordFixture>(p => p.Add(f => f.Command, command));

    // bUnit replaces the whole parameter set, so the command is given again each time.
    private void RenderPageAgain(UpdateStock command, int ticks) =>
        page.Render(p => p.Add(f => f.Command, command).Add(f => f.Ticks, ticks));

    private EditContext Context => page.FindComponent<EditForm>().Instance.EditContext!;

    private static UpdateStock NewCommand() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Bolt",
            Quantity = 4,
        };

    [Fact]
    public void Every_property_the_form_edits_is_init_only()
    {
        // Guards every test below: with an ordinary setter they would pass for the wrong reason.
        string[] edited =
        [
            nameof(UpdateStock.Name),
            nameof(UpdateStock.Description),
            nameof(UpdateStock.Owner),
            nameof(UpdateStock.Quantity),
            nameof(UpdateStock.IsActive),
            nameof(UpdateStock.Shipped),
        ];

        foreach (string name in edited)
        {
            PropertyInfo property = typeof(UpdateStock).GetProperty(name)!;
            property
                .SetMethod!.ReturnParameter.GetRequiredCustomModifiers()
                .ShouldContain(typeof(IsExternalInit), name);
        }
    }

    [Fact]
    public void Each_kind_of_control_writes_the_record_it_was_given()
    {
        UpdateStock command = NewCommand();
        Guid id = command.Id;
        RenderForm(command);

        page.Find("#name").Change("Washer");
        page.Find("#description").Change("M8, zinc");
        page.Find("#quantity").Change("12");
        page.Find("#active").Change(true);
        page.Find("#shipped").Change("False");

        page.Instance.Command.ShouldBeSameAs(command);
        command.Name.ShouldBe("Washer");
        command.Description.ShouldBe("M8, zinc");
        command.Quantity.ShouldBe(12);
        command.IsActive.ShouldBeTrue();
        command.Shipped.ShouldBe(false);
        command.Id.ShouldBe(id);
    }

    [Fact]
    public void A_control_wired_by_hand_writes_the_record_through_the_resolver()
    {
        UpdateStock command = NewCommand();
        RenderForm(command);
        EditContext context = Context;

        page.Find("#owner").Focus();
        page.FindAll(".combobox-option").Single(o => o.TextContent.Trim() == "Grace").Click();

        command.Owner.ShouldBe("grace");
        page.Instance.Command.ShouldBeSameAs(command);
        context.IsModified(context.Field(nameof(UpdateStock.Owner))).ShouldBeTrue();
        Context.ShouldBeSameAs(context);
    }

    [Fact]
    public void The_form_keeps_one_context_through_edits_and_page_renders()
    {
        UpdateStock command = NewCommand();
        RenderForm(command);
        EditContext first = Context;
        first.Model.ShouldBeSameAs(command);

        page.Find("#name").Change("Washer");
        RenderPageAgain(command, ticks: 1);
        page.Find("#quantity").Change("12");
        RenderPageAgain(command, ticks: 2);

        page.Find("#ticks").TextContent.ShouldBe("2");
        Context.ShouldBeSameAs(first);
    }

    [Fact]
    public void What_the_form_knows_about_a_field_survives_the_record_changing_under_it()
    {
        UpdateStock command = NewCommand();
        RenderForm(command);
        EditContext context = Context;
        int hashBefore = command.GetHashCode();

        // Emptied, so the validator has something to say about it.
        page.Find("#name").Change("");
        context
            .GetValidationMessages(context.Field(nameof(UpdateStock.Name)))
            .ShouldBe(["Item name is required."]);

        page.Find("#description").Change("M8, zinc");
        RenderPageAgain(command, ticks: 1);

        // A record's hash code follows its values, so anything that filed the model under its
        // hash would now look in the wrong place.
        command.GetHashCode().ShouldNotBe(hashBefore);
        context.IsModified(context.Field(nameof(UpdateStock.Name))).ShouldBeTrue();
        context.IsModified(context.Field(nameof(UpdateStock.Quantity))).ShouldBeFalse();
        context
            .GetValidationMessages(context.Field(nameof(UpdateStock.Name)))
            .ShouldBe(["Item name is required."]);
        page.Find("#name").GetAttribute("aria-invalid").ShouldBe("true");
        page.Find("#name-error").TextContent.ShouldContain("Item name is required.");
    }

    [Fact]
    public void Submitting_hands_back_the_same_record_holding_what_was_typed()
    {
        UpdateStock command = NewCommand();
        RenderForm(command);
        EditContext first = Context;

        page.Find("#name").Change("Washer");
        page.Find("#quantity").Change("0");
        page.Find("form").Submit();
        page.Instance.Submission.ShouldBeNull();
        first.GetValidationMessages().ShouldBe(["Quantity must be at least 1."]);

        page.Find("#quantity").Change("12");
        page.Find("form").Submit();

        page.Instance.Submission.ShouldBeSameAs(first);
        page.Instance.Submission!.Model.ShouldBeSameAs(command);
        command.ShouldBe(NewCommand() with { Id = command.Id, Name = "Washer", Quantity = 12 });
    }

    [Fact]
    public void Another_record_with_the_same_values_is_another_form()
    {
        UpdateStock command = NewCommand() with { Name = "" };
        RenderForm(command);
        EditContext first = Context;
        page.Find("#quantity").Change("0");
        first.GetValidationMessages().ShouldNotBeEmpty();

        // What `command = command with { ... }` does to a page: equal by value, a different object.
        UpdateStock twin = command with
        { };
        twin.ShouldBe(command);
        RenderPageAgain(twin, ticks: 0);

        Context.ShouldNotBeSameAs(first);
        Context.Model.ShouldBeSameAs(twin);
        Context.GetValidationMessages().ShouldBeEmpty();
        Context.IsModified().ShouldBeFalse();

        // And the fields now write the new one.
        page.Find("#name").Change("Washer");
        twin.Name.ShouldBe("Washer");
        command.Name.ShouldBe("");
    }
}
