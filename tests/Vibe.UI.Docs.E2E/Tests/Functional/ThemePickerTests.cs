using Microsoft.Playwright;
using Shouldly;
using Vibe.UI.Docs.E2E.Infrastructure;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Functional;

/// <summary>
/// The header's theme picker: a reader can see the docs in every built-in theme, in light and
/// dark, and the choice is kept across pages and reloads.
/// </summary>
[Trait("Category", TestCategories.Functional)]
public class ThemePickerTests : E2ETestBase
{
    private ILocator Picker => Page.Locator("header").GetByLabel("Theme", new() { Exact = true });

    [Fact]
    public async Task PickerOffersEveryBuiltInThemeAndStartsOnTheDefault()
    {
        await NavigateAndWaitForBlazorAsync("/components/button");

        (await Picker.Locator("option").AllInnerTextsAsync())
            .Select(text => text.Trim())
            .ShouldBe(BuiltInThemes.Names);
        (await Picker.InputValueAsync()).ShouldBe(BuiltInThemes.Names[0]);
        (await Page.ThemeMismatchAsync(BuiltInThemes.Names[0])).ShouldBeNull();
    }

    [Fact]
    public async Task EachThemeAppliesInLightAndDarkAndSurvivesAReload()
    {
        await NavigateAndWaitForBlazorAsync("/components/button");

        foreach (var theme in BuiltInThemes.Names.Skip(1).Append(BuiltInThemes.Names[0]))
        {
            await Picker.SelectOptionAsync(theme);

            foreach (var dark in new[] { false, true })
            {
                await Page.EvaluateAsync(
                    "dark => document.documentElement.classList.toggle('dark', dark)",
                    dark
                );
                await ShouldShowAsync(theme, dark ? "dark" : "light");
            }

            await Page.EvaluateAsync("() => document.documentElement.classList.remove('dark')");
            // A fresh load of the page, as a returning reader would get.
            await NavigateAndWaitForBlazorAsync("/components/button");
            await ShouldShowAsync(theme, "after a reload");
            (await Picker.InputValueAsync()).ShouldBe(theme);
        }
    }

    [Fact]
    public async Task ChoiceCarriesAcrossPages()
    {
        await NavigateAndWaitForBlazorAsync("/components/button");
        var theme = BuiltInThemes.Names[^1];
        await Picker.SelectOptionAsync(theme);

        await Page.Locator("header nav[aria-label='Primary']")
            .GetByRole(AriaRole.Link, new() { Name = "Docs", Exact = true })
            .ClickAsync();
        await Page.GetByRole(AriaRole.Heading, new() { Level = 1 }).WaitForAsync();

        await ShouldShowAsync(theme, "on another page");
    }

    private async Task ShouldShowAsync(string theme, string when)
    {
        string? mismatch = null;
        for (var attempt = 0; attempt < 30; attempt++)
        {
            mismatch = await Page.ThemeMismatchAsync(theme);
            if (mismatch is null)
            {
                return;
            }

            await Page.WaitForTimeoutAsync(100);
        }

        mismatch.ShouldBeNull($"The page should show {theme} {when}");
    }
}
