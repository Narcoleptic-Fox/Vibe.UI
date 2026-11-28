using Row = Vibe.UI.Tests.Components.DataGrid.VibeGridPairFixture.Row;

namespace Vibe.UI.Tests.Components.DataGrid;

/// <summary>
/// Two grids of one row type keep their prerendered pages under one name unless their keys
/// differ. The framework refuses a name already in use, and its refusal costs the page
/// everything kept on it, other grids' pages included. So a name used twice keeps nothing, each
/// of the two fetches again, and nothing else on the page is touched.
/// </summary>
public class VibeGridPairTests : GridTestBase
{
    private IRenderedComponent<VibeGridPairFixture> page = default!;

    private void RenderPair(string? firstKey, string? secondKey, bool prerender)
    {
        RenderAs(interactive: !prerender);
        page = Render<VibeGridPairFixture>(p =>
        {
            p.Add(f => f.FirstKey, firstKey).Add(f => f.SecondKey, secondKey);
            if (prerender)
            {
                p.SetAssignedRenderMode(Interactive);
            }
        });
        page.WaitForAssertion(() => page.FindAll("tbody tr[aria-rowindex]").Count.ShouldBe(10));
    }

    private string FirstCellOf(string id) =>
        page.Find($"#{id} tbody tr[aria-rowindex] td").TextContent.Trim();

    [Theory]
    [InlineData(null)]
    [InlineData("list")]
    public void Two_grids_under_one_name_keep_nothing_and_cost_another_grid_nothing(string? sharedKey)
    {
        RenderPair(firstKey: sharedKey, secondKey: sharedKey, prerender: true);
        var other = Render<VibeGridFixture>(p => p.SetAssignedRenderMode(Interactive));
        other.WaitForAssertion(() => other.FindAll("tbody tr[aria-rowindex]").Count.ShouldBe(10));

        Should.NotThrow(() => KeptState.TriggerOnPersisting());

        KeptState.TryTake(KeptPageName<Row>(sharedKey), out string? _).ShouldBeFalse();
        KeptState.TryTake(KeptPageName<VibeGridFixture.Part>(), out string? others).ShouldBeTrue();
        others.ShouldNotBeNull().ShouldContain("Part 001");
    }

    [Fact]
    public void A_grid_taken_off_the_page_keeps_nothing_and_frees_its_name()
    {
        RenderPair(firstKey: null, secondKey: null, prerender: true);

        page.Render(p => p.Add(f => f.WithoutFirst, true));
        page.WaitForAssertion(() => page.FindAll("tbody tr[aria-rowindex]").Count.ShouldBe(5));

        // One grid is left under the name, so its page is kept, and it is its own.
        string kept = KeptAfterPrerender<Row>().ShouldNotBeNull();
        kept.ShouldContain("Second 1");
        kept.ShouldNotContain("First 1");
    }

    [Fact]
    public void A_grid_taken_off_the_page_does_not_leave_its_page_behind()
    {
        RenderPair(firstKey: "first", secondKey: "second", prerender: true);

        page.Render(p => p.Add(f => f.WithoutFirst, true));
        page.WaitForAssertion(() => page.FindAll("tbody tr[aria-rowindex]").Count.ShouldBe(5));

        KeptState.TriggerOnPersisting();
        KeptState.TryTake(KeptPageName<Row>("first"), out string? _).ShouldBeFalse();
        KeptState.TryTake(KeptPageName<Row>("second"), out string? _).ShouldBeTrue();
    }

    [Fact]
    public void Two_grids_with_different_keys_each_keep_their_own_page()
    {
        RenderPair(firstKey: "first", secondKey: "second", prerender: true);

        KeptState.TriggerOnPersisting();
        KeptState.TryTake(KeptPageName<Row>("first"), out string? first).ShouldBeTrue();
        KeptState.TryTake(KeptPageName<Row>("second"), out string? second).ShouldBeTrue();
        first.ShouldNotBeNull().ShouldContain("First 1");
        first.ShouldNotContain("Second 1");
        second.ShouldNotBeNull().ShouldContain("Second 1");
    }

    [Fact]
    public void One_key_is_enough_to_tell_two_grids_apart()
    {
        RenderPair(firstKey: null, secondKey: "second", prerender: true);

        KeptState.TriggerOnPersisting();
        KeptState.TryTake(KeptPageName<Row>(), out string? first).ShouldBeTrue();
        KeptState.TryTake(KeptPageName<Row>("second"), out string? second).ShouldBeTrue();
        first.ShouldNotBeNull().ShouldContain("First 1");
        second.ShouldNotBeNull().ShouldContain("Second 1");
    }

    [Fact]
    public void Each_of_two_keyed_grids_starts_with_its_own_kept_page()
    {
        RenderPair(firstKey: "first", secondKey: "second", prerender: true);
        KeptState.TriggerOnPersisting();
        KeptState.TryTake(KeptPageName<Row>("first"), out string? first).ShouldBeTrue();
        KeptState.TryTake(KeptPageName<Row>("second"), out string? second).ShouldBeTrue();
        int asked = page.Instance.FirstRequests + page.Instance.SecondRequests;
        asked.ShouldBe(2);

        KeptState.Persist(KeptPageName<Row>("first"), first);
        KeptState.Persist(KeptPageName<Row>("second"), second);
        RenderPair(firstKey: "first", secondKey: "second", prerender: false);

        page.Instance.FirstRequests.ShouldBe(0);
        page.Instance.SecondRequests.ShouldBe(0);
        FirstCellOf("first").ShouldBe("First 1");
        FirstCellOf("second").ShouldBe("Second 1");
    }
}
