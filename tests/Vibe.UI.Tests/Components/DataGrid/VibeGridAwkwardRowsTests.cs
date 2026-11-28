using Kind = Vibe.UI.Tests.Components.DataGrid.VibeGridAwkwardRowsFixture.Kind;
using Loop = Vibe.UI.Tests.Components.DataGrid.VibeGridAwkwardRowsFixture.Loop;
using Lossy = Vibe.UI.Tests.Components.DataGrid.VibeGridAwkwardRowsFixture.Lossy;
using WithCallback = Vibe.UI.Tests.Components.DataGrid.VibeGridAwkwardRowsFixture.WithCallback;
using NotRebuildable = Vibe.UI.Tests.Components.DataGrid.VibeGridAwkwardRowsFixture.NotRebuildable;

namespace Vibe.UI.Tests.Components.DataGrid;

/// <summary>
/// The grid keeps its prerendered page as JSON. A row type that JSON cannot write, can write and
/// cannot rebuild, or rebuilds with values missing, must cost one more request and nothing else.
/// Handed to the framework as an object, the second kind ended the Interactive Server circuit:
/// seen in a real app before the grid took the writing and reading on itself.
/// </summary>
public class VibeGridAwkwardRowsTests : GridTestBase
{
    private IRenderedComponent<VibeGridAwkwardRowsFixture> page = default!;

    private void Prerender(Kind rows)
    {
        RenderAs(interactive: false);
        RenderRows(rows, p => p.SetAssignedRenderMode(Interactive));
    }

    private void RenderRows(
        Kind rows,
        Action<ComponentParameterCollectionBuilder<VibeGridAwkwardRowsFixture>>? more = null
    )
    {
        page = Render<VibeGridAwkwardRowsFixture>(p =>
        {
            p.Add(f => f.Rows, rows);
            more?.Invoke(p);
        });
        page.WaitForAssertion(() => page.FindAll("tbody tr[aria-rowindex]").Count.ShouldBe(5));
    }

    private string FirstCell() => page.Find("tbody tr[aria-rowindex] td").TextContent.Trim();

    [Fact]
    public void Rows_JSON_cannot_write_are_shown_and_nothing_is_kept()
    {
        Prerender(Kind.ReferToThemselves);

        page.Instance.Requests.ShouldBe(1);
        FirstCell().ShouldBe("Loop 1");
        KeptAfterPrerender<Loop>().ShouldBeNull();
    }

    [Fact]
    public void Rows_that_carry_a_delegate_are_shown_and_nothing_is_kept()
    {
        // Refused in another way than a row that refers to itself: not supported, where that
        // one is a cycle found while writing.
        Prerender(Kind.CarryACallback);

        page.Instance.Requests.ShouldBe(1);
        FirstCell().ShouldBe("Callback 1");
        KeptAfterPrerender<WithCallback>().ShouldBeNull();
    }

    [Fact]
    public void Rows_JSON_can_write_are_kept_even_when_it_could_not_rebuild_them()
    {
        Prerender(Kind.CannotBeRebuilt);

        // Writing succeeds, so something is kept. It is the reading that has to cope.
        KeptAfterPrerender<NotRebuildable>().ShouldNotBeNull().ShouldContain("Closed 1");
    }

    [Fact]
    public void A_kept_page_whose_rows_cannot_be_rebuilt_is_dropped_and_the_grid_fetches()
    {
        Prerender(Kind.CannotBeRebuilt);
        string kept = KeptAfterPrerender<NotRebuildable>().ShouldNotBeNull();

        // The interactive render that follows, handed exactly what the prerender kept.
        KeptState.Persist(KeptPageName<NotRebuildable>(), kept);
        RenderAs(interactive: true);
        RenderRows(Kind.CannotBeRebuilt);

        page.Instance.Requests.ShouldBe(1);
        FirstCell().ShouldBe("Closed 1");
    }

    [Fact]
    public void A_kept_page_whose_rows_come_back_with_values_missing_is_dropped_and_the_grid_fetches()
    {
        Prerender(Kind.ComeBackEmpty);
        string kept = KeptAfterPrerender<Lossy>().ShouldNotBeNull();
        kept.ShouldContain("Lossy 1");

        KeptState.Persist(KeptPageName<Lossy>(), kept);
        RenderAs(interactive: true);
        RenderRows(Kind.ComeBackEmpty);

        // Used as it came back, the rows would be five blank names and no request.
        page.Instance.Requests.ShouldBe(1);
        FirstCell().ShouldBe("Lossy 1");
    }
}
