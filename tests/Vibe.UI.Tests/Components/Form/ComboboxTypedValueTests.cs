namespace Vibe.UI.Tests.Components.Form;

/// <summary>
/// Charter rule 2 for <c>Combobox</c>: the options carry typed values, so picking one reports a
/// <c>TValue</c> with no text conversion in between. Each case goes both ways: the bound value
/// shows its option's label, and picking an option reports its value.
/// </summary>
public class ComboboxTypedValueTests : TestBase
{
    public enum Priority
    {
        Low,
        NeedsReview,
        High,
    }

    private static readonly Guid First = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Second = new("22222222-2222-2222-2222-222222222222");

    private sealed class Bound<TValue>(
        ComboboxTypedValueTests test,
        TValue? initial,
        List<ComboboxOption<TValue>>? options,
        Action<ComponentParameterCollectionBuilder<Combobox<TValue>>>? more
    )
    {
        public TValue? Model = initial;
        public int Changes;
        private IRenderedComponent<Combobox<TValue>>? cut;

        public IRenderedComponent<Combobox<TValue>> Cut =>
            cut ??= test.Render<Combobox<TValue>>(Parameters);

        public AngleSharp.Dom.IElement Input => Cut.Find("input");

        public string? Text => Input.GetAttribute("value");

        public void Rerender() => Cut.Render(Parameters);

        public void Pick(string label)
        {
            Input.Focus();
            Cut.FindAll(".combobox-option").Single(o => o.TextContent.Trim() == label).Click();
        }

        public string[] Labels()
        {
            Input.Focus();
            return Cut.FindAll(".combobox-option").Select(o => o.TextContent.Trim()).ToArray();
        }

        private void Parameters(ComponentParameterCollectionBuilder<Combobox<TValue>> parameters)
        {
            parameters
                .Add(p => p.Value, Model)
                .Add(
                    p => p.ValueChanged,
                    value =>
                    {
                        Model = value;
                        Changes++;
                    }
                );
            if (options is not null)
            {
                parameters.Add(p => p.Options, options);
            }
            more?.Invoke(parameters);
        }
    }

    private Bound<TValue> Bind<TValue>(
        TValue? initial,
        List<ComboboxOption<TValue>>? options,
        Action<ComponentParameterCollectionBuilder<Combobox<TValue>>>? more = null
    ) => new(this, initial, options, more);

    private static List<ComboboxOption<TValue>> Options<TValue>(
        params (string Label, TValue Value)[] items
    ) =>
        items.Select(i => new ComboboxOption<TValue> { Label = i.Label, Value = i.Value }).ToList();

    // === Both directions, per type ===

    [Fact]
    public void Int_binds_both_ways()
    {
        var bound = Bind(25, Options(("Ten", 10), ("Twenty-five", 25)));

        bound.Text.ShouldBe("Twenty-five");

        bound.Pick("Ten");

        bound.Model.ShouldBe(10);
        bound.Text.ShouldBe("Ten");
    }

    [Fact]
    public void A_zero_value_is_a_real_selection_not_nothing_selected()
    {
        var bound = Bind(0, Options(("None", 0), ("One", 1)));

        bound.Text.ShouldBe("None");
        bound.Input.Focus();
        bound.Cut.Find(".combobox-option.selected").TextContent.Trim().ShouldBe("None");
    }

    [Fact]
    public void Nullable_int_binds_both_ways_and_emptying_the_text_clears_it()
    {
        var bound = Bind<int?>(10, Options<int?>(("Ten", 10), ("Twenty-five", 25)));

        bound.Text.ShouldBe("Ten");
        bound.Pick("Twenty-five");
        bound.Model.ShouldBe(25);

        bound.Input.Input("");

        bound.Model.ShouldBeNull();
        bound.Changes.ShouldBe(2);
        bound.Cut.FindAll(".combobox-option.selected").ShouldBeEmpty();
    }

    [Fact]
    public void Nothing_selected_shows_no_text_and_no_selected_option()
    {
        var bound = Bind<int?>(null, Options<int?>(("Ten", 10)));

        bound.Text.ShouldBe(string.Empty);
        bound.Input.Focus();
        bound.Cut.FindAll(".combobox-option.selected").ShouldBeEmpty();
    }

    [Fact]
    public void Guid_binds_both_ways()
    {
        var bound = Bind(First, Options(("Ada", First), ("Grace", Second)));

        bound.Text.ShouldBe("Ada");

        bound.Pick("Grace");

        bound.Model.ShouldBe(Second);
    }

    [Fact]
    public void String_binds_both_ways_as_before()
    {
        var bound = Bind("us", Options(("United States", "us"), ("Canada", "ca")));

        bound.Text.ShouldBe("United States");

        bound.Pick("Canada");

        bound.Model.ShouldBe("ca");
    }

    [Fact]
    public void Opening_with_a_value_selected_lists_every_option_and_starts_on_the_selected_one()
    {
        var bound = Bind(25, Options(("Ten", 10), ("Twenty-five", 25), ("Fifty", 50)));

        bound.Labels().ShouldBe(["Ten", "Twenty-five", "Fifty"]);
        bound.Cut.Find(".combobox-option.highlighted").TextContent.Trim().ShouldBe("Twenty-five");
    }

    [Fact]
    public void Typing_filters_the_list_even_when_a_value_is_selected()
    {
        var bound = Bind(25, Options(("Ten", 10), ("Twenty-five", 25), ("Fifty", 50)));

        bound.Input.Input("f");

        bound.Cut.FindAll(".combobox-option").Select(o => o.TextContent.Trim()).ShouldBe(["Twenty-five", "Fifty"]);
    }

    // === Enums ===

    [Fact]
    public void An_enum_with_no_options_given_offers_its_own_values_with_readable_labels()
    {
        var bound = Bind(Priority.High, options: null);

        bound.Text.ShouldBe("High");
        bound.Labels().ShouldBe(["Low", "Needs review", "High"]);

        bound.Pick("Needs review");

        bound.Model.ShouldBe(Priority.NeedsReview);
    }

    [Fact]
    public void A_nullable_enum_offers_its_values_and_clears_to_null()
    {
        var bound = Bind<Priority?>(Priority.Low, options: null);

        bound.Text.ShouldBe("Low");
        bound.Labels().ShouldBe(["Low", "Needs review", "High"]);

        bound.Input.Input("");

        bound.Model.ShouldBeNull();
    }

    [Fact]
    public void Options_given_for_an_enum_replace_the_generated_ones()
    {
        var bound = Bind(
            Priority.Low,
            Options(("Urgent", Priority.High), ("Whenever", Priority.Low))
        );

        bound.Text.ShouldBe("Whenever");
        bound.Labels().ShouldBe(["Urgent", "Whenever"]);
    }

    [Fact]
    public void An_empty_option_list_given_for_an_enum_stays_empty()
    {
        var bound = Bind(Priority.Low, new List<ComboboxOption<Priority>>());

        bound.Labels().ShouldBeEmpty();
    }

    // === Clearing ===

    [Fact]
    public void Emptying_the_text_does_not_clear_a_value_that_cannot_be_null()
    {
        var bound = Bind(10, Options(("Ten", 10)));

        bound.Input.Input("");

        bound.Model.ShouldBe(10);
        bound.Changes.ShouldBe(0);
    }

    [Fact]
    public void Emptying_the_text_does_not_clear_a_string_unless_asked()
    {
        var plain = Bind("us", Options(("United States", "us")));
        var clearable = Bind(
            "us",
            Options(("United States", "us")),
            p => p.Add(x => x.Clearable, true)
        );

        plain.Input.Input("");
        clearable.Input.Input("");

        plain.Model.ShouldBe("us");
        clearable.Model.ShouldBeNull();
    }

    [Fact]
    public void Clearable_false_keeps_a_nullable_value_when_the_text_is_emptied()
    {
        var bound = Bind<int?>(10, Options<int?>(("Ten", 10)), p => p.Add(x => x.Clearable, false));

        bound.Input.Input("");

        bound.Model.ShouldBe(10);
    }

    [Fact]
    public void Typing_to_filter_does_not_clear_the_value()
    {
        var bound = Bind<int?>(10, Options<int?>(("Ten", 10), ("Twenty-five", 25)));

        bound.Input.Input("Tw");

        bound.Model.ShouldBe(10);
        bound.Changes.ShouldBe(0);
    }

    // === Free text ===

    [Fact]
    public void Free_text_is_converted_to_the_value_type()
    {
        var bound = Bind<int?>(
            null,
            Options<int?>(("Ten", 10)),
            p => p.Add(x => x.AllowFreeText, true)
        );

        bound.Input.Input("42");
        bound.Input.KeyDown("Escape");
        bound.Input.KeyDown("Enter");

        bound.Model.ShouldBe(42);
    }

    [Fact]
    public void Free_text_that_is_not_a_value_is_ignored_and_does_not_throw()
    {
        var bound = Bind<int?>(
            7,
            Options<int?>(("Ten", 10)),
            p => p.Add(x => x.AllowFreeText, true)
        );

        bound.Input.Input("abc");
        bound.Input.KeyDown("Escape");
        Should.NotThrow(() => bound.Input.KeyDown("Enter"));

        bound.Model.ShouldBe(7);
        bound.Changes.ShouldBe(0);
    }

    [Fact]
    public void A_free_text_value_with_no_option_is_shown_in_the_invariant_format()
    {
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(
            "de-DE"
        );
        var bound = Bind(1.5m, Options(("One", 1m)), p => p.Add(x => x.AllowFreeText, true));

        bound.Text.ShouldBe("1.5");
    }

    // === The parent rendering again ===

    [Fact]
    public void A_value_set_in_code_changes_the_text()
    {
        var bound = Bind(10, Options(("Ten", 10), ("Twenty-five", 25)));
        // Render first. The binding is created on first use, so without this the component would
        // only ever see 25 and the test could not fail.
        bound.Text.ShouldBe("Ten");

        bound.Model = 25;
        bound.Rerender();

        bound.Text.ShouldBe("Twenty-five");
    }

    [Fact]
    public void A_rerender_with_the_same_value_does_not_rewrite_what_is_being_typed()
    {
        var options = Options(("Ten", 10), ("Twenty-five", 25));
        var bound = Bind(10, options);

        bound.Input.Input("Tw");
        bound.Rerender();

        bound.Text.ShouldBe("Tw");
    }
}
