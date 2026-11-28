using AngleSharp.Dom;
using Vibe.UI.Components;
using Part = Vibe.UI.Tests.Components.DataGrid.VibeGridFixture.Part;

namespace Vibe.UI.Tests.Components.DataGrid;

/// <summary>
/// B3.1: <c>VibeGrid</c> is QuickGrid with the pagination state, the paginator and the theme owned
/// for the page, and with one request to the provider per page shown. Raw QuickGrid asks twice
/// after any answer that changes the total; these tests count.
/// </summary>
public class VibeGridTests : TestBase
{
    private IRenderedComponent<VibeGridFixture> page = default!;

    private List<VibeGridRequest> Requests => page.Instance.Requests;

    private void RenderPage(int pageSize = 10)
    {
        page = Render<VibeGridFixture>(p => p.Add(f => f.PageSize, pageSize));
        page.WaitForAssertion(() => DataRows().Count.ShouldBeGreaterThan(0));
    }

    // QuickGrid pads a short last page with rows that carry no aria-rowindex.
    private IReadOnlyList<IElement> DataRows() => page.FindAll("tbody tr[aria-rowindex]");

    private string FirstCell() => DataRows()[0].QuerySelector("td")!.TextContent.Trim();

    private IElement PageLink(string label) =>
        page.FindAll(".vibe-grid-pager a, .vibe-grid-pager button")
            .First(e => e.TextContent.Trim() == label);

    private IElement SortButton(string title) =>
        page.FindAll("thead button.col-title").First(b => b.TextContent.Contains(title));

    [Fact]
    public void Loading_the_page_asks_the_provider_once()
    {
        RenderPage();

        Requests.Count.ShouldBe(1);
        Requests[0].Page.ShouldBe(0);
        Requests[0].PageSize.ShouldBe(10);
        Requests[0].SortBy.ShouldBeNull();
        DataRows().Count.ShouldBe(10);
        FirstCell().ShouldBe("Part 001");
    }

    [Fact]
    public void The_page_rendering_again_around_the_grid_asks_nothing_more()
    {
        RenderPage();

        page.Find("#poke").Click();
        page.Find("#poke").Click();

        page.Find("#poke").TextContent.ShouldBe("Poked 2");
        Requests.Count.ShouldBe(1);
    }

    [Fact]
    public void Going_to_another_page_asks_once_for_that_page()
    {
        RenderPage();

        PageLink("2").Click();
        page.WaitForAssertion(() => FirstCell().ShouldBe("Part 011"));
        page.Find("#poke").Click();

        Requests.Count.ShouldBe(2);
        Requests[1].Page.ShouldBe(1);
        Requests[1].PageSize.ShouldBe(10);
    }

    [Fact]
    public void Sorting_asks_once_and_names_the_property_and_the_direction()
    {
        RenderPage();

        SortButton("Stock").Click();
        page.WaitForAssertion(() => Requests.Count.ShouldBe(2));
        page.Find("#poke").Click();

        Requests.Count.ShouldBe(2);
        Requests[1].SortBy.ShouldBe(nameof(Part.Stock));
        Requests[1].SortDescending.ShouldBeFalse();

        SortButton("Stock").Click();
        page.WaitForAssertion(() => Requests.Count.ShouldBe(3));
        Requests[2].SortBy.ShouldBe(nameof(Part.Stock));
        Requests[2].SortDescending.ShouldBeTrue();
    }

    [Fact]
    public async Task A_changed_filter_followed_by_a_refresh_asks_once_and_shows_the_new_rows()
    {
        RenderPage();

        // "Part 09" matches 090 to 095: the rows change and so does the total.
        page.Instance.Filter = "Part 09";
        await page.InvokeAsync(() => page.Instance.Grid.RefreshAsync(toFirstPage: true));
        page.WaitForAssertion(() => FirstCell().ShouldBe("Part 090"));
        page.Find("#poke").Click();

        Requests.Count.ShouldBe(2);
        DataRows().Count.ShouldBe(6);
    }

    [Fact]
    public async Task A_refresh_from_a_later_page_goes_back_to_the_first_page_in_one_request()
    {
        RenderPage();
        PageLink("3").Click();
        page.WaitForAssertion(() => FirstCell().ShouldBe("Part 021"));

        page.Instance.Filter = "Part 09";
        await page.InvokeAsync(() => page.Instance.Grid.RefreshAsync(toFirstPage: true));
        page.WaitForAssertion(() => FirstCell().ShouldBe("Part 090"));
        page.Find("#poke").Click();

        Requests.Count.ShouldBe(3);
        Requests[2].Page.ShouldBe(0);
    }

    [Fact]
    public async Task A_refresh_asks_again_even_when_nothing_about_the_question_changed()
    {
        RenderPage();

        // A row was deleted elsewhere: same page, same sort, and the answer may differ.
        await page.InvokeAsync(() => page.Instance.Grid.RefreshAsync());

        Requests.Count.ShouldBe(2);
        Requests[1].Page.ShouldBe(Requests[0].Page);
        Requests[1].SortBy.ShouldBe(Requests[0].SortBy);
    }

    [Fact]
    public void The_page_size_is_the_one_the_page_asked_for()
    {
        RenderPage(pageSize: 25);

        Requests.Count.ShouldBe(1);
        Requests[0].PageSize.ShouldBe(25);
        DataRows().Count.ShouldBe(25);
    }

    [Fact]
    public void The_paginator_is_drawn_from_the_total_and_marks_the_current_page()
    {
        RenderPage();

        // 95 items, 10 to a page.
        page.Find(".vibe-grid-pager nav").GetAttribute("aria-label").ShouldBe("Pages");
        PageLink("10").ShouldNotBeNull();
        page.Find(".vibe-grid-pager [aria-current=\"page\"]").TextContent.Trim().ShouldBe("1");

        PageLink("2").Click();
        page.WaitForAssertion(() =>
            page.Find(".vibe-grid-pager [aria-current=\"page\"]").TextContent.Trim().ShouldBe("2")
        );
    }

    [Fact]
    public void The_grid_asks_for_the_vibe_theme_inside_its_own_root()
    {
        RenderPage();

        page.Find(".vibe-grid > table.quickgrid").GetAttribute("theme").ShouldBe("vibe");
    }
}
