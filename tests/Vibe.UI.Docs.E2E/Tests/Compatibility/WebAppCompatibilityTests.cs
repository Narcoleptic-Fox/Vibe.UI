using Microsoft.Playwright;
using Shouldly;
using Vibe.UI.Docs.E2E.Infrastructure;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Compatibility;

[Trait("Category", TestCategories.Compatibility)]
public class WebAppCompatibilityTests : CompatibilityE2ETestBase
{
    protected override CompatibilityApp App => CompatibilityApp.WebApp;

    [Fact]
    public async Task StaticSsrRendersVibeUiComponents()
    {
        await NavigateToSmokePageAsync("/", "webapp-static-smoke");

        await Page.Locator("text=Static render path").WaitForAsync();
        await Page.Locator("text=Interactive Server").WaitForAsync();
        await Page.Locator("text=Interactive WebAssembly").WaitForAsync();
        await Page.Locator("text=Interactive Auto").WaitForAsync();

        AssertNoBrowserErrors();
    }

    [Theory]
    [InlineData("/server", "webapp-server-smoke", "server")]
    [InlineData("/client", "webapp-client-smoke", "client")]
    [InlineData("/auto", "webapp-auto-smoke", "auto")]
    public async Task InteractiveRenderModesSupportVibeUiInteractions(string path, string smokeTestId, string prefix)
    {
        await NavigateToSmokePageAsync(path, smokeTestId);
        await VerifyInteractiveSmokeAsync(prefix);

        AssertNoBrowserErrors();
    }

    // ---- The interactive boundary (charter: "One limitation to state up front") ----
    // A PropertyInput takes an expression and a Form takes child content. Neither can be
    // serialized, so neither can be handed from a static page to an interactive component.

    [Theory]
    [InlineData("server")]
    [InlineData("client")]
    [InlineData("auto")]
    public async Task AFormInsideOneInteractiveComponentWorksInEachRenderMode(string mode)
    {
        await NavigateToSmokePageAsync($"/boundary/inside/{mode}", $"boundary-{mode}");

        // The label comes from the model's metadata, on the server prerender and after hand-off.
        (await Page.Locator($"#{mode}-name-label").TextContentAsync()).ShouldNotBeNull().ShouldContain("Guest name");

        // Typed before the runtime attaches, text is lost, so type until the model answers.
        // Each attempt types something new: a browser reports a change only when the value
        // differs from the last one it reported, so the same text twice would be no retry.
        var name = Page.Locator($"#{mode}-name");
        var echo = Page.Locator($"[data-testid='boundary-{mode}-echo']");
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        var typed = string.Empty;
        for (var attempt = 1; ; attempt++)
        {
            typed = $"Ada{attempt}";
            await name.FillAsync(typed);
            await name.BlurAsync();
            try
            {
                await Assertions.Expect(echo).ToHaveTextAsync($"{typed} for 2", new() { Timeout = 1000 });
                break;
            }
            catch (PlaywrightException) when (DateTimeOffset.UtcNow < deadline)
            {
            }
        }

        var guests = Page.Locator($"#{mode}-guests");
        await guests.FillAsync("5");
        await guests.BlurAsync();
        await Assertions.Expect(echo).ToHaveTextAsync($"{typed} for 5");

        AssertNoBrowserErrors();
    }

    [Theory]
    [InlineData("server", "ServerComponentSerializer")]
    [InlineData("client", "WebAssemblyComponentSerializer")]
    [InlineData("auto", "ServerComponentSerializer")]
    public async Task ForHandedFromAStaticPageToAnInteractivePropertyInputFailsTheRequest(string mode, string serializer)
    {
        var response = await Page.GotoAsync($"{BaseUrl.TrimEnd('/')}/boundary/field-across/{mode}");

        response.ShouldNotBeNull().Status.ShouldBe(500);
        var body = System.Net.WebUtility.HtmlDecode(await response.TextAsync());

        // The framework's message names neither PropertyInput nor For. This is the text a
        // developer sees, so the docs quote it; if it changes, the docs are wrong.
        body.ShouldContain("Serialization and deserialization of 'System.Type' instances is not supported");
        // And it is raised where the static renderer serializes an interactive component's
        // parameters, by the serializer of that render mode.
        body.ShouldContain("SSRRenderModeBoundary");
        body.ShouldContain(serializer);
    }

    [Fact]
    public async Task ChildContentHandedFromAStaticPageToAnInteractiveFormFailsTheRequest()
    {
        var response = await Page.GotoAsync($"{BaseUrl.TrimEnd('/')}/boundary/form-across");

        response.ShouldNotBeNull().Status.ShouldBe(500);
        var body = System.Net.WebUtility.HtmlDecode(await response.TextAsync());

        body.ShouldContain("Cannot pass the parameter 'ChildContent' to component 'Form`1' with rendermode 'InteractiveServerRenderMode'");
        body.ShouldContain("cannot be serialized");
    }

    [Fact]
    public async Task WithNoRenderModeAtAllTheFieldIsDrawnButNeverWritesTheModel()
    {
        await NavigateToSmokePageAsync("/boundary/none", "boundary-none");

        (await Page.Locator("#none-name-label").TextContentAsync()).ShouldNotBeNull().ShouldContain("Guest name");

        var name = Page.Locator("#none-name");
        await name.FillAsync("Ada");
        await name.BlurAsync();
        await Page.WaitForTimeoutAsync(1500);

        (await Page.Locator("[data-testid='boundary-none-echo']").TextContentAsync()).ShouldBe("[]");
        AssertNoBrowserErrors();
    }

    // ---- The grid across the same boundary ----
    // VibeGrid takes a provider, which is a delegate, and columns, which are child content.

    [Theory]
    [InlineData("server")]
    [InlineData("client")]
    [InlineData("auto")]
    public async Task AGridStartsWithThePageTheServerFetchedThenPagesAndSortsInEachRenderMode(string mode)
    {
        // What the server sends, before any script runs: the page is there, and fetching it
        // took one request.
        var prerendered = await Page.Context.APIRequest.GetAsync($"{BaseUrl.TrimEnd('/')}/grid/inside/{mode}");
        prerendered.Status.ShouldBe(200);
        var html = await prerendered.TextAsync();
        html.ShouldContain("Part 001");
        html.ShouldContain("Requests: 1");

        await NavigateToSmokePageAsync($"/grid/inside/{mode}", $"grid-{mode}");

        var grid = Page.Locator($"[data-testid='grid-{mode}']");
        var rows = grid.Locator("tbody tr[aria-rowindex]");
        var firstCell = rows.First.Locator("td").First;
        var requests = grid.Locator($"[data-testid='grid-{mode}-requests']");
        var poke = grid.Locator($"[data-testid='grid-{mode}-poke']");

        await PokeUntilInteractiveAsync(poke);

        // The interactive component is a new one, and it has asked nothing: it started with the
        // page the server fetched. The rows and the paginator are drawn from that page.
        await Assertions.Expect(requests).ToHaveTextAsync("Requests: 0");
        await Assertions.Expect(rows).ToHaveCountAsync(10);
        await Assertions.Expect(firstCell).ToHaveTextAsync("Part 001");
        await Assertions.Expect(grid.Locator(".vibe-grid-pager").GetByText("10", new() { Exact = true })).ToBeVisibleAsync();

        await grid.Locator(".vibe-grid-pager").GetByText("2", new() { Exact = true }).ClickAsync();
        await Assertions.Expect(firstCell).ToHaveTextAsync("Part 011");
        await Assertions.Expect(requests).ToHaveTextAsync("Requests: 1");

        await grid.Locator("thead button.col-title", new() { HasTextString = "Stock" }).ClickAsync();
        await Assertions.Expect(requests).ToHaveTextAsync("Requests: 2");
        await Assertions
            .Expect(grid.Locator("thead th", new() { HasTextString = "Stock" }))
            .ToHaveAttributeAsync("aria-sort", "ascending");

        AssertNoBrowserErrors();
    }

    [Fact]
    public async Task AProviderHandedFromAStaticPageToAnInteractiveGridFailsTheRequest()
    {
        var response = await Page.GotoAsync($"{BaseUrl.TrimEnd('/')}/grid/provider-across");

        response.ShouldNotBeNull().Status.ShouldBe(500);
        var body = System.Net.WebUtility.HtmlDecode(await response.TextAsync());

        // A delegate is refused by name, unlike the expression a PropertyInput takes.
        body.ShouldContain("Cannot pass the parameter 'ItemsProvider' to component 'VibeGrid`1' with rendermode 'InteractiveServerRenderMode'");
        body.ShouldContain("cannot be serialized");
    }

    [Theory]
    [InlineData("keyed", "Alpha: 0, Beta: 0")]
    [InlineData("unkeyed", "Alpha: 1, Beta: 1")]
    public async Task TwoGridsInOneComponentStartWithTheirOwnPagesOnlyWhenEachHasAKey(string route, string interactiveRequests)
    {
        await NavigateToSmokePageAsync($"/grid/two/{route}", "two-grids");

        var poke = Page.GetByTestId("two-grids-poke");
        await PokeUntilInteractiveAsync(poke);

        // Keyed: neither asks. Unkeyed: the two share a name, so nothing is kept for either and
        // each asks once. Either way each grid shows its own rows.
        await Assertions.Expect(Page.GetByTestId("two-grids-requests")).ToHaveTextAsync(interactiveRequests);
        await Assertions
            .Expect(Page.Locator("[data-testid='first'] tbody tr[aria-rowindex] td").First)
            .ToHaveTextAsync("Alpha 1");
        await Assertions
            .Expect(Page.Locator("[data-testid='second'] tbody tr[aria-rowindex] td").First)
            .ToHaveTextAsync("Beta 1");

        AssertNoBrowserErrors();
    }

    [Theory]
    [InlineData("keyed", "Requests: 0")]
    [InlineData("unkeyed", "Requests: 1")]
    public async Task TheSameGridComponentTwiceOnAPageShowsItsOwnRowsAndKeepsItsPageOnlyWithAKey(string route, string interactiveRequests)
    {
        await NavigateToSmokePageAsync($"/grid/twice/{route}", "grid-left");

        foreach (var (mode, label) in new[] { ("left", "Left"), ("right", "Right") })
        {
            var grid = Page.Locator($"[data-testid='grid-{mode}']");
            await PokeUntilInteractiveAsync(grid.Locator($"[data-testid='grid-{mode}-poke']"));

            await Assertions.Expect(grid.Locator($"[data-testid='grid-{mode}-requests']")).ToHaveTextAsync(interactiveRequests);
            await Assertions.Expect(grid.Locator("tbody tr[aria-rowindex]")).ToHaveCountAsync(10);
            await Assertions.Expect(grid.Locator("tbody tr[aria-rowindex] td").First).ToHaveTextAsync($"{label} 001");
        }

        // A grid under its own name keeps its page whatever the other two do with theirs.
        var third = Page.Locator("[data-testid='grid-third']");
        await PokeUntilInteractiveAsync(third.Locator("[data-testid='grid-third-poke']"));
        await Assertions.Expect(third.Locator("[data-testid='grid-third-requests']")).ToHaveTextAsync("Requests: 0");
        await Assertions.Expect(third.Locator("tbody tr[aria-rowindex] td").First).ToHaveTextAsync("Third 001");

        AssertNoBrowserErrors();
    }

    [Fact]
    public async Task AGridOnAPageWithNoRenderModeIsRenderedAndAnInteractiveGridBesideItStillStartsWithItsPage()
    {
        // With the attribute the framework offers for this, a grid on a static page failed the
        // request: a component with no render mode cannot keep state. Seen as an HTTP 500.
        var response = await Page.Context.APIRequest.GetAsync($"{BaseUrl.TrimEnd('/')}/grid/static");
        response.Status.ShouldBe(200);
        var html = await response.TextAsync();
        html.ShouldContain("Static 001");
        html.ShouldContain("Beside 001");

        await NavigateToSmokePageAsync("/grid/static", "grid-static");

        var still = Page.Locator("[data-testid='grid-static']");
        await Assertions.Expect(still.Locator("tbody tr[aria-rowindex]")).ToHaveCountAsync(10);
        await Assertions.Expect(still.Locator("[data-testid='grid-static-requests']")).ToHaveTextAsync("Requests: 1");

        var beside = Page.Locator("[data-testid='grid-beside']");
        await PokeUntilInteractiveAsync(beside.Locator("[data-testid='grid-beside-poke']"));
        await Assertions.Expect(beside.Locator("[data-testid='grid-beside-requests']")).ToHaveTextAsync("Requests: 0");
        await Assertions.Expect(beside.Locator("tbody tr[aria-rowindex] td").First).ToHaveTextAsync("Beside 001");

        AssertNoBrowserErrors();
    }

    // Interactive once a click is counted. Each attempt is a new click, so it can be repeated.
    private static async Task PokeUntilInteractiveAsync(ILocator poke)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (true)
        {
            await poke.ClickAsync();
            try
            {
                await Assertions.Expect(poke).Not.ToHaveTextAsync("Poked 0", new() { Timeout = 1000 });
                break;
            }
            catch (PlaywrightException) when (DateTimeOffset.UtcNow < deadline)
            {
            }
        }
    }
}
