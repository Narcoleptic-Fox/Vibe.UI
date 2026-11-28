namespace Vibe.UI.Tests.Components.DataGrid;

/// <summary>
/// The measurement <c>VibeGrid</c> rests on, kept as a test so that it stays true or says when it
/// stops being true. QuickGrid notes the state of its pagination before its provider answers, and
/// that state includes the total, which it sets from the answer. So after any answer that changes
/// the total, the next render of the page makes it ask again for the page it already has.
/// </summary>
public class RawQuickGridTests : TestBase
{
    [Fact]
    public void With_a_paginator_that_renders_with_the_page_QuickGrid_asks_twice_on_load()
    {
        var page = Render<RawQuickGridFixture>(p =>
            p.Add(f => f.PaginatorRendersWithThePage, true)
        );
        page.WaitForAssertion(() => page.FindAll("tbody tr[aria-rowindex]").Count.ShouldBe(10));

        page.Instance.Requests.ShouldBe(2);

        page.Find("#poke").Click();
        page.Instance.Requests.ShouldBe(2);
    }

    [Fact]
    public void With_nothing_rendering_the_page_QuickGrid_asks_once_on_load_and_once_more_at_the_next_render()
    {
        var page = Render<RawQuickGridFixture>();
        page.WaitForAssertion(() => page.FindAll("tbody tr[aria-rowindex]").Count.ShouldBe(10));

        page.Instance.Requests.ShouldBe(1);

        // Nothing about the data changed. The page rendered, and that was enough.
        page.Find("#poke").Click();
        page.WaitForAssertion(() => page.Instance.Requests.ShouldBe(2));

        page.Find("#poke").Click();
        page.Instance.Requests.ShouldBe(2);
    }
}
