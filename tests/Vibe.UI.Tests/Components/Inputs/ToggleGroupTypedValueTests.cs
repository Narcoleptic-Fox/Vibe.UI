namespace Vibe.UI.Tests.Components.Inputs;

/// <summary>
/// Charter rule 2 for <c>ToggleGroup</c>: items carry typed values, in single and in multiple
/// selection. Each case goes both ways: the bound value presses its item, and pressing an item
/// reports its value.
/// </summary>
public class ToggleGroupTypedValueTests : TestBase
{
    public enum Alignment
    {
        Left,
        CenterAligned,
        Right,
    }

    /// <summary>
    /// A group of TGroup holding items compiled as TItem. They differ when Razor types an item by
    /// its own literal, such as Value="true" inside a group bound to bool?.
    /// </summary>
    private sealed class Bound<TGroup, TItem>(
        ToggleGroupTypedValueTests test,
        ToggleGroupType type,
        TGroup? value,
        List<TGroup>? values,
        (string Label, TItem Value)[]? items
    )
    {
        public TGroup? Model = value;
        public List<TGroup> Models = values ?? [];
        public int Changes;
        private IRenderedComponent<ToggleGroup<TGroup>>? cut;

        /// <summary>Created on first use. A test that changes the model must read something first.</summary>
        public IRenderedComponent<ToggleGroup<TGroup>> Cut =>
            cut ??= test.Render<ToggleGroup<TGroup>>(Parameters);

        public void Rerender() => Cut.Render(Parameters);

        public string[] Labels() =>
            Cut.FindAll(".vibe-toggle-group-item").Select(i => i.TextContent.Trim()).ToArray();

        public string[] Pressed() =>
            Cut.FindAll(".vibe-toggle-group-item-pressed")
                .Select(i => i.TextContent.Trim())
                .ToArray();

        public void Press(string label) =>
            Cut.FindAll(".vibe-toggle-group-item")
                .Single(i => i.TextContent.Trim() == label)
                .Click();

        private void Parameters(ComponentParameterCollectionBuilder<ToggleGroup<TGroup>> parameters)
        {
            parameters
                .Add(p => p.Type, type)
                .Add(p => p.Value, Model)
                .Add(p => p.Values, Models)
                .Add(
                    p => p.ValueChanged,
                    changed =>
                    {
                        Model = changed;
                        Changes++;
                    }
                )
                .Add(
                    p => p.ValuesChanged,
                    changed =>
                    {
                        Models = changed;
                        Changes++;
                    }
                );
            if (items is not null)
            {
                parameters.Add<RenderFragment?>(
                    p => p.ChildContent,
                    builder =>
                    {
                        foreach (var (label, itemValue) in items)
                        {
                            builder.OpenComponent<ToggleGroupItem<TItem>>(0);
                            builder.AddAttribute(
                                1,
                                nameof(ToggleGroupItem<TItem>.Value),
                                itemValue
                            );
                            builder.AddAttribute(
                                2,
                                nameof(ToggleGroupItem<TItem>.ChildContent),
                                (RenderFragment)(content => content.AddContent(0, label))
                            );
                            builder.SetKey(label);
                            builder.CloseComponent();
                        }
                    }
                );
            }
        }
    }

    private Bound<T, T> Single<T>(T? value, params (string Label, T Value)[] items) =>
        new(this, ToggleGroupType.Single, value, null, items);

    private Bound<T, T> SingleFromEnum<T>(T? value) =>
        new(this, ToggleGroupType.Single, value, null, null);

    private Bound<T, T> Multiple<T>(List<T> values, params (string Label, T Value)[] items) =>
        new(this, ToggleGroupType.Multiple, default, values, items);

    // === Single selection ===

    [Fact]
    public void Single_int_binds_both_ways()
    {
        var bound = Single(25, ("Ten", 10), ("Twenty-five", 25));

        bound.Pressed().ShouldBe(["Twenty-five"]);

        bound.Press("Ten");

        bound.Model.ShouldBe(10);
        bound.Pressed().ShouldBe(["Ten"]);
    }

    [Fact]
    public void Single_zero_is_a_real_selection()
    {
        var bound = Single(0, ("None", 0), ("One", 1));

        bound.Pressed().ShouldBe(["None"]);
    }

    [Fact]
    public void Pressing_the_selected_item_clears_a_value_that_can_be_null()
    {
        var bound = Single<int?>(10, ("Ten", 10), ("Twenty-five", 25));

        bound.Press("Ten");

        bound.Model.ShouldBeNull();
        bound.Changes.ShouldBe(1);
        bound.Pressed().ShouldBeEmpty();
    }

    [Fact]
    public void Pressing_the_selected_item_keeps_a_value_that_cannot_be_null()
    {
        var bound = Single(10, ("Ten", 10), ("Twenty-five", 25));

        bound.Press("Ten");

        // There is no int that means "nothing", so reporting 0 would invent a selection.
        bound.Model.ShouldBe(10);
        bound.Changes.ShouldBe(0);
        bound.Pressed().ShouldBe(["Ten"]);
    }

    [Fact]
    public void Single_string_binds_both_ways_and_clears_as_before()
    {
        var bound = Single("b", ("Alpha", "a"), ("Beta", "b"));

        bound.Pressed().ShouldBe(["Beta"]);

        bound.Press("Alpha");
        bound.Model.ShouldBe("a");

        bound.Press("Alpha");
        bound.Model.ShouldBeNull();
    }

    [Fact]
    public void A_single_enum_with_no_items_given_renders_one_per_value_with_readable_labels()
    {
        var bound = SingleFromEnum(Alignment.Right);

        bound.Labels().ShouldBe(["Left", "Center aligned", "Right"]);
        bound.Pressed().ShouldBe(["Right"]);

        bound.Press("Center aligned");

        bound.Model.ShouldBe(Alignment.CenterAligned);
    }

    [Fact]
    public void A_nullable_enum_starts_with_nothing_pressed_and_can_be_cleared_again()
    {
        var bound = SingleFromEnum<Alignment?>(null);

        bound.Labels().ShouldBe(["Left", "Center aligned", "Right"]);
        bound.Pressed().ShouldBeEmpty();

        bound.Press("Left");
        bound.Model.ShouldBe(Alignment.Left);

        bound.Press("Left");
        bound.Model.ShouldBeNull();
    }

    [Fact]
    public void Items_given_for_an_enum_replace_the_generated_ones()
    {
        var bound = Single(Alignment.Left, ("Start", Alignment.Left), ("End", Alignment.Right));

        bound.Labels().ShouldBe(["Start", "End"]);
        bound.Pressed().ShouldBe(["Start"]);
    }

    [Fact]
    public void A_single_value_set_in_code_moves_the_pressed_item()
    {
        var bound = Single(10, ("Ten", 10), ("Twenty-five", 25));
        // Render first: the binding is created on first use.
        bound.Pressed().ShouldBe(["Ten"]);

        bound.Model = 25;
        bound.Rerender();

        bound.Pressed().ShouldBe(["Twenty-five"]);
    }

    // === Multiple selection ===

    [Fact]
    public void Multiple_ints_bind_both_ways()
    {
        var original = new List<int> { 10 };
        var bound = Multiple(original, ("Ten", 10), ("Twenty-five", 25), ("Fifty", 50));

        bound.Pressed().ShouldBe(["Ten"]);

        bound.Press("Fifty");

        bound.Models.ShouldBe([10, 50]);
        bound.Pressed().ShouldBe(["Ten", "Fifty"]);

        bound.Press("Ten");

        bound.Models.ShouldBe([50]);
        // The list the caller handed in is theirs. Each change reports a new one.
        original.ShouldBe([10]);
    }

    [Fact]
    public void Multiple_enums_bind_both_ways_with_generated_items()
    {
        var bound = new Bound<Alignment, Alignment>(
            this,
            ToggleGroupType.Multiple,
            default,
            [Alignment.Left, Alignment.Right],
            null
        );

        bound.Labels().ShouldBe(["Left", "Center aligned", "Right"]);
        bound.Pressed().ShouldBe(["Left", "Right"]);

        bound.Press("Center aligned");
        bound.Press("Left");

        bound.Models.ShouldBe([Alignment.Right, Alignment.CenterAligned]);
    }

    [Fact]
    public void Multiple_zero_is_a_value_like_any_other()
    {
        var bound = Multiple(new List<int> { 0 }, ("Zero", 0), ("One", 1));

        bound.Pressed().ShouldBe(["Zero"]);

        bound.Press("Zero");

        bound.Models.ShouldBeEmpty();
    }

    [Fact]
    public void Multiple_values_set_in_code_change_the_pressed_items()
    {
        var bound = Multiple(new List<int> { 10 }, ("Ten", 10), ("Twenty-five", 25));
        bound.Pressed().ShouldBe(["Ten"]);

        bound.Models = [25];
        bound.Rerender();

        bound.Pressed().ShouldBe(["Twenty-five"]);
    }

    [Fact]
    public void A_list_already_reported_is_not_changed_by_the_next_press()
    {
        // One-way use: the caller keeps what it was told and never binds it back.
        var reported = new List<List<int>>();
        var cut = Render<ToggleGroup<int>>(parameters =>
            parameters
                .Add(p => p.Type, ToggleGroupType.Multiple)
                .Add(p => p.Values, new List<int>())
                .Add(p => p.ValuesChanged, values => reported.Add(values))
                .Add<RenderFragment?>(
                    p => p.ChildContent,
                    builder =>
                    {
                        foreach (var number in new[] { 10, 25 })
                        {
                            builder.OpenComponent<ToggleGroupItem<int>>(0);
                            builder.AddAttribute(1, nameof(ToggleGroupItem<int>.Value), number);
                            builder.AddAttribute(
                                2,
                                nameof(ToggleGroupItem<int>.ChildContent),
                                (RenderFragment)(content => content.AddContent(0, number.ToString()))
                            );
                            builder.SetKey(number);
                            builder.CloseComponent();
                        }
                    }
                )
        );

        cut.FindAll(".vibe-toggle-group-item")[0].Click();
        cut.FindAll(".vibe-toggle-group-item")[1].Click();

        reported.Count.ShouldBe(2);
        reported[0].ShouldBe([10]);
        reported[1].ShouldBe([10, 25]);
    }

    [Fact]
    public void A_change_the_caller_makes_to_its_list_without_rendering_is_not_picked_up()
    {
        var original = new List<int> { 10 };
        var bound = Multiple(original, ("Ten", 10), ("Twenty-five", 25), ("Fifty", 50));
        bound.Pressed().ShouldBe(["Ten"]);

        // The group works from the values it was last given, not from a list it does not own.
        original.Add(25);
        bound.Press("Fifty");

        bound.Models.ShouldBe([10, 50]);
    }

    // === Items whose own type is not the group's ===

    [Fact]
    public void Bool_items_work_inside_a_nullable_bool_group()
    {
        var bound = new Bound<bool?, bool>(
            this,
            ToggleGroupType.Single,
            null,
            null,
            [("On", true), ("Off", false)]
        );

        bound.Pressed().ShouldBeEmpty();

        bound.Press("Off");

        bound.Model.ShouldBe(false);
        bound.Pressed().ShouldBe(["Off"]);
    }

    [Fact]
    public void Int_items_work_inside_a_multiple_group_of_long()
    {
        var bound = new Bound<long, int>(
            this,
            ToggleGroupType.Multiple,
            default,
            [5L],
            [("One", 1), ("Five", 5)]
        );

        bound.Pressed().ShouldBe(["Five"]);

        bound.Press("One");

        bound.Models.ShouldBe([5L, 1L]);
    }

    [Fact]
    public void An_item_whose_value_cannot_be_the_groups_type_fails_and_names_both()
    {
        var bound = new Bound<int, string>(
            this,
            ToggleGroupType.Single,
            1,
            null,
            [("Bad", "not a number")]
        );

        var exception = Should.Throw<InvalidOperationException>(() => bound.Pressed());

        exception.Message.ShouldContain("String");
        exception.Message.ShouldContain("not a number");
        exception.Message.ShouldContain("ToggleGroup<Int32>");
    }

    // === Edges ===

    [Fact]
    public void A_type_that_is_not_an_enum_renders_no_items_of_its_own()
    {
        var bound = SingleFromEnum(1);

        bound.Labels().ShouldBeEmpty();
    }

    [Fact]
    public void A_null_list_of_values_is_an_empty_selection()
    {
        var reported = new List<List<int>>();
        var cut = Render<ToggleGroup<int>>(parameters =>
            parameters
                .Add(p => p.Type, ToggleGroupType.Multiple)
                .Add(p => p.Values, null!)
                .Add(p => p.ValuesChanged, values => reported.Add(values))
                .AddChildContent<ToggleGroupItem<int>>(item => item.Add(i => i.Value, 10).AddChildContent("Ten"))
        );

        cut.FindAll(".vibe-toggle-group-item-pressed").ShouldBeEmpty();

        cut.Find(".vibe-toggle-group-item").Click();

        reported.Single().ShouldBe([10]);
    }

    [Fact]
    public void An_item_with_a_null_value_cannot_join_a_multiple_selection()
    {
        // A list of pressed values has no place for "nothing", so the item is inert.
        var bound = new Bound<int?, int?>(
            this,
            ToggleGroupType.Multiple,
            default,
            [5],
            [("None", null), ("Five", 5)]
        );

        bound.Pressed().ShouldBe(["Five"]);

        bound.Press("None");

        bound.Changes.ShouldBe(0);
        bound.Models.ShouldBe([5]);
        bound.Pressed().ShouldBe(["Five"]);
    }

    [Fact]
    public void Int_items_work_inside_a_nullable_long_group()
    {
        var bound = new Bound<long?, int>(this, ToggleGroupType.Single, 5L, null, [("One", 1), ("Five", 5)]);

        bound.Pressed().ShouldBe(["Five"]);

        bound.Press("One");

        bound.Model.ShouldBe(1L);
    }

    // === Disabled ===

    [Fact]
    public void A_disabled_group_reports_nothing()
    {
        var cut = Render<ToggleGroup<int>>(parameters =>
            parameters
                .Add(p => p.Value, 10)
                .Add(p => p.Disabled, true)
                .Add(p => p.ValueChanged, _ => throw new InvalidOperationException("reported"))
                .AddChildContent<ToggleGroupItem<int>>(item =>
                    item.Add(i => i.Value, 25).AddChildContent("Twenty-five")
                )
        );

        cut.Find(".vibe-toggle-group-item").HasAttribute("disabled").ShouldBeTrue();
    }
}
