using Microsoft.Playwright;
using Shouldly;
using Vibe.UI.Docs.E2E.Infrastructure;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Functional;

/// <summary>
/// The VibeGrid page's preview in a real browser, in WebAssembly. The page counts every call to
/// its provider and prints the count, so "one request for each page shown" is read off the page.
/// What a stylesheet does to QuickGrid's markup can only be seen here: the component tests read
/// the stylesheet, they do not apply it.
/// </summary>
[Trait("Category", TestCategories.Functional)]
public class VibeGridPageTests : E2ETestBase
{
    private const string Route = "/components/vibegrid";

    private ILocator Requests => Page.GetByTestId("grid-requests");

    private ILocator DataRows => Page.Locator(".vibe-grid tbody tr[aria-rowindex]");

    private ILocator FirstCell => DataRows.First.Locator("td").First;

    private ILocator Header(string title) =>
        Page.Locator(".vibe-grid thead button.col-title", new() { HasTextString = title });

    private async Task OpenAsync()
    {
        await NavigateAndWaitForBlazorAsync(Route);
        await Assertions.Expect(DataRows).ToHaveCountAsync(10);
    }

    [Fact]
    public async Task LoadingThePageAsksTheProviderOnce()
    {
        await OpenAsync();

        await Assertions.Expect(FirstCell).ToHaveTextAsync("Part 001");
        await Assertions.Expect(Requests).ToHaveTextAsync("Requests to the provider: 1");
    }

    [Fact]
    public async Task APageChangeASortAndAFilterEachAskOnce()
    {
        await OpenAsync();

        await Page.Locator(".vibe-grid-pager").GetByText("2", new() { Exact = true }).ClickAsync();
        await Assertions.Expect(FirstCell).ToHaveTextAsync("Part 011");
        await Assertions.Expect(Requests).ToHaveTextAsync("Requests to the provider: 2");

        await Header("Stock").ClickAsync();
        await Assertions.Expect(Requests).ToHaveTextAsync("Requests to the provider: 3");
        await Assertions
            .Expect(Page.Locator(".vibe-grid thead th", new() { HasTextString = "Stock" }))
            .ToHaveAttributeAsync("aria-sort", "ascending");

        // "Part 09" matches 090 to 095. The count is read after typing and again after leaving
        // the field, because a text field reports its value at both moments.
        var filter = Page.Locator("#grid-filter");
        await filter.FillAsync("Part 09");
        await Assertions.Expect(DataRows).ToHaveCountAsync(6);
        await Assertions.Expect(Requests).ToHaveTextAsync("Requests to the provider: 4");
        await filter.PressAsync("Tab");
        await Page.WaitForTimeoutAsync(500);
        await Assertions.Expect(Requests).ToHaveTextAsync("Requests to the provider: 4");

        // Six rows are one page: the paginator has nothing to offer and is not drawn.
        await Assertions.Expect(Page.Locator(".vibe-grid-pager")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task TheThemeIsAppliedToQuickGridsOwnMarkup()
    {
        await OpenAsync();

        var style = await Page.EvaluateAsync<string[]>(
            """
            () => {
                const grid = document.querySelector('.vibe-grid');
                const table = grid.querySelector('table.quickgrid');
                const headers = [...table.querySelectorAll('thead th')];
                const stockHeader = headers.find(th => th.textContent.includes('Stock'));
                const nameCell = table.querySelector('tbody tr[aria-rowindex] td:nth-child(1)');
                const stockCell = table.querySelector('tbody tr[aria-rowindex] td:nth-child(2)');
                const css = element => getComputedStyle(element);
                return [
                    table.getAttribute('theme'),
                    css(nameCell).paddingLeft,
                    css(nameCell).borderBottomStyle,
                    css(nameCell).textAlign,
                    css(stockCell).textAlign,
                    css(stockHeader).textAlign,
                    String(Math.round(table.getBoundingClientRect().width) === Math.round(grid.getBoundingClientRect().width)),
                ];
            }
            """
        );

        style[0].ShouldBe("vibe");
        // 0.75rem: the rule reached a cell that belongs to QuickGrid, not to VibeGrid.
        style[1].ShouldBe("12px");
        style[2].ShouldBe("solid");
        // A column's Align: these rules are in QuickGrid's default theme, which this theme replaces.
        style[3].ShouldBe("left");
        style[4].ShouldBe("right");
        style[5].ShouldBe("right");
        style[6].ShouldBe("true", "the table fills the grid");
    }

    [Fact]
    public async Task TheSortArrowIsDrawnInTheTextColourAndTurnsOverForDescending()
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await OpenAsync();

        const string read = """
            () => {
                const indicator = document.querySelector('.vibe-grid .col-sort-asc .sort-indicator, .vibe-grid .col-sort-desc .sort-indicator');
                if (!indicator) return null;
                const css = getComputedStyle(indicator);
                const box = indicator.getBoundingClientRect();
                return [
                    css.backgroundColor,
                    css.color,
                    css.maskImage || css.webkitMaskImage || '',
                    String(Math.round(box.width)),
                    String(Math.round(box.height)),
                    css.transform,
                    (() => {
                        const text = indicator.parentElement.querySelector('.col-title-text').getBoundingClientRect();
                        const middle = box.top + box.height / 2;
                        return String(box.left >= text.right - 1 && middle > text.top && middle < text.bottom);
                    })(),
                    getComputedStyle(indicator.closest('.col-title')).cursor,
                    getComputedStyle(indicator.closest('.col-title')).borderTopWidth,
                ];
            }
            """;

        await Header("Name").ClickAsync();
        await Assertions
            .Expect(Page.Locator(".vibe-grid thead th", new() { HasTextString = "Name" }))
            .ToHaveAttributeAsync("aria-sort", "ascending");
        var ascending = await Page.EvaluateAsync<string[]>(read);

        await Header("Name").ClickAsync();
        await Assertions
            .Expect(Page.Locator(".vibe-grid thead th", new() { HasTextString = "Name" }))
            .ToHaveAttributeAsync("aria-sort", "descending");
        var descending = await Page.EvaluateAsync<string[]>(read);

        ascending.ShouldNotBeNull();
        descending.ShouldNotBeNull();

        // QuickGrid's default theme draws the arrow, and this theme replaces it. So the arrow is
        // the theme's to draw: a shape, with a size, filled with the text colour.
        ascending[0].ShouldBe(ascending[1]);
        ascending[0].ShouldNotBe("rgb(0, 0, 0)");
        ascending[2].ShouldContain("svg");
        int.Parse(ascending[3]).ShouldBeGreaterThan(4);
        int.Parse(ascending[4]).ShouldBeGreaterThan(4);

        // Beside the title, not under it, and the title is a plain thing to click.
        ascending[6].ShouldBe("true", "the arrow sits on the title's line, after it");
        ascending[7].ShouldBe("pointer");
        ascending[8].ShouldBe("0px");

        // And the two directions do not look the same.
        descending[5].ShouldNotBe(ascending[5], "descending must not draw the same arrow as ascending");
    }
}
