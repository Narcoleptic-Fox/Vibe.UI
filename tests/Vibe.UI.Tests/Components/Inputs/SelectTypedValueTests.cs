namespace Vibe.UI.Tests.Components.Inputs;

/// <summary>
/// Charter rule 2: a value-bearing input is generic, and binds int, int?, an enum and Guid without
/// a converter. Each case goes both ways: model to selected option, and a change event to model.
/// </summary>
public class SelectTypedValueTests : TestBase
{
    public enum Priority
    {
        Low,
        NeedsReview,
        High,
    }

    private static readonly Guid First = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Second = new("22222222-2222-2222-2222-222222222222");

    private IRenderedComponent<Select<TValue>> RenderSelect<TValue>(
        TValue? value,
        Action<TValue?> onChanged,
        string options,
        Action<ComponentParameterCollectionBuilder<Select<TValue>>>? more = null
    ) =>
        Render<Select<TValue>>(parameters =>
        {
            parameters
                .Add(p => p.Value, value)
                .Add(p => p.ValueChanged, onChanged)
                .AddChildContent(options);
            more?.Invoke(parameters);
        });

    [Fact]
    public void Int_binds_both_ways()
    {
        int model = 25;
        var cut = RenderSelect<int>(
            model,
            v => model = v,
            "<option value='10'>10</option><option value='25'>25</option>"
        );

        cut.Find("select").GetAttribute("value").ShouldBe("25");
        cut.Find("select").Change("10");

        model.ShouldBe(10);
    }

    [Fact]
    public void Nullable_int_binds_both_ways_and_can_be_cleared()
    {
        int? model = 10;
        var cut = RenderSelect<int?>(
            model,
            v => model = v,
            "<option value='10'>10</option><option value='25'>25</option>"
        );

        cut.Find("select").GetAttribute("value").ShouldBe("10");
        cut.Find("select").Change("25");
        model.ShouldBe(25);

        // A nullable value gets a selectable empty option without being asked.
        cut.Find("option[value='']").HasAttribute("disabled").ShouldBeFalse();
        cut.Find("select").Change("");
        model.ShouldBeNull();
    }

    [Fact]
    public void Enum_binds_both_ways_by_name()
    {
        Priority model = Priority.High;
        var cut = RenderSelect<Priority>(
            model,
            v => model = v,
            "<option value='Low'>Low</option><option value='High'>High</option>"
        );

        cut.Find("select").GetAttribute("value").ShouldBe("High");
        cut.Find("select").Change("Low");

        model.ShouldBe(Priority.Low);
    }

    [Fact]
    public void A_bool_binds_both_ways_and_a_nullable_one_can_be_cleared()
    {
        // The framework's converter reads a bool only from a bool, and an option sends text.
        bool flag = true;
        var plain = RenderSelect<bool>(
            flag,
            v => flag = v,
            "<option value='True'>Yes</option><option value='False'>No</option>"
        );
        plain.Find("select").GetAttribute("value").ShouldBe("True");
        plain.Find("select").Change("False");
        flag.ShouldBeFalse();

        bool? answer = null;
        var nullable = RenderSelect<bool?>(
            answer,
            v => answer = v,
            "<option value='True'>Yes</option><option value='False'>No</option>"
        );
        nullable.Find("select").Change("True");
        answer.ShouldBe(true);
        nullable.Find("select").Change("");
        answer.ShouldBeNull();

        Should.Throw<InvalidOperationException>(() => plain.Find("select").Change("maybe"));
        // Only the empty option clears a nullable bool. Other text is still a mistake.
        Should.Throw<InvalidOperationException>(() => nullable.Find("select").Change("maybe"));
        Should.Throw<InvalidOperationException>(() => plain.Find("select").Change(""));
    }

    [Fact]
    public void Guid_binds_both_ways()
    {
        Guid model = First;
        var cut = RenderSelect<Guid>(
            model,
            v => model = v,
            $"<option value='{First}'>One</option><option value='{Second}'>Two</option>"
        );

        cut.Find("select").GetAttribute("value").ShouldBe(First.ToString());
        cut.Find("select").Change(Second.ToString());

        model.ShouldBe(Second);
    }

    [Fact]
    public void Decimal_round_trips_in_the_invariant_culture_whatever_the_current_culture_is()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(
            "de-DE"
        );
        try
        {
            decimal model = 1.5m;
            var cut = RenderSelect<decimal>(
                model,
                v => model = v,
                "<option value='1.5'>1.5</option><option value='2.5'>2.5</option>"
            );

            // German formatting would write "1,5", which matches no option.
            cut.Find("select").GetAttribute("value").ShouldBe("1.5");
            cut.Find("select").Change("2.5");

            model.ShouldBe(2.5m);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void String_still_reports_an_empty_string_not_null()
    {
        string? model = "us";
        var cut = RenderSelect<string>(
            model,
            v => model = v,
            "<option value=''>None</option><option value='us'>US</option>"
        );

        cut.Find("select").Change("");

        model.ShouldBe(string.Empty);
    }

    [Fact]
    public void An_enum_with_no_options_written_lists_its_values_with_readable_labels()
    {
        Priority model = Priority.Low;
        var cut = Render<Select<Priority>>(parameters =>
            parameters.Add(p => p.Value, model).Add(p => p.ValueChanged, v => model = v)
        );

        cut.FindAll("option")
            .Select(o => (o.GetAttribute("value"), o.TextContent))
            .ShouldBe([("Low", "Low"), ("NeedsReview", "Needs review"), ("High", "High")]);

        cut.Find("select").Change("NeedsReview");
        model.ShouldBe(Priority.NeedsReview);
    }

    [Fact]
    public void A_nullable_enum_lists_its_values_after_an_empty_option_that_clears_it()
    {
        Priority? model = Priority.High;
        var cut = Render<Select<Priority?>>(parameters =>
            parameters
                .Add(p => p.Value, model)
                .Add(p => p.ValueChanged, v => model = v)
                .Add(p => p.Placeholder, "Any priority")
        );

        cut.FindAll("option")
            .Select(o => o.TextContent)
            .ShouldBe(["Any priority", "Low", "Needs review", "High"]);
        cut.Find("select").GetAttribute("value").ShouldBe("High");

        cut.Find("select").Change("");
        model.ShouldBeNull();
    }

    [Fact]
    public void A_null_value_selects_the_empty_option_and_a_non_nullable_default_does_not()
    {
        var nullable = RenderSelect<int?>(
            null,
            _ => { },
            "<option value='0'>Zero</option>",
            p => p.Add(x => x.Placeholder, "Pick one")
        );
        var plain = RenderSelect<int>(
            0,
            _ => { },
            "<option value='0'>Zero</option>",
            p => p.Add(x => x.Placeholder, "Pick one")
        );

        nullable.Find("option[value='']").HasAttribute("selected").ShouldBeTrue();
        // An int is always some number: 0 is the "Zero" option, never "nothing selected".
        plain.Find("option[value='']").HasAttribute("selected").ShouldBeFalse();
        plain.Find("select").GetAttribute("value").ShouldBe("0");
    }

    [Fact]
    public void Clearable_can_be_turned_off_for_a_nullable_type_and_on_for_a_string()
    {
        var required = RenderSelect<int?>(
            null,
            _ => { },
            "<option value='1'>One</option>",
            p => p.Add(x => x.Clearable, false).Add(x => x.Placeholder, "Pick one")
        );
        var optional = RenderSelect<string>(
            "us",
            _ => { },
            "<option value='us'>US</option>",
            p => p.Add(x => x.Clearable, true).Add(x => x.Placeholder, "No country")
        );

        required.Find("option[value='']").HasAttribute("disabled").ShouldBeTrue();
        optional.Find("option[value='']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void A_string_placeholder_stays_a_disabled_prompt_as_before()
    {
        var cut = RenderSelect<string>(
            null,
            _ => { },
            "<option value='us'>US</option>",
            p => p.Add(x => x.Placeholder, "Choose a country")
        );

        cut.Find("option[value='']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("option[value='']").HasAttribute("selected").ShouldBeTrue();
    }

    [Fact]
    public void An_option_value_that_is_not_valid_for_the_type_fails_loudly_and_leaves_the_model_alone()
    {
        int model = 10;
        var cut = RenderSelect<int>(
            model,
            v => model = v,
            "<option value='10'>10</option><option value='ten'>Ten</option>"
        );

        var error = Should.Throw<InvalidOperationException>(() => cut.Find("select").Change("ten"));

        error.Message.ShouldContain("'ten'");
        model.ShouldBe(10);
    }

    [Fact]
    public void A_number_that_is_not_a_defined_enum_value_is_rejected()
    {
        Priority model = Priority.Low;
        var cut = RenderSelect<Priority>(
            model,
            v => model = v,
            "<option value='Low'>Low</option><option value='99'>Bad</option>"
        );

        Should.Throw<InvalidOperationException>(() => cut.Find("select").Change("99"));

        model.ShouldBe(Priority.Low);
    }

    [Fact]
    public void A_change_event_with_no_value_is_read_as_the_empty_option()
    {
        int? cleared = 10;
        string? text = "us";
        var nullable = RenderSelect<int?>(cleared, v => cleared = v, "<option value='10'>10</option>");
        var plain = RenderSelect<string>(text, v => text = v, "<option value='us'>US</option>");

        nullable.Find("select").TriggerEvent("onchange", new ChangeEventArgs { Value = null });
        plain.Find("select").TriggerEvent("onchange", new ChangeEventArgs { Value = null });

        cleared.ShouldBeNull();
        text.ShouldBe(string.Empty);
    }

    [Fact]
    public void A_disabled_select_does_not_change_the_model()
    {
        int model = 10;
        var cut = RenderSelect<int>(
            model,
            v => model = v,
            "<option value='10'>10</option><option value='25'>25</option>",
            p => p.Add(x => x.Disabled, true)
        );

        cut.Find("select").Change("25");

        model.ShouldBe(10);
    }
}
