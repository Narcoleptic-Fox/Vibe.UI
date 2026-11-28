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
}
