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

        // Interactive once a click is counted. Each attempt is a new click, so it can be repeated.
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

        // Keyed: neither asks. Unkeyed: the framework cannot tell them apart, so neither is given
        // a page and each asks once. Either way each grid shows its own rows.
        await Assertions.Expect(Page.GetByTestId("two-grids-requests")).ToHaveTextAsync(interactiveRequests);
        await Assertions
            .Expect(Page.Locator("[data-testid='first'] tbody tr[aria-rowindex] td").First)
            .ToHaveTextAsync("Alpha 1");
        await Assertions
            .Expect(Page.Locator("[data-testid='second'] tbody tr[aria-rowindex] td").First)
            .ToHaveTextAsync("Beta 1");

        AssertNoBrowserErrors();
    }
}
