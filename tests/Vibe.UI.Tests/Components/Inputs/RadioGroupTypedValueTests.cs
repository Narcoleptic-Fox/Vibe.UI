namespace Vibe.UI.Tests.Components.Inputs;

/// <summary>
/// Charter rule 2 for <c>RadioGroup</c>: items carry typed values, so picking one reports a
/// <c>TValue</c> with no text conversion. Each case goes both ways: the bound value checks its
/// item, and picking an item reports its value.
/// </summary>
public class RadioGroupTypedValueTests : TestBase
{
    public enum Density
    {
        Compact,
        ComfortablySpaced,
        Roomy,
    }

    private static readonly Guid First = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Second = new("22222222-2222-2222-2222-222222222222");

    private sealed class Bound<TValue>(
        RadioGroupTypedValueTests test,
        TValue? initial,
        (string Label, TValue Value)[]? items,
        Action<ComponentParameterCollectionBuilder<RadioGroup<TValue>>>? more
    )
    {
        public TValue? Model = initial;
        public int Changes;
        private IRenderedComponent<RadioGroup<TValue>>? cut;

        /// <summary>Created on first use. A test that changes Model must read something first.</summary>
        public IRenderedComponent<RadioGroup<TValue>> Cut =>
            cut ??= test.Render<RadioGroup<TValue>>(Parameters);

        public void Rerender() => Cut.Render(Parameters);

        public string[] Labels() =>
            Cut.FindAll(".vibe-radio-group-item").Select(i => i.TextContent.Trim()).ToArray();

        public string? CheckedLabel() =>
            Cut.FindAll(".vibe-radio-group-item")
                .SingleOrDefault(i => i.QuerySelector("input")!.HasAttribute("checked"))
                ?.TextContent.Trim();

        public void Pick(string label) =>
            Cut.FindAll(".vibe-radio-group-item")
                .Single(i => i.TextContent.Trim() == label)
                .QuerySelector("input")!
                .Change(true);

        private void Parameters(ComponentParameterCollectionBuilder<RadioGroup<TValue>> parameters)
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
            if (items is not null)
            {
                // One fragment, not one AddChildContent per item: the latter cannot be re-rendered.
                parameters.Add<RenderFragment?>(
                    p => p.ChildContent,
                    builder =>
                    {
                        foreach (var (label, value) in items)
                        {
                            builder.OpenComponent<RadioGroupItem<TValue>>(0);
                            builder.AddAttribute(1, nameof(RadioGroupItem<TValue>.Value), value);
                            builder.AddAttribute(
                                2,
                                nameof(RadioGroupItem<TValue>.ChildContent),
                                (RenderFragment)(content => content.AddContent(0, label))
                            );
                            builder.SetKey(label);
                            builder.CloseComponent();
                        }
                    }
                );
            }
            more?.Invoke(parameters);
        }
    }

    private Bound<TValue> Bind<TValue>(
        TValue? initial,
        (string Label, TValue Value)[]? items,
        Action<ComponentParameterCollectionBuilder<RadioGroup<TValue>>>? more = null
    ) => new(this, initial, items, more);

    // === Both directions, per type ===

    [Fact]
    public void Int_binds_both_ways()
    {
        var bound = Bind(25, [("Ten", 10), ("Twenty-five", 25)]);

        bound.CheckedLabel().ShouldBe("Twenty-five");

        bound.Pick("Ten");

        bound.Model.ShouldBe(10);
        bound.CheckedLabel().ShouldBe("Ten");
    }

    [Fact]
    public void A_zero_value_is_a_real_selection_not_nothing_selected()
    {
        var bound = Bind(0, [("None", 0), ("One", 1)]);

        bound.CheckedLabel().ShouldBe("None");
    }

    [Fact]
    public void A_null_value_checks_nothing()
    {
        var bound = Bind<int?>(null, [("Zero", 0), ("One", 1)]);

        bound.CheckedLabel().ShouldBeNull();

        bound.Pick("Zero");

        bound.Model.ShouldBe(0);
    }

    [Fact]
    public void An_item_with_a_null_value_is_the_choice_for_nothing()
    {
        var bound = Bind<int?>(5, [("No limit", null), ("Five", 5)]);

        bound.CheckedLabel().ShouldBe("Five");
        // A null value has no text to post.
        bound.Cut.FindAll("input")[0].GetAttribute("value").ShouldBe(string.Empty);

        bound.Pick("No limit");

        bound.Model.ShouldBeNull();
        bound.Changes.ShouldBe(1);
        bound.CheckedLabel().ShouldBe("No limit");
    }

    [Fact]
    public void Bool_binds_both_ways()
    {
        var bound = Bind(true, [("Yes", true), ("No", false)]);

        bound.CheckedLabel().ShouldBe("Yes");

        bound.Pick("No");

        bound.Model.ShouldBeFalse();
        bound.Changes.ShouldBe(1);
    }

    [Fact]
    public void Guid_binds_both_ways()
    {
        var bound = Bind(First, [("Ada", First), ("Grace", Second)]);

        bound.CheckedLabel().ShouldBe("Ada");

        bound.Pick("Grace");

        bound.Model.ShouldBe(Second);
    }

    [Fact]
    public void String_binds_both_ways_as_before()
    {
        var bound = Bind("b", [("Alpha", "a"), ("Beta", "b")]);

        bound.CheckedLabel().ShouldBe("Beta");

        bound.Pick("Alpha");

        bound.Model.ShouldBe("a");
    }

    // === Enums ===

    [Fact]
    public void An_enum_with_no_items_given_renders_one_per_value_with_readable_labels()
    {
        var bound = Bind(Density.Roomy, items: null);

        bound.Labels().ShouldBe(["Compact", "Comfortably spaced", "Roomy"]);
        bound.CheckedLabel().ShouldBe("Roomy");

        bound.Pick("Comfortably spaced");

        bound.Model.ShouldBe(Density.ComfortablySpaced);
    }

    [Fact]
    public void A_nullable_enum_renders_its_values_and_starts_with_none_checked()
    {
        var bound = Bind<Density?>(null, items: null);

        bound.Labels().ShouldBe(["Compact", "Comfortably spaced", "Roomy"]);
        bound.CheckedLabel().ShouldBeNull();

        bound.Pick("Compact");

        bound.Model.ShouldBe(Density.Compact);
    }

    [Fact]
    public void Items_given_for_an_enum_replace_the_generated_ones()
    {
        var bound = Bind(Density.Compact, [("Tight", Density.Compact), ("Loose", Density.Roomy)]);

        bound.Labels().ShouldBe(["Tight", "Loose"]);
        bound.CheckedLabel().ShouldBe("Tight");
    }

    [Fact]
    public void A_type_that_is_not_an_enum_renders_no_items_of_its_own()
    {
        var bound = Bind(1, items: null);

        bound.Labels().ShouldBeEmpty();
    }

    // === Items whose own type is not the group's ===

    /// <summary>
    /// Renders a group of TGroup holding items compiled as TItem, which is what Razor produces
    /// for a literal such as Value="true" inside a group bound to bool?.
    /// </summary>
    private (IRenderedComponent<RadioGroup<TGroup>> Cut, Func<TGroup?> Model) RenderMixed<TGroup, TItem>(
        TGroup? initial,
        params (string Label, TItem Value)[] items
    )
    {
        TGroup? model = initial;
        var cut = Render<RadioGroup<TGroup>>(parameters =>
            parameters
                .Add(p => p.Value, initial)
                .Add(p => p.ValueChanged, value => model = value)
                .Add<RenderFragment?>(
                    p => p.ChildContent,
                    builder =>
                    {
                        foreach (var (label, value) in items)
                        {
                            builder.OpenComponent<RadioGroupItem<TItem>>(0);
                            builder.AddAttribute(1, nameof(RadioGroupItem<TItem>.Value), value);
                            builder.AddAttribute(
                                2,
                                nameof(RadioGroupItem<TItem>.ChildContent),
                                (RenderFragment)(content => content.AddContent(0, label))
                            );
                            builder.SetKey(label);
                            builder.CloseComponent();
                        }
                    }
                )
        );
        return (cut, () => model);
    }

    private static AngleSharp.Dom.IElement Radio(IRenderedComponent<IComponent> cut, string label) =>
        cut.FindAll(".vibe-radio-group-item").Single(i => i.TextContent.Trim() == label).QuerySelector("input")!;

    [Fact]
    public void Bool_items_work_inside_a_nullable_bool_group()
    {
        var (cut, model) = RenderMixed<bool?, bool>(null, ("Yes", true), ("No", false));

        // Without a shared link the items would have no group: no name, and a click would do nothing.
        Radio(cut, "No").GetAttribute("name").ShouldNotBeNullOrEmpty();
        cut.FindAll("input[checked]").ShouldBeEmpty();

        Radio(cut, "No").Change(true);

        model().ShouldBe(false);
        Radio(cut, "No").HasAttribute("checked").ShouldBeTrue();
        Radio(cut, "Yes").HasAttribute("checked").ShouldBeFalse();
    }

    [Fact]
    public void Int_items_work_inside_a_long_group()
    {
        var (cut, model) = RenderMixed<long, int>(5L, ("One", 1), ("Five", 5));

        Radio(cut, "Five").HasAttribute("checked").ShouldBeTrue();

        Radio(cut, "One").Change(true);

        model().ShouldBe(1L);
    }

    [Fact]
    public void Int_items_work_inside_an_enum_group()
    {
        var (cut, model) = RenderMixed<Density, int>(Density.Compact, ("Compact", 0), ("Roomy", 2));

        Radio(cut, "Compact").HasAttribute("checked").ShouldBeTrue();

        Radio(cut, "Roomy").Change(true);

        model().ShouldBe(Density.Roomy);
    }

    [Fact]
    public void An_item_whose_value_cannot_be_the_groups_type_fails_and_names_both()
    {
        var exception = Should.Throw<InvalidOperationException>(() =>
            RenderMixed<int, string>(1, ("Bad", "not a number"))
        );

        exception.Message.ShouldContain("String");
        exception.Message.ShouldContain("not a number");
        exception.Message.ShouldContain("RadioGroup<Int32>");
    }

    // === What the item puts in the page ===

    [Fact]
    public void The_html_value_is_written_in_the_invariant_format()
    {
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(
            "de-DE"
        );
        var bound = Bind(1.5m, [("One and a half", 1.5m)]);

        bound.Cut.Find("input").GetAttribute("value").ShouldBe("1.5");
    }

    [Fact]
    public void Items_share_the_group_name()
    {
        var bound = Bind(1, [("One", 1), ("Two", 2)], p => p.Add(x => x.Name, "count"));

        bound.Cut.FindAll("input").Select(i => i.GetAttribute("name")).ShouldBe(["count", "count"]);
    }

    // === Reporting ===

    [Fact]
    public void Picking_the_item_that_is_already_selected_reports_nothing()
    {
        var bound = Bind(10, [("Ten", 10), ("Twenty-five", 25)]);

        bound.Pick("Ten");

        bound.Changes.ShouldBe(0);
    }

    [Fact]
    public void A_disabled_group_reports_nothing()
    {
        var bound = Bind(10, [("Ten", 10), ("Twenty-five", 25)], p => p.Add(x => x.Disabled, true));

        bound.Pick("Twenty-five");

        bound.Model.ShouldBe(10);
        bound.Changes.ShouldBe(0);
    }

    [Fact]
    public void A_value_set_in_code_moves_the_checked_item()
    {
        var bound = Bind(10, [("Ten", 10), ("Twenty-five", 25)]);
        // Render first: the binding is created on first use, and without this the group would
        // only ever see 25.
        bound.CheckedLabel().ShouldBe("Ten");

        bound.Model = 25;
        bound.Rerender();

        bound.CheckedLabel().ShouldBe("Twenty-five");
    }
}
