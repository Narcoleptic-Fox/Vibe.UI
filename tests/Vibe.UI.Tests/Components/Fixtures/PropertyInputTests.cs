using System.Globalization;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Vibe.UI.Metadata;
using Account = Vibe.UI.Tests.Components.Fixtures.PropertyInputFixture.Account;
using AccountKind = Vibe.UI.Tests.Components.Fixtures.PropertyInputFixture.AccountKind;
using Priority = Vibe.UI.Tests.Components.Fixtures.PropertyInputFixture.Priority;

namespace Vibe.UI.Tests.Components.Fixtures;

/// <summary>
/// Charter step 5: a form field written in one line. The control comes from the property's type;
/// the label, required marker, limits and messages from its metadata; a parameter at the call
/// site overrides each one; and a change is written to the model before the form is told.
/// </summary>
public class PropertyInputTests : TestBase
{
    private IRenderedComponent<PropertyInputFixture> cut = default!;

    private EditContext Context => cut.Instance.Context;

    private Account Model => cut.Instance.Model;

    private void RenderForm(Account? model = null) =>
        cut = Render<PropertyInputFixture>(p => p.Add(f => f.Model, model ?? new Account()));

    private bool Validate() => cut.InvokeAsync(() => Context.Validate()).GetAwaiter().GetResult();

    private string[] Messages(string property) =>
        Context.GetValidationMessages(new FieldIdentifier(Model, property)).ToArray();

    private IElement Control(string id) => cut.Find("#" + id);

    private IElement Label(string id) => cut.Find($"#{id}-label");

    private static int Occurrences(
        IRenderedComponent<PropertyInputFixture> rendered,
        string text
    ) => rendered.FindAll("*").Count(e => e.ChildElementCount == 0 && e.TextContent.Trim() == text);

    // ---- The control comes from the type ----

    [Theory]
    [InlineData("name", "input", "text")]
    [InlineData("email", "input", "email")]
    [InlineData("age", "input", "number")]
    [InlineData("seats", "input", "number")]
    [InlineData("score", "input", "number")]
    [InlineData("active", "input", "checkbox")]
    [InlineData("born", "input", "date")]
    [InlineData("start", "input", "date")]
    [InlineData("changed", "input", "datetime-local")]
    [InlineData("owner", "input", "text")]
    [InlineData("title", "input", "text")]
    [InlineData("visits", "input", "number")]
    [InlineData("phone", "input", "tel")]
    [InlineData("site", "input", "url")]
    [InlineData("secret", "input", "password")]
    [InlineData("opens", "input", "time")]
    [InlineData("seen", "input", "datetime-local")]
    [InlineData("kind", "select", null)]
    [InlineData("priority", "select", null)]
    [InlineData("archived", "select", null)]
    [InlineData("notes", "textarea", null)]
    [InlineData("floors", "input", "number")]
    public void The_control_follows_the_property_type(string id, string element, string? type)
    {
        RenderForm();

        var control = Control(id);

        control.LocalName.ShouldBe(element);
        control.GetAttribute("type").ShouldBe(type);
    }

    [Fact]
    public void An_enum_gets_one_option_per_value_and_a_nullable_one_can_be_cleared()
    {
        RenderForm();

        Control("kind")
            .QuerySelectorAll("option")
            .Select(o => o.TextContent.Trim())
            .ShouldBe(["Person", "Small company"]);
        Control("priority")
            .QuerySelectorAll("option")
            .Select(o => o.GetAttribute("value"))
            .ShouldBe(["", "Low", "High"]);
    }

    [Fact]
    public void A_nullable_bool_is_yes_no_or_not_answered()
    {
        RenderForm();
        Control("archived")
            .QuerySelectorAll("option")
            .Select(o => o.TextContent.Trim())
            .ShouldBe(["", "Yes", "No"]);

        Control("archived").Change("False");
        Model.Archived.ShouldBe(false);

        Control("archived").Change("True");
        Model.Archived.ShouldBe(true);

        Control("archived").Change(string.Empty);
        Model.Archived.ShouldBeNull();
    }

    [Fact]
    public void Text_over_several_lines_is_a_textarea_wired_like_any_other_field()
    {
        RenderForm(new Account { Notes = "Ring twice" });
        var notes = Control("notes");

        notes.TextContent.ShouldBe("Ring twice");
        Label("notes").GetAttribute("for").ShouldBe("notes");
        Label("notes").TextContent.ShouldContain("*");
        notes.HasAttribute("required").ShouldBeTrue();
        notes.GetAttribute("minlength").ShouldBe("5");
        notes.GetAttribute("maxlength").ShouldBe("200");
        notes.GetAttribute("placeholder").ShouldBe("Anything we should know");
    }

    [Fact]
    public void A_textarea_writes_the_model_and_then_tells_the_form()
    {
        RenderForm();
        Validate();
        Messages(nameof(Account.Notes)).ShouldNotBeEmpty();

        Control("notes").Input("Ring twice");
        Control("notes").Change("Ring twice");

        Model.Notes.ShouldBe("Ring twice");
        Context.IsModified(new FieldIdentifier(Model, nameof(Account.Notes))).ShouldBeTrue();
        Messages(nameof(Account.Notes)).ShouldBeEmpty();
    }

    [Fact]
    public void A_date_shows_in_the_format_its_input_type_reads()
    {
        RenderForm();

        Control("start").GetAttribute("value").ShouldBe("2026-03-01");
        Control("changed").GetAttribute("value").ShouldBe("2026-03-01T09:30:00");
    }

    // ---- Label, required marker, limits and description come from the metadata ----

    [Fact]
    public void The_label_comes_from_the_metadata_and_points_at_the_control()
    {
        RenderForm();

        Label("email").TextContent.ShouldContain("Work email");
        Label("email").GetAttribute("for").ShouldBe("email");
        // No attribute names this one: the convention turns the property name into words.
        Label("owner").TextContent.Trim().ShouldBe("Owner id");
    }

    [Fact]
    public void A_checkbox_carries_its_label_beside_it_so_it_is_said_once()
    {
        RenderForm();

        cut.FindAll("#active-label").ShouldBeEmpty();
        Occurrences(cut, "Is active").ShouldBe(1);
    }

    [Fact]
    public void A_required_property_is_marked_and_the_control_is_told()
    {
        RenderForm();

        Label("name").QuerySelectorAll(".form-field-required").Length.ShouldBe(1);
        Control("name").HasAttribute("required").ShouldBeTrue();
        Label("age").QuerySelectorAll(".form-field-required").ShouldBeEmpty();
        Control("age").HasAttribute("required").ShouldBeFalse();
    }

    [Fact]
    public void Required_on_a_bool_does_not_become_must_be_ticked()
    {
        RenderForm();

        // The browser reads required on a checkbox as "tick this to submit".
        Control("active").HasAttribute("required").ShouldBeFalse();
        Control("active").HasAttribute("aria-required").ShouldBeFalse();
    }

    [Fact]
    public void Limits_reach_the_control()
    {
        RenderForm();

        Control("email").GetAttribute("minlength").ShouldBe("3");
        Control("email").GetAttribute("maxlength").ShouldBe("50");
        Control("age").GetAttribute("min").ShouldBe("18");
        Control("age").GetAttribute("max").ShouldBe("120");
        Control("born").GetAttribute("min").ShouldBe("1900-01-01");
        Control("born").GetAttribute("max").ShouldBe("2030-12-31");
        // Nothing sets a limit here, so nothing is invented.
        Control("seats").HasAttribute("min").ShouldBeFalse();
        Control("name").HasAttribute("maxlength").ShouldBeFalse();
    }

    [Fact]
    public void A_limit_is_written_the_way_the_browser_reads_it_whatever_the_culture()
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            // German writes 1,5. The browser's min and max take 1.5 and nothing else.
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            RenderForm();

            Control("score").GetAttribute("min").ShouldBe("1.5");
            Control("score").GetAttribute("max").ShouldBe("4.5");
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    [Fact]
    public void The_description_and_placeholder_come_from_the_metadata()
    {
        RenderForm();

        cut.Find("#email-description").TextContent.ShouldBe("We never share it.");
        Control("email").GetAttribute("aria-describedby")!.ShouldContain("email-description");
        Control("email").GetAttribute("placeholder").ShouldBe("you@work.com");
    }

    // ---- A parameter at the call site overrides each one ----

    [Fact]
    public void A_call_site_parameter_overrides_each_piece_of_metadata()
    {
        var overrides = Render<PropertyInputOverridesFixture>();
        IElement Find(string css) => overrides.Find(css);

        Find("#email-label").TextContent.ShouldContain("Contact");
        Find("#email-label").TextContent.ShouldNotContain("Work email");
        Find("#email-description").TextContent.ShouldBe("Used for receipts.");
        Find("#email").GetAttribute("placeholder").ShouldBe("name@example.com");
        Find("#email").HasAttribute("required").ShouldBeFalse();
        Find("#email-label").QuerySelectorAll(".form-field-required").ShouldBeEmpty();
        Find("#email").GetAttribute("minlength").ShouldBe("5");
        Find("#email").GetAttribute("maxlength").ShouldBe("20");
        Find("#email").GetAttribute("type").ShouldBe("text");

        Find("#age").HasAttribute("required").ShouldBeTrue();
        Find("#age").GetAttribute("min").ShouldBe("21");
        Find("#age").GetAttribute("max").ShouldBe("65");

        // A class and any other attribute land on the field's root, beside the field's own class.
        var root = Find("[data-field='age']");
        root.ClassList.ShouldContain("wide");
        root.ClassList.ShouldContain("vibe-form-field");

        // An empty label means none, which is not the same as leaving it to the metadata.
        overrides.FindAll("#seats-label").ShouldBeEmpty();

        Find("#active").HasAttribute("disabled").ShouldBeTrue();
        Find("#notes").HasAttribute("disabled").ShouldBeTrue();
        Find("#title").LocalName.ShouldBe("textarea");
        overrides.Find(".vibe-checkbox-label").TextContent.Trim().ShouldBe("Enabled");
    }

    [Fact]
    public void A_control_named_at_the_call_site_replaces_the_one_the_type_would_get()
    {
        var model = new Account { Seats = 2 };
        var overrides = Render<PropertyInputOverridesFixture>(p => p.Add(f => f.Model, model));

        overrides.Find(".seat-picker").TextContent.ShouldBe("2 seats");
        overrides.Find(".seat-picker").Click();

        model.Seats.ShouldBe(3);
        overrides.Find(".seat-picker").TextContent.ShouldBe("3 seats");
    }

    [Fact]
    public void A_named_control_that_can_name_its_field_tells_the_form()
    {
        var overrides = Render<PropertyInputOverridesFixture>();

        overrides.Find("#explicit").Change("7");

        // The message exists only if the form was told the field changed.
        overrides.Find("#explicit-error").TextContent.ShouldContain("between 18 and 120");
    }

    [Fact]
    public void A_date_limit_is_written_in_the_format_of_its_input_type()
    {
        var overrides = Render<PropertyInputOverridesFixture>();

        overrides.Find("#start").GetAttribute("min").ShouldBe("2026-01-02");
        overrides.Find("#start").GetAttribute("max").ShouldBe("2026-12-30");
        overrides.Find("#changed").GetAttribute("min").ShouldBe("2026-01-02T00:00:00");
        overrides.Find("#opens").GetAttribute("min").ShouldBe("09:00:00");
        overrides.Find("#seen").GetAttribute("min").ShouldBe("2026-01-02T08:00:00");
        overrides.Find("#born").GetAttribute("max").ShouldBe("2010-06-30");
        // The call site set only the latest date; the earliest still comes from the attribute.
        overrides.Find("#born").GetAttribute("min").ShouldBe("1900-01-01");
    }

    [Fact]
    public void A_control_named_in_a_model_profile_is_used_with_nothing_at_the_call_site()
    {
        Services.AddVibeMetadata(m =>
            m.Model<Account>(a => a.Property(x => x.Seats).Control<SeatPicker>().Label("Places"))
        );
        RenderForm(new Account { Seats = 4 });

        cut.Find(".seat-picker").TextContent.ShouldBe("4 seats");
        Label("seats").TextContent.Trim().ShouldBe("Places");
    }

    // ---- A change is written to the model, then the form is told ----

    [Fact]
    public void Text_is_written_to_the_model_and_the_form_validates_the_new_value()
    {
        RenderForm();
        Validate();
        Messages(nameof(Account.Name)).ShouldNotBeEmpty();

        Control("name").Change("Ada");

        Model.Name.ShouldBe("Ada");
        Context.IsModified(new FieldIdentifier(Model, nameof(Account.Name))).ShouldBeTrue();
        Messages(nameof(Account.Name)).ShouldBeEmpty();
    }

    [Fact]
    public void A_number_out_of_range_gets_the_forms_message_once_under_the_field()
    {
        RenderForm();

        Control("age").Change("7");

        Model.Age.ShouldBe(7);
        cut.Find("#age-error").TextContent.ShouldContain("between 18 and 120");
        Control("age").GetAttribute("aria-invalid").ShouldBe("true");
        Control("age").GetAttribute("aria-errormessage").ShouldBe("age-error");
    }

    [Fact]
    public void Text_that_is_not_a_number_is_said_once_and_leaves_the_model_alone()
    {
        RenderForm(new Account { Age = 30 });

        Control("age").Change("abc");

        Model.Age.ShouldBe(30);
        Occurrences(cut, "Enter a valid number.").ShouldBe(1);
        Validate().ShouldBeFalse();
    }

    [Fact]
    public void An_enum_a_checkbox_a_date_and_a_guid_are_each_written_as_their_own_type()
    {
        RenderForm();
        Validate();
        Messages(nameof(Account.Priority)).ShouldNotBeEmpty();
        var owner = Guid.NewGuid();

        Control("kind").Change("SmallCompany");
        Control("priority").Change("High");
        Control("active").Change(true);
        Control("born").Change("1990-05-17");
        Control("start").Change("2026-04-02");
        Control("owner").Change(owner.ToString());
        Control("score").Change("3.25");
        Control("title").Change("Dr");
        Control("visits").Change("5000000000");
        Control("opens").Change("08:15:00");

        Model.Kind.ShouldBe(AccountKind.SmallCompany);
        Model.Priority.ShouldBe(Priority.High);
        Model.IsActive.ShouldBeTrue();
        Model.Born.ShouldBe(new DateOnly(1990, 5, 17));
        Model.Start.ShouldBe(new System.DateTime(2026, 4, 2));
        Model.OwnerId.ShouldBe(owner);
        Model.Score.ShouldBe(3.25m);
        Model.Title.ShouldBe("Dr");
        Model.Visits.ShouldBe(5_000_000_000);
        Model.Opens.ShouldBe(new TimeOnly(8, 15));
        Messages(nameof(Account.Priority)).ShouldBeEmpty();
        Context.IsModified(new FieldIdentifier(Model, nameof(Account.IsActive))).ShouldBeTrue();
    }

    [Fact]
    public void An_init_only_property_is_written_in_place()
    {
        RenderForm();
        var model = Model;
        var context = Context;

        Control("nickname").Change("ace");

        model.Nickname.ShouldBe("ace");
        cut.Instance.Model.ShouldBeSameAs(model);
        cut.Instance.Context.ShouldBeSameAs(context);
    }

    [Fact]
    public void When_the_parent_swaps_the_model_writes_go_to_the_new_one()
    {
        // Not inside a form: an EditForm rebuilds its fields for a new model, which would hide
        // a PropertyInput that still held the old one.
        var first = new Account { Name = "Ada" };
        var second = new Account { Name = "Grace" };
        var page = Render<PropertyInputSwapFixture>(p => p.Add(f => f.Model, first));

        page.Render(p => p.Add(f => f.Model, second));
        page.Find("#name").GetAttribute("value").ShouldBe("Grace");
        page.Find("#name").Change("Hopper");

        second.Name.ShouldBe("Hopper");
        first.Name.ShouldBe("Ada");
    }

    // ---- Content that depends on the value ----

    [Fact]
    public void Inside_a_Form_content_that_depends_on_a_field_follows_it()
    {
        var page = Render<PropertyInputDependentFixture>(p => p.Add(f => f.InForm, true));
        page.FindAll("#company-only").ShouldBeEmpty();

        page.Find("#kind").Change("SmallCompany");

        page.FindAll("#company-only").Count.ShouldBe(1);
    }

    [Fact]
    public void Outside_a_form_the_caller_hears_the_change_and_its_content_follows()
    {
        var page = Render<PropertyInputDependentFixture>(p => p.Add(f => f.Listen, true));

        page.Find("#kind").Change("SmallCompany");

        page.Instance.Model.Kind.ShouldBe(AccountKind.SmallCompany);
        page.Instance.Heard.ShouldBe(AccountKind.SmallCompany);
        page.FindAll("#company-only").Count.ShouldBe(1);
    }

    [Fact]
    public void Outside_a_form_with_nobody_listening_the_model_is_still_written()
    {
        var page = Render<PropertyInputDependentFixture>();

        page.Find("#kind").Change("SmallCompany");

        page.Instance.Model.Kind.ShouldBe(AccountKind.SmallCompany);
        page.Find("#kind-label").TextContent.Trim().ShouldBe("Kind");
    }

    // ---- What it cannot edit, it says ----

    private sealed class Unsupported
    {
        public List<string> Tags { get; set; } = [];

        public string Fixed { get; } = "x";
    }

    [Fact]
    public void A_type_with_no_control_says_so_and_names_the_way_out()
    {
        var model = new Unsupported();

        var thrown = Should.Throw<InvalidOperationException>(() =>
            Render<PropertyInput<List<string>>>(p => p.Add(i => i.For, () => model.Tags))
        );

        thrown.Message.ShouldContain("Unsupported.Tags");
        thrown.Message.ShouldContain("Control");
    }

    [Fact]
    public void A_field_with_no_For_says_what_to_write()
    {
        var thrown = Should.Throw<InvalidOperationException>(() => Render<PropertyInput<string>>());

        thrown.Message.ShouldContain("For=\"() => model.Name\"");
    }

    [Fact]
    public void A_Form_follows_a_new_model_and_stops_listening_to_the_old_one()
    {
        var page = Render<PropertyInputDependentFixture>(p => p.Add(f => f.InForm, true));
        var form = page.FindComponent<Form<Account>>();
        var first = page.FindComponent<EditForm>().Instance.EditContext!;
        var renders = form.RenderCount;

        page.Render(p => p.Add(f => f.InForm, true).Add(f => f.Replacement, new Account()));
        var second = page.FindComponent<EditForm>().Instance.EditContext!;
        second.ShouldNotBeSameAs(first);
        renders = form.RenderCount;

        // The old context is no longer this form's business.
        page.InvokeAsync(() => first.NotifyFieldChanged(first.Field(nameof(Account.Name))));
        form.RenderCount.ShouldBe(renders);

        page.InvokeAsync(() => second.NotifyFieldChanged(second.Field(nameof(Account.Name))));
        form.RenderCount.ShouldBeGreaterThan(renders);

        // Once the form is gone, a change to its context reaches nothing.
        page.Render(p => p.Add(f => f.InForm, false));
        Should.NotThrow(() => page.InvokeAsync(() => second.NotifyFieldChanged(second.Field(nameof(Account.Name)))));
    }

    [Fact]
    public void A_property_with_no_setter_says_so()
    {
        var model = new Unsupported();

        var thrown = Should.Throw<InvalidOperationException>(() =>
            Render<PropertyInput<string>>(p => p.Add(i => i.For, () => model.Fixed))
        );

        thrown.Message.ShouldContain("Unsupported.Fixed");
        thrown.Message.ShouldContain("setter");
    }
}
