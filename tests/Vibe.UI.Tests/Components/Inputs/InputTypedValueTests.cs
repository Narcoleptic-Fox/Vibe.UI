using System.Globalization;

namespace Vibe.UI.Tests.Components.Inputs;

/// <summary>
/// Charter rule 2 for <c>Input</c>: numbers, dates and times bind without a converter, both ways.
/// Unlike <c>Select</c>, the text is typed by the user, so text that does not convert must never
/// throw: it stays in the box, nothing is reported, and the input is marked invalid.
/// </summary>
public class InputTypedValueTests : TestBase
{
    /// <summary>A two-way binding with a parent that can re-render.</summary>
    private sealed class Bound<TValue>(
        InputTypedValueTests test,
        TValue? initial,
        string? type = null
    )
    {
        public TValue? Model = initial;
        public int Changes;
        private IRenderedComponent<Input<TValue>>? cut;

        public IRenderedComponent<Input<TValue>> Cut =>
            cut ??= test.Render<Input<TValue>>(Parameters);

        public AngleSharp.Dom.IElement Element => Cut.Find("input");

        public string? Text => Element.GetAttribute("value");

        /// <summary>The parent renders again with whatever the model now holds.</summary>
        public void Rerender() => Cut.Render(Parameters);

        private void Parameters(ComponentParameterCollectionBuilder<Input<TValue>> parameters)
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
            if (type is not null)
            {
                parameters.Add(p => p.Type, type);
            }
        }
    }

    private Bound<TValue> Bind<TValue>(TValue? initial, string? type = null) =>
        new(this, initial, type);

    private static void UseCulture(string name)
    {
        CultureInfo.CurrentCulture = new CultureInfo(name);
        CultureInfo.CurrentUICulture = new CultureInfo(name);
    }

    // === Both directions, per type ===

    [Fact]
    public void Int_binds_both_ways_as_a_number_input()
    {
        var bound = Bind<int>(25);

        bound.Element.GetAttribute("type").ShouldBe("number");
        bound.Text.ShouldBe("25");
        bound.Element.HasAttribute("step").ShouldBeFalse();

        bound.Element.Change("10");

        bound.Model.ShouldBe(10);
    }

    [Fact]
    public void Nullable_int_binds_both_ways_and_an_empty_box_is_null()
    {
        var bound = Bind<int?>(10);

        bound.Text.ShouldBe("10");
        bound.Element.Change("25");
        bound.Model.ShouldBe(25);

        bound.Element.Change("");

        bound.Model.ShouldBeNull();
        bound.Element.HasAttribute("aria-invalid").ShouldBeFalse();
    }

    [Fact]
    public void Decimal_uses_the_invariant_format_under_a_german_culture()
    {
        UseCulture("de-DE");
        var bound = Bind<decimal>(1.5m);

        // The browser's number input reads and writes "1.5" whatever the user's locale shows.
        bound.Text.ShouldBe("1.5");
        bound.Element.GetAttribute("type").ShouldBe("number");
        bound.Element.GetAttribute("step").ShouldBe("any");

        bound.Element.Change("2.25");

        bound.Model.ShouldBe(2.25m);
    }

    [Fact]
    public void Double_binds_both_ways()
    {
        var bound = Bind<double>(0.5);

        bound.Text.ShouldBe("0.5");
        bound.Element.Change("-3.75");

        bound.Model.ShouldBe(-3.75);
    }

    [Fact]
    public void DateOnly_binds_both_ways_as_a_date_input()
    {
        UseCulture("de-DE");
        var bound = Bind<DateOnly>(new DateOnly(2026, 11, 25));

        bound.Element.GetAttribute("type").ShouldBe("date");
        bound.Text.ShouldBe("2026-11-25");

        bound.Element.Change("2026-12-01");

        bound.Model.ShouldBe(new DateOnly(2026, 12, 1));
    }

    [Fact]
    public void Nullable_DateOnly_can_be_cleared()
    {
        var bound = Bind<DateOnly?>(new DateOnly(2026, 11, 25));

        bound.Text.ShouldBe("2026-11-25");
        bound.Element.Change("");

        bound.Model.ShouldBeNull();
        bound.Changes.ShouldBe(1);
    }

    [Fact]
    public void Every_nullable_value_type_clears_to_null()
    {
        var dateTime = Bind<System.DateTime?>(new System.DateTime(2026, 11, 25, 14, 30, 0));
        var time = Bind<TimeOnly?>(new TimeOnly(14, 30));
        var number = Bind<decimal?>(1.5m);
        var id = Bind<Guid?>(Guid.NewGuid());

        dateTime.Element.Change("");
        time.Element.Change("");
        number.Element.Change("");
        id.Element.Change("");

        dateTime.Model.ShouldBeNull();
        time.Model.ShouldBeNull();
        number.Model.ShouldBeNull();
        id.Model.ShouldBeNull();
        new[] { dateTime.Changes, time.Changes, number.Changes, id.Changes }.ShouldAllBe(c =>
            c == 1
        );
    }

    [Fact]
    public void DateTime_binds_both_ways_as_a_local_date_and_time_input()
    {
        UseCulture("de-DE");
        var bound = Bind<System.DateTime>(new System.DateTime(2026, 11, 25, 14, 30, 0));

        bound.Element.GetAttribute("type").ShouldBe("datetime-local");
        bound.Text.ShouldBe("2026-11-25T14:30:00");

        // Browsers leave the seconds off when they are zero.
        bound.Element.Change("2026-12-01T08:15");

        bound.Model.ShouldBe(new System.DateTime(2026, 12, 1, 8, 15, 0));
    }

    [Fact]
    public void TimeOnly_binds_both_ways_as_a_time_input()
    {
        UseCulture("de-DE");
        var bound = Bind<TimeOnly>(new TimeOnly(14, 30));

        bound.Element.GetAttribute("type").ShouldBe("time");
        bound.Text.ShouldBe("14:30:00");

        bound.Element.Change("08:15");

        bound.Model.ShouldBe(new TimeOnly(8, 15));
    }

    [Fact]
    public void Guid_binds_both_ways_as_text()
    {
        var first = new Guid("11111111-1111-1111-1111-111111111111");
        var second = new Guid("22222222-2222-2222-2222-222222222222");
        var bound = Bind<Guid>(first);

        bound.Element.GetAttribute("type").ShouldBe("text");
        bound.Text.ShouldBe(first.ToString());

        bound.Element.Change(second.ToString());

        bound.Model.ShouldBe(second);
    }

    [Fact]
    public void String_stays_a_text_input_and_an_empty_box_is_an_empty_string()
    {
        var bound = Bind<string>("hello");

        bound.Element.GetAttribute("type").ShouldBe("text");
        bound.Text.ShouldBe("hello");

        bound.Element.Change("");

        bound.Model.ShouldBe(string.Empty);
        bound.Element.HasAttribute("aria-invalid").ShouldBeFalse();
    }

    // === The input type ===

    [Fact]
    public void An_explicit_type_wins_over_the_one_the_value_type_suggests()
    {
        var bound = Bind<int>(5, type: "text");

        bound.Element.GetAttribute("type").ShouldBe("text");
        bound.Text.ShouldBe("5");
    }

    [Fact]
    public void A_date_typed_DateTime_is_formatted_as_a_date()
    {
        var bound = Bind<System.DateTime>(
            new System.DateTime(2026, 11, 25, 14, 30, 0),
            type: "date"
        );

        bound.Text.ShouldBe("2026-11-25");
    }

    [Fact]
    public void A_lowercase_type_attribute_decides_the_format_too()
    {
        // The date pickers pass type this way. The framework binds it to Type whatever its case.
        var cut = Render<Input<System.DateTime>>(parameters =>
            parameters
                .Add(p => p.Value, new System.DateTime(2026, 11, 25, 14, 30, 0))
                .AddUnmatched("type", "date")
        );

        cut.Find("input").GetAttribute("type").ShouldBe("date");
        cut.Find("input").GetAttribute("value").ShouldBe("2026-11-25");
    }

    [Fact]
    public void A_step_the_caller_gives_is_kept()
    {
        var cut = Render<Input<decimal>>(parameters =>
            parameters.Add(p => p.Value, 1.5m).AddUnmatched("step", "0.01")
        );

        cut.Find("input").GetAttribute("step").ShouldBe("0.01");
    }

    // === Text that does not convert ===

    [Fact]
    public void Committed_text_that_does_not_convert_is_kept_marked_invalid_and_not_reported()
    {
        var bound = Bind<int>(5);

        Should.NotThrow(() => bound.Element.Change("abc"));

        bound.Model.ShouldBe(5);
        bound.Changes.ShouldBe(0);
        bound.Text.ShouldBe("abc");
        bound.Element.GetAttribute("aria-invalid").ShouldBe("true");
        bound.Cut.Find(".vibe-input-container").ClassList.ShouldContain("vibe-input-has-error");

        bound.Element.Change("7");

        bound.Model.ShouldBe(7);
        bound.Element.HasAttribute("aria-invalid").ShouldBeFalse();
    }

    [Fact]
    public void Half_typed_text_is_not_an_error_until_it_is_committed()
    {
        var bound = Bind<int>(5);

        bound.Element.Input("-");

        bound.Changes.ShouldBe(0);
        bound.Text.ShouldBe("-");
        bound.Element.HasAttribute("aria-invalid").ShouldBeFalse();

        bound.Element.Change("-");

        bound.Element.GetAttribute("aria-invalid").ShouldBe("true");
    }

    [Fact]
    public void An_empty_box_on_a_type_that_cannot_be_null_is_invalid_and_keeps_the_value()
    {
        var bound = Bind<int>(5);

        bound.Element.Change("");

        bound.Model.ShouldBe(5);
        bound.Changes.ShouldBe(0);
        bound.Text.ShouldBe("");
        bound.Element.GetAttribute("aria-invalid").ShouldBe("true");
    }

    [Fact]
    public void The_parsing_message_is_shown_and_announced_while_the_text_does_not_convert()
    {
        var cut = Render<Input<int>>(parameters =>
            parameters.Add(p => p.Value, 5).Add(p => p.ParsingErrorMessage, "Enter a whole number")
        );

        cut.FindAll(".vibe-input-error").ShouldBeEmpty();

        cut.Find("input").Change("abc");

        var error = cut.Find(".vibe-input-error");
        error.TextContent.ShouldBe("Enter a whole number");
        cut.Find("input").GetAttribute("aria-errormessage").ShouldBe(error.Id);
    }

    [Fact]
    public void Without_a_message_of_its_own_an_invalid_box_still_says_what_is_wrong()
    {
        var number = Bind<int>(5);
        var date = Bind<DateOnly>(new DateOnly(2026, 11, 25));
        var time = Bind<TimeOnly>(new TimeOnly(9, 30));
        var id = Bind<Guid>(Guid.NewGuid());

        number.Element.Change("abc");
        date.Element.Change("abc");
        time.Element.Change("abc");
        id.Element.Change("abc");

        number.Cut.Find(".vibe-input-error").TextContent.ShouldBe("Enter a valid number.");
        date.Cut.Find(".vibe-input-error").TextContent.ShouldBe("Enter a valid date.");
        time.Cut.Find(".vibe-input-error").TextContent.ShouldBe("Enter a valid time.");
        id.Cut.Find(".vibe-input-error").TextContent.ShouldBe("Enter a valid value.");
    }

    [Fact]
    public void An_empty_parsing_message_shows_no_text_but_still_marks_the_box()
    {
        var cut = Render<Input<int>>(parameters =>
            parameters.Add(p => p.Value, 5).Add(p => p.ParsingErrorMessage, string.Empty)
        );

        cut.Find("input").Change("abc");

        cut.FindAll(".vibe-input-error").ShouldBeEmpty();
        cut.Find("input").GetAttribute("aria-invalid").ShouldBe("true");
    }

    [Fact]
    public void DateTimeOffset_binds_both_ways_as_a_local_date_and_time_input()
    {
        var bound = Bind<DateTimeOffset>(
            new DateTimeOffset(2026, 11, 25, 14, 30, 0, TimeSpan.Zero)
        );

        bound.Element.GetAttribute("type").ShouldBe("datetime-local");
        bound.Text.ShouldBe("2026-11-25T14:30:00");

        bound.Element.Change("2026-12-01T08:15");

        bound.Model.DateTime.ShouldBe(new System.DateTime(2026, 12, 1, 8, 15, 0));
    }

    [Fact]
    public void A_month_typed_DateTime_binds_both_ways()
    {
        var bound = Bind<System.DateTime>(new System.DateTime(2026, 11, 25), type: "month");

        bound.Text.ShouldBe("2026-11");

        bound.Element.Change("2026-12");

        bound.Model.ShouldBe(new System.DateTime(2026, 12, 1));
    }

    [Fact]
    public void A_comma_decimal_typed_into_a_text_box_is_refused_not_read_as_fifteen()
    {
        UseCulture("de-DE");
        var bound = Bind<decimal>(2m, type: "text");

        bound.Element.Change("1,5");

        bound.Model.ShouldBe(2m);
        bound.Changes.ShouldBe(0);
        bound.Element.GetAttribute("aria-invalid").ShouldBe("true");
    }

    // === Typing survives the parent rendering again ===

    [Fact]
    public void Text_that_stands_for_the_value_is_not_rewritten_by_a_rerender()
    {
        var bound = Bind<decimal>(0m);

        bound.Element.Input("1.50");
        bound.Model.ShouldBe(1.5m);

        bound.Rerender();

        // Rewriting it to "1.5" would eat the zero the user just typed.
        bound.Text.ShouldBe("1.50");
    }

    [Fact]
    public void Text_that_does_not_convert_survives_a_rerender_that_did_not_change_the_value()
    {
        var bound = Bind<int>(5);

        bound.Element.Input("");
        bound.Rerender();

        bound.Text.ShouldBe("");
        bound.Model.ShouldBe(5);
    }

    [Fact]
    public void A_value_set_in_code_replaces_the_text()
    {
        var bound = Bind<decimal>(0m);
        bound.Element.Input("1.50");

        bound.Model = 7m;
        bound.Rerender();

        bound.Text.ShouldBe("7");
    }

    [Fact]
    public void A_value_set_in_code_replaces_text_that_does_not_convert_and_clears_the_error()
    {
        var bound = Bind<int>(5);
        bound.Element.Change("abc");

        bound.Model = 9;
        bound.Rerender();

        bound.Text.ShouldBe("9");
        bound.Element.HasAttribute("aria-invalid").ShouldBeFalse();
    }
}
