using Vibe.UI.Components;
using Loop = Vibe.UI.Tests.Components.DataGrid.VibeGridAwkwardRowsFixture.Loop;
using NotRebuildable = Vibe.UI.Tests.Components.DataGrid.VibeGridAwkwardRowsFixture.NotRebuildable;

namespace Vibe.UI.Tests.Components.DataGrid;

/// <summary>
/// The grid keeps its prerendered page as JSON. A row type that JSON cannot write, or can write
/// and cannot rebuild, must cost one more request and nothing else. Handed to the framework as
/// an object, the second kind ended the Interactive Server circuit: seen in a real app before
/// the grid took the writing and reading on itself.
/// </summary>
public class VibeGridAwkwardRowsTests : GridTestBase
{
    [Fact]
    public void Rows_JSON_cannot_write_are_shown_and_nothing_is_kept()
    {
        RenderAs(interactive: false);

        var page = Render<VibeGridAwkwardRowsFixture>(p =>
            p.Add(f => f.RowsReferToThemselves, true)
        );
        page.WaitForAssertion(() => page.FindAll("tbody tr[aria-rowindex]").Count.ShouldBe(5));

        page.Instance.Requests.ShouldBe(1);
        page.Instance.Looped!.PrerenderedPage.ShouldBeNull();
    }

    [Fact]
    public void Rows_JSON_can_write_are_kept_even_when_it_could_not_rebuild_them()
    {
        RenderAs(interactive: false);

        var page = Render<VibeGridAwkwardRowsFixture>();
        page.WaitForAssertion(() => page.FindAll("tbody tr[aria-rowindex]").Count.ShouldBe(5));

        // Writing succeeds, so something is kept. It is the reading that has to cope.
        page.Instance.Closed!.PrerenderedPage.ShouldNotBeNull().ShouldContain("Closed 1");
    }

    [Fact]
    public void A_kept_page_whose_rows_cannot_be_rebuilt_is_dropped_and_the_grid_fetches()
    {
        // What the prerender of this very page keeps, as the test above shows.
        const string kept =
            "{\"StartIndex\":0,\"PageSize\":5,\"SortBy\":null,\"SortDescending\":false,"
            + "\"Page\":{\"Items\":[{\"Id\":1,\"Name\":\"Closed 1\"}],\"TotalCount\":40}}";
        ComponentFactories.Add<VibeGrid<NotRebuildable>>(() =>
            new VibeGrid<NotRebuildable> { PrerenderedPage = kept }
        );

        var page = Render<VibeGridAwkwardRowsFixture>();
        page.WaitForAssertion(() => page.FindAll("tbody tr[aria-rowindex]").Count.ShouldBe(5));

        page.Instance.Requests.ShouldBe(1);
        page.Find("tbody tr[aria-rowindex] td").TextContent.Trim().ShouldBe("Closed 1");
        page.Instance.Closed!.PrerenderedPage.ShouldBeNull();
    }
}
