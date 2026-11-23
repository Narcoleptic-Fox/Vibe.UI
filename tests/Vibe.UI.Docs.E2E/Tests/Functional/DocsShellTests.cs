using System.Text.RegularExpressions;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Shouldly;
using Vibe.UI.Docs.E2E.Infrastructure;
using Vibe.UI.Docs.E2E.Tests.Smoke;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Functional;

/// <summary>
/// Contracts for the site shell: the navigation rail, the "On this page" list, the header
/// links, the footer, the not-found page, and how they collapse on a phone.
/// </summary>
[Trait("Category", TestCategories.Functional)]
public class DocsShellTests : E2ETestBase
{
    private const string RailSelector = "nav[aria-label='Documentation']";
    private const string TocSelector = "nav[aria-label='On this page']";

    [Fact]
    public async Task ComponentPageShowsTheNavigationRailWithTheCurrentPageMarked()
    {
        await NavigateAndWaitForBlazorAsync("/components/button");

        var rail = Page.Locator(RailSelector);
        await rail.WaitForAsync();

        var current = rail.Locator("a[aria-current='page']");
        (await current.CountAsync()).ShouldBe(
            1,
            "Exactly one rail link should be marked as the current page"
        );
        (await current.InnerTextAsync()).Trim().ShouldBe("Button");
        (
            await current.EvaluateAsync<bool>(
                """
                link => {
                    const rail = link.closest('.docs-rail').getBoundingClientRect();
                    const box = link.getBoundingClientRect();
                    return box.top >= rail.top && box.bottom <= rail.bottom;
                }
                """
            )
        ).ShouldBeTrue("The current page's link should be scrolled into view inside the rail");
    }

    [Fact]
    public async Task HomePageHasNoNavigationRail()
    {
        await NavigateAndWaitForBlazorAsync("/");

        (await Page.Locator(RailSelector).CountAsync()).ShouldBe(0);
        (await Page.Locator(TocSelector).CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task NavigationRailListsEveryRoutedComponentPageOnce()
    {
        await NavigateAndWaitForBlazorAsync("/components");

        var rail = Page.Locator(RailSelector);
        await rail.WaitForAsync();

        var hrefs = await rail.Locator("a[href^='components/']")
            .EvaluateAllAsync<string[]>("links => links.map(link => link.getAttribute('href'))");

        var expected = ComponentPageManifest
            .Pages.Select(page => page.CanonicalRoute.TrimStart('/'))
            .OrderBy(route => route, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        hrefs
            .OrderBy(route => route, StringComparer.OrdinalIgnoreCase)
            .ToArray()
            .ShouldBe(
                expected,
                ignoreOrder: false,
                "The rail must link every routed component page, relative to the base path"
            );
    }

    [Fact]
    public async Task HeaderDocsLinkOpensTheGettingStartedGuide()
    {
        await NavigateAndWaitForBlazorAsync("/");

        var docsLink = Page.Locator("header nav[aria-label='Primary']")
            .GetByRole(AriaRole.Link, new() { Name = "Docs", Exact = true });
        (await docsLink.GetAttributeAsync("href")).ShouldBe(
            "getting-started",
            "The Docs link must be relative so it works under the deployed base path"
        );

        await docsLink.ClickAsync();
        await Page.WaitForURLAsync(new Regex("/getting-started$"));

        var heading = Page.GetByRole(AriaRole.Heading, new() { Level = 1 });
        (await heading.InnerTextAsync()).Trim().ShouldBe("Getting Started");
        await Page.Locator(RailSelector)
            .Locator("a[aria-current='page']")
            .Filter(new() { HasText = "Introduction" })
            .WaitForAsync();
    }

    [Fact]
    public async Task TableOfContentsListsThePageSectionsAndScrollsToThem()
    {
        await Page.SetViewportSizeAsync(1440, 900);
        await NavigateAndWaitForBlazorAsync("/components/button");

        var toc = Page.Locator(TocSelector);
        await toc.WaitForAsync();

        var entries = (await toc.Locator("a").AllInnerTextsAsync())
            .Select(text => text.Trim())
            .ToArray();
        entries.ShouldContain("Examples");
        entries.ShouldContain("API Reference");
        entries.ShouldContain("Accessibility");

        // Every entry must point at a heading on the page with the same text.
        (
            await toc.Locator("a")
                .EvaluateAllAsync<bool>(
                    """
                    links => links.every(link => {
                        const heading = document.getElementById(link.dataset.tocTarget);
                        return heading !== null && heading.textContent.trim() === link.textContent.trim();
                    })
                    """
                )
        ).ShouldBeTrue("Each table of contents entry must resolve to its heading");

        await toc.GetByRole(AriaRole.Link, new() { Name = "API Reference", Exact = true })
            .ClickAsync();

        // The heading stops just below the sticky header, not underneath it.
        await Page.WaitForFunctionAsync(
            """
            () => {
                const top = document.getElementById('api-reference').getBoundingClientRect().top;
                return top >= 56 && top <= 140;
            }
            """
        );
        Page.Url.ShouldEndWith("/components/button#api-reference");
        await toc.Locator("a[aria-current='location']")
            .Filter(new() { HasText = "API Reference" })
            .WaitForAsync();
    }

    [Fact]
    public async Task TableOfContentsIsRebuiltWhenNavigatingBetweenPages()
    {
        await Page.SetViewportSizeAsync(1440, 900);
        await NavigateAndWaitForBlazorAsync("/components/button");
        await Page.Locator(TocSelector).WaitForAsync();

        await Page.Locator(RailSelector)
            .GetByRole(AriaRole.Link, new() { Name = "Table", Exact = true })
            .ClickAsync();
        await Page.WaitForURLAsync(new Regex("/components/table$"));

        // The list must describe the page navigated to, not the one navigated from.
        await Page.WaitForFunctionAsync(
            """
            () => {
                const title = document.querySelector('main h1')?.textContent.trim();
                const links = [...document.querySelectorAll("nav[aria-label='On this page'] a")];
                const sections = [...document.querySelectorAll('main section > h2')];
                return title === 'Table' &&
                    links.length >= sections.length &&
                    sections.every(heading => links.some(link =>
                        link.dataset.tocTarget === heading.id &&
                        link.textContent.trim() === heading.textContent.trim()));
            }
            """
        );
    }

    [Fact]
    public async Task TableOfContentsIsHiddenBelowItsBreakpointSoThePageColumnKeepsItsWidth()
    {
        await Page.SetViewportSizeAsync(1280, 900);
        await NavigateAndWaitForBlazorAsync("/components/button");

        await Page.Locator(RailSelector).WaitForAsync();
        (await Page.Locator(".docs-rail-toc").IsVisibleAsync()).ShouldBeFalse();

        var columnWidth = await Page.Locator("main .live-preview")
            .First.EvaluateAsync<double>("element => element.getBoundingClientRect().width");
        columnWidth.ShouldBe(
            896,
            1,
            "The page column should keep its full 56rem width beside the rail"
        );
    }

    [Fact]
    public async Task WideWindowPinsTheRailAndLogoToTheLeftEdge()
    {
        // Wider than 96rem, the rail and the app bar stay attached to the window's left edge.
        // The page stops growing and sits, with its table of contents, in the space left over.
        await Page.SetViewportSizeAsync(1920, 900);
        await NavigateAndWaitForBlazorAsync("/components/button");
        await Page.Locator(TocSelector).WaitForAsync();

        var box = await Page.EvaluateAsync<double[]>(
            @"() => {
                const rect = selector => document.querySelector(selector).getBoundingClientRect();
                const rail = rect('.docs-rail-nav');
                const main = rect('.docs-shell-docs > main');
                const toc = rect('.docs-rail-toc');
                return [
                    rail.left,
                    rect('header a img').left,
                    rect('.docs-rail-nav a').left,
                    main.left - rail.right,
                    document.documentElement.clientWidth - toc.right,
                    toc.left - main.right,
                    rect('main .live-preview').width,
                    rect('.docs-footer-inner').left,
                    document.documentElement.scrollWidth - document.documentElement.clientWidth
                ];
            }");

        box[0].ShouldBe(0, 0.5, "The rail should start at the window's left edge");
        box[1].ShouldBeLessThan(40, "The logo should sit at the left edge, above the rail");
        Math.Abs(box[1] - box[2]).ShouldBeLessThan(16, "The logo and the rail's links should line up");
        box[3].ShouldBeGreaterThan(100, "This test needs a window wide enough to leave spare room");
        box[3].ShouldBe(box[4], 1, "The page and its table of contents should be centered in the spare room");
        box[5].ShouldBe(0, 1, "The table of contents should stay beside the page");
        box[6].ShouldBe(896, 1, "The page column should keep its 56rem width");
        box[7].ShouldBe(0, 0.5, "The footer should span the window like the app bar");
        box[8].ShouldBeLessThanOrEqualTo(0, "The layout must not add sideways scrolling");
    }

    [Fact]
    [Trait("Category", TestCategories.Mobile)]
    public async Task PhoneWidthReplacesTheRailWithADrawer()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateAndWaitForBlazorAsync("/components/button");

        (await Page.Locator(".docs-rail-nav").IsVisibleAsync()).ShouldBeFalse(
            "The rail should not take space on a phone"
        );

        var menuButton = Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Open navigation menu", Exact = true }
        );
        await menuButton.ClickAsync();

        var drawer = Page.GetByRole(AriaRole.Dialog);
        await drawer.WaitForAsync();

        var drawerNav = drawer.Locator("nav[aria-label='Site navigation']");
        (await drawerNav.Locator("a[aria-current='page']").InnerTextAsync())
            .Trim()
            .ShouldBe("Button");

        await drawerNav
            .GetByRole(AriaRole.Link, new() { Name = "Card", Exact = true })
            .ClickAsync();
        await Page.WaitForURLAsync(new Regex("/components/card$"));
        await drawer.WaitForAsync(new() { State = WaitForSelectorState.Detached });

        (await Page.GetByRole(AriaRole.Heading, new() { Level = 1 }).InnerTextAsync())
            .Trim()
            .ShouldBe("Card");
    }

    [Fact]
    public async Task UnknownAddressShowsTheNotFoundPageInsideTheSiteLayout()
    {
        await NavigateAndWaitForBlazorAsync("/no/such/page");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Level = 1 });
        (await heading.InnerTextAsync()).Trim().ShouldBe("Page not found");
        (await Page.Locator("main").InnerTextAsync()).ShouldContain("/no/such/page");

        (await new PageObjects.BasePage(Page).Logo.IsVisibleAsync()).ShouldBeTrue(
            "The header should still be there"
        );
        (await Page.Locator("footer.docs-footer").IsVisibleAsync()).ShouldBeTrue(
            "The footer should still be there"
        );

        var home = Page.Locator("main")
            .GetByRole(AriaRole.Link, new() { Name = "Back to home", Exact = true });
        (await home.GetAttributeAsync("href")).ShouldBe("./");

        await Page.Locator("main")
            .GetByRole(AriaRole.Link, new() { Name = "Browse components", Exact = true })
            .ClickAsync();
        await Page.WaitForURLAsync(new Regex("/components$"));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/components")]
    [InlineData("/components/button")]
    [InlineData("/getting-started")]
    public async Task EveryPageKindEndsWithTheSiteFooter(string path)
    {
        await NavigateAndWaitForBlazorAsync(path);

        var footer = Page.Locator("footer.docs-footer");
        (await footer.CountAsync()).ShouldBe(1);
        (await Page.Locator("body footer").CountAsync()).ShouldBe(
            1,
            "Pages should not bring a second footer"
        );

        var github = footer.GetByRole(AriaRole.Link, new() { Name = "GitHub", Exact = true });
        (await github.GetAttributeAsync("href")).ShouldBe(
            "https://github.com/Narcoleptic-Fox/Vibe.UI"
        );
    }

    [Theory]
    [Trait("Category", TestCategories.Mobile)]
    [InlineData("/")]
    [InlineData("/components")]
    [InlineData("/components/button")]
    [InlineData("/components/table")]
    [InlineData("/getting-started")]
    [InlineData("/no/such/page")]
    public async Task PhoneWidthPagesDoNotScrollSideways(string path)
    {
        await Page.SetViewportSizeAsync(390, 844);
        await NavigateAndWaitForBlazorAsync(path);
        await Page.GetByRole(AriaRole.Heading, new() { Level = 1 }).WaitForAsync();

        var overflow = await Page.EvaluateAsync<int>(
            "() => document.documentElement.scrollWidth - document.documentElement.clientWidth"
        );
        overflow.ShouldBe(0, $"{path} should fit a 390px viewport without horizontal scrolling");

        // A clipping container hides overflow from the page-level check, so also require the
        // page title itself to sit inside the viewport.
        (await Page.GetByRole(AriaRole.Heading, new() { Level = 1 }).EvaluateAsync<bool>(
            """
            heading => {
                const box = heading.getBoundingClientRect();
                return box.left >= 0 && box.right <= window.innerWidth;
            }
            """)).ShouldBeTrue($"The title of {path} should not be clipped on a phone");

        // Every header control must be inside the viewport.
        (
            await Page.Locator("header")
                .First.Locator("a, button")
                .EvaluateAllAsync<bool>(
                    """
                    controls => controls
                        .filter(control => control.getClientRects().length > 0)
                        .every(control => {
                            const box = control.getBoundingClientRect();
                            return box.left >= 0 && box.right <= window.innerWidth;
                        })
                    """
                )
        ).ShouldBeTrue("Header controls should not be pushed off screen on a phone");
    }

    [Fact]
    public async Task HomePageStatesOnlyWhatIsTrueToday()
    {
        await NavigateAndWaitForBlazorAsync("/");

        var text = await Page.Locator("main").InnerTextAsync();

        // The beta is not on NuGet: any install command must name the locally packed source.
        Regex
            .Matches(text, @"dotnet tool install[^\n]*")
            .Select(match => match.Value)
            .Where(command => !command.Contains("--add-source", StringComparison.Ordinal))
            .ShouldBeEmpty(
                "The home page must not advertise installing the unpublished beta from NuGet"
            );

        text.ShouldNotContain("111");
        text.ShouldNotContain("Production-ready", Case.Insensitive);
        text.ShouldContain($"{ComponentPageManifest.Pages.Count} documented components");
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public async Task ShellPartsOutsideTheFlagshipScanHaveNoSeriousAxeViolations(string theme)
    {
        // The flagship accessibility scan runs at 1280px, where the table of contents is
        // hidden and the drawer is closed. Scan those two here.
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await Page.SetViewportSizeAsync(1440, 900);
        await NavigateAndWaitForBlazorAsync("/components/button");
        await Page.EvaluateAsync("theme => document.documentElement.classList.toggle('dark', theme === 'dark')", theme);

        var toc = Page.Locator(TocSelector);
        await toc.WaitForAsync();
        SeriousViolations(await toc.RunAxe()).ShouldBeEmpty("The table of contents should pass axe");
        SeriousViolations(await Page.Locator("footer.docs-footer").RunAxe())
            .ShouldBeEmpty("The footer should pass axe");

        await Page.SetViewportSizeAsync(375, 667);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Open navigation menu", Exact = true }).ClickAsync();
        var drawer = Page.GetByRole(AriaRole.Dialog);
        await drawer.WaitForAsync();
        SeriousViolations(await drawer.RunAxe()).ShouldBeEmpty("The open navigation drawer should pass axe");
    }

    private static string[] SeriousViolations(Deque.AxeCore.Commons.AxeResult result) =>
        result.Violations
            .Where(violation =>
                string.Equals(violation.Impact, "critical", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(violation.Impact, "serious", StringComparison.OrdinalIgnoreCase))
            .Select(violation => $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target?.ToString()))}")
            .ToArray();
}
