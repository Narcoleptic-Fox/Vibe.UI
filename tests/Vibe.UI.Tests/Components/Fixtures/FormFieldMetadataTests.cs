using System.Linq.Expressions;
using AngleSharp.Dom;
using Vibe.UI.Components;
using Account = Vibe.UI.Tests.Components.Fixtures.PropertyInputFixture.Account;

namespace Vibe.UI.Tests.Components.Fixtures;

/// <summary>
/// B2.3: <c>FormField</c> is the generic wrapper. Around a control the caller supplies it gives the
/// same label, description, required marker and messages that <c>PropertyInput</c> gives around a
/// control it picks, from the same metadata, with the call site winning.
/// </summary>
public class FormFieldMetadataTests : TestBase
{
    private IRenderedComponent<FormFieldMetadataFixture> cut = default!;

    private void RenderForm() => cut = Render<FormFieldMetadataFixture>();

    private IElement Control(string id) => cut.Find("#" + id);

    private IElement Label(string id) => cut.Find($"#{id}-label");

    [Fact]
    public void Unset_at_the_call_site_the_label_description_and_required_marker_come_from_the_metadata()
    {
        RenderForm();

        Label("email").TextContent.ShouldContain("Work email");
        Label("email").GetAttribute("for").ShouldBe("email");
        Label("email").QuerySelectorAll(".form-field-required").Length.ShouldBe(1);
        cut.Find("#email-description").TextContent.ShouldBe("We never share it.");
        Control("email").HasAttribute("required").ShouldBeTrue();
        Control("email").GetAttribute("aria-describedby").ShouldBe("email-description");
    }

    [Fact]
    public void A_call_site_value_wins_over_the_metadata()
    {
        RenderForm();

        // Age has no [Required] and its label would be "Age".
        Label("age").TextContent.ShouldContain("Years");
        Label("age").QuerySelectorAll(".form-field-required").Length.ShouldBe(1);
        cut.Find("#age-description").TextContent.ShouldBe("Whole years.");
    }

    [Fact]
    public void An_empty_string_at_the_call_site_shows_nothing_where_the_metadata_has_something()
    {
        RenderForm();

        cut.FindAll("#quiet-label").ShouldBeEmpty();
        cut.FindAll("#quiet-description").ShouldBeEmpty();
        Control("quiet").HasAttribute("required").ShouldBeFalse();
    }

    [Fact]
    public void Required_from_the_metadata_does_not_reach_a_bool_but_said_at_the_call_site_it_does()
    {
        RenderForm();

        // [Required] on a bool is what a NotNull rule reports. On a checkbox the browser would
        // read it as "must be ticked".
        Label("active").QuerySelectorAll(".form-field-required").ShouldBeEmpty();
        Control("active").HasAttribute("required").ShouldBeFalse();

        Label("terms").QuerySelectorAll(".form-field-required").Length.ShouldBe(1);
        Control("terms").HasAttribute("required").ShouldBeTrue();

        // A bool? can be unanswered, so required still asks for something.
        Label("answered").QuerySelectorAll(".form-field-required").Length.ShouldBe(1);
    }

    [Fact]
    public void A_control_brought_by_the_app_is_wired_through_the_public_field_context()
    {
        RenderForm();
        var picker = cut.Find(".field-aware-picker");

        picker.Id.ShouldBe("age");
        Label("age").GetAttribute("for").ShouldBe(picker.Id);
        picker.GetAttribute("aria-required").ShouldBe("true");
        picker.GetAttribute("aria-describedby").ShouldBe("age-description");
        picker.HasAttribute("aria-invalid").ShouldBeFalse();

        // Age is [Range(18, 120)]: one press makes it 1, which the form then finds invalid.
        picker.Click();
        cut.InvokeAsync(() => cut.Instance.Context.Validate());

        picker = cut.Find(".field-aware-picker");
        picker.GetAttribute("aria-invalid").ShouldBe("true");
        picker.GetAttribute("aria-describedby").ShouldBe("age-description age-error");
        cut.Find("#age-error").TextContent.ShouldContain("between 18 and 120");
    }

    [Fact]
    public void A_field_that_is_not_a_property_and_a_field_with_no_For_show_only_what_the_call_site_says()
    {
        RenderForm();

        cut.FindAll("#code-label").ShouldBeEmpty();
        Control("code").HasAttribute("required").ShouldBeFalse();
        Label("plain").TextContent.Trim().ShouldBe("Plain");
    }

    [Fact]
    public void A_property_with_no_setter_still_gets_its_label_from_the_metadata()
    {
        RenderForm();

        Label("reference").TextContent.Trim().ShouldBe("Order reference");
    }

    [Fact]
    public void Without_the_metadata_services_a_field_still_shows_what_the_call_site_says()
    {
        // An app that never called AddVibeUI: nothing is registered.
        using var bare = new BunitContext();
        var model = new Account();
        Expression<Func<string?>> email = () => model.Email;

        var field = bare.Render<FormField<string?>>(p =>
            p.Add(f => f.Id, "email").Add(f => f.For, email).Add(f => f.Description, "Typed here.")
        );

        field.FindAll("#email-label").ShouldBeEmpty();
        field.Find("#email-description").TextContent.ShouldBe("Typed here.");
    }

    [Fact]
    public void The_old_parameter_name_is_an_error_that_says_what_to_write()
    {
        var model = new Account();
        Expression<Func<string?>> email = () => model.Email;

        var error = Should.Throw<InvalidOperationException>(() =>
            Render<FormField<string?>>(p => p.AddUnmatched("ValidationFor", email))
        );

        error.Message.ShouldContain("Use For");
    }
}
