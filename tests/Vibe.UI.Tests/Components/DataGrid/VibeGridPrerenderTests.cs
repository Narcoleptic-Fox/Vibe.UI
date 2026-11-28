using AngleSharp.Dom;
using Vibe.UI.Components;
using Part = Vibe.UI.Tests.Components.DataGrid.VibeGridFixture.Part;

namespace Vibe.UI.Tests.Components.DataGrid;

/// <summary>
/// B3.2: a page is prerendered on the server and then rendered again, interactively, by a new
/// component. The grid keeps the page it fetched the first time so that the second does not
/// fetch it again. These tests take the two halves apart: what the grid keeps when it is not
/// interactive, and what it does when it starts with a kept page. The compatibility tests put
/// the halves together in a real app.
/// </summary>
public class VibeGridPrerenderTests : GridTestBase
{
    private IRenderedComponent<VibeGridFixture> page = default!;

    private List<VibeGridRequest> Requests => page.Instance.Requests;

    private IReadOnlyList<IElement> DataRows() => page.FindAll("tbody tr[aria-rowindex]");

    private string FirstCell() => DataRows()[0].QuerySelector("td")!.TextContent.Trim();

    private void RenderPage(int pageSize = 10)
    {
        page = Render<VibeGridFixture>(p => p.Add(f => f.PageSize, pageSize));
        page.WaitForAssertion(() => DataRows().Count.ShouldBeGreaterThan(0));
    }

    // The interactive grid is a new object. The framework gives it the kept page before it starts.
    private void StartWith(VibeGridSnapshot<Part> kept) =>
        ComponentFactories.Add<VibeGrid<Part>>(() => new VibeGrid<Part> { PrerenderedPage = kept });

    private static VibeGridSnapshot<Part> KeptPage(int pageSize = 10, string? sortBy = null) =>
        new(
            StartIndex: 0,
            PageSize: pageSize,
            SortBy: sortBy,
            SortDescending: false,
            Page: new VibeGridPage<Part>(
                Enumerable.Range(1, pageSize).Select(i => new Part(i, $"Kept {i:000}", 0)).ToList(),
                95
            )
        );

    [Fact]
    public void While_prerendering_the_grid_keeps_the_page_it_fetched()
    {
        RenderAs(interactive: false);

        RenderPage();

        Requests.Count.ShouldBe(1);
        VibeGridSnapshot<Part> kept = page.Instance.Grid.PrerenderedPage.ShouldNotBeNull();
        kept.StartIndex.ShouldBe(0);
        kept.PageSize.ShouldBe(10);
        kept.SortBy.ShouldBeNull();
        kept.Page.TotalCount.ShouldBe(95);
        kept.Page.Items.Select(part => part.Name).First().ShouldBe("Part 001");
        kept.Page.Items.Count.ShouldBe(10);
    }

    [Fact]
    public void Once_interactive_the_grid_keeps_nothing()
    {
        RenderPage();

        // Kept only for the render that follows a prerender. Saved at any other moment it could
        // come back later, to a page whose filter is no longer the one it was fetched for.
        page.Instance.Grid.PrerenderedPage.ShouldBeNull();
    }

    [Fact]
    public void A_grid_that_starts_with_a_kept_page_shows_it_and_asks_nothing()
    {
        StartWith(KeptPage());

        RenderPage();
        page.Find("#poke").Click();

        Requests.ShouldBeEmpty();
        DataRows().Count.ShouldBe(10);
        FirstCell().ShouldBe("Kept 001");
        // The paginator is drawn from the kept total: 95 items, 10 to a page.
        page.FindAll(".vibe-grid-pager a, .vibe-grid-pager button")
            .ShouldContain(e => e.TextContent.Trim() == "10");
        page.Instance.Grid.PrerenderedPage.ShouldBeNull();
    }

    [Fact]
    public void After_a_kept_page_the_next_page_and_a_refresh_each_ask_once()
    {
        StartWith(KeptPage());
        RenderPage();

        page.FindAll(".vibe-grid-pager a, .vibe-grid-pager button")
            .First(e => e.TextContent.Trim() == "2")
            .Click();
        page.WaitForAssertion(() => FirstCell().ShouldBe("Part 011"));
        Requests.Count.ShouldBe(1);
        Requests[0].Page.ShouldBe(1);

        page.InvokeAsync(() => page.Instance.Grid.RefreshAsync(toFirstPage: true));
        page.WaitForAssertion(() => FirstCell().ShouldBe("Part 001"));
        Requests.Count.ShouldBe(2);
    }

    [Fact]
    public void A_kept_page_that_answers_another_question_is_not_used()
    {
        // Kept for 25 to a page; this page asks for 10.
        StartWith(KeptPage(pageSize: 25));

        RenderPage(pageSize: 10);

        Requests.Count.ShouldBe(1);
        DataRows().Count.ShouldBe(10);
        FirstCell().ShouldBe("Part 001");
    }

    [Fact]
    public void A_kept_page_in_another_order_is_not_used()
    {
        // Kept sorted by stock; this grid starts unsorted.
        StartWith(KeptPage(sortBy: nameof(Part.Stock)));

        RenderPage();

        Requests.Count.ShouldBe(1);
        Requests[0].SortBy.ShouldBeNull();
        FirstCell().ShouldBe("Part 001");
    }
}
