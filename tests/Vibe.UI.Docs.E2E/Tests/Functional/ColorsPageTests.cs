using Microsoft.Playwright;
using Shouldly;
using Vibe.UI.Docs.E2E.Infrastructure;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Functional;

/// <summary>
/// The Colors page. The palette's hex values are held to the utility generator by a test in
/// Vibe.UI.CSS.Tests; these check that the page paints what it prints, and that the theme
/// colours follow the theme picker.
/// </summary>
[Trait("Category", TestCategories.Functional)]
public class ColorsPageTests : E2ETestBase
{
    [Fact]
    public async Task EveryPaletteSwatchShowsTheColourItPrints()
    {
        await NavigateAndWaitForBlazorAsync("/colors");

        var cells = await Page.Locator("[data-testid=palette] tbody td")
            .EvaluateAllAsync<string[][]>(
                """
                cells => cells.map(cell => {
                    const hex = cell.querySelector('.docs-palette-hex').textContent.trim();
                    const probe = document.createElement('span');
                    probe.style.color = hex;
                    document.body.appendChild(probe);
                    const expected = getComputedStyle(probe).color;
                    probe.remove();
                    return [hex, getComputedStyle(cell.querySelector('.docs-swatch')).backgroundColor, expected];
                })
                """
            );

        (await Page.Locator("[data-testid=palette] tbody tr").CountAsync()).ShouldBe(22);
        cells.Length.ShouldBe(22 * 11);
        cells
            .Where(cell => cell[1] != cell[2])
            .Select(cell => $"{cell[0]} painted as {cell[1]}")
            .ShouldBeEmpty();
    }

    [Fact]
    public async Task ThemeColoursFollowTheThemePicker()
    {
        await NavigateAndWaitForBlazorAsync("/colors");
        var primary = Page.Locator(
                "[data-testid=theme-colors] tr",
                new() { HasText = "--vibe-primary" }
            )
            .First.Locator(".docs-swatch");

        var before = await primary.EvaluateAsync<string>(
            "element => getComputedStyle(element).backgroundColor"
        );
        await Page.Locator("header")
            .GetByLabel("Theme", new() { Exact = true })
            .SelectOptionAsync(BuiltInThemes.Names[^1]);

        string after = before;
        for (var attempt = 0; attempt < 30 && after == before; attempt++)
        {
            await Page.WaitForTimeoutAsync(100);
            after = await primary.EvaluateAsync<string>(
                "element => getComputedStyle(element).backgroundColor"
            );
        }

        after.ShouldNotBe(before, "The primary swatch should change when the theme does");
        (await Page.ThemeMismatchAsync(BuiltInThemes.Names[^1])).ShouldBeNull();
    }
}
