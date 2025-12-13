using Shouldly;
using Vibe.UI.Docs.E2E.Infrastructure;
using Vibe.UI.Docs.E2E.PageObjects;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Functional;

/// <summary>
/// Functional tests for theme toggle functionality
/// Tests light/dark mode switching and persistence
/// </summary>
[Trait("Category", TestCategories.Functional)]
public class ThemeToggleTests : E2ETestBase
{
    [Fact]
    public async Task ThemeToggleSwitchesFromLightToDark()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");
        var basePage = new BasePage(Page);

        // Ensure starting in light mode
        await Page.EvaluateAsync("() => { document.documentElement.classList.remove('dark'); localStorage.setItem('theme', 'light'); }");
        await Page.WaitForTimeoutAsync(100);

        var initialTheme = await basePage.GetCurrentThemeAsync();

        // Act
        await basePage.ToggleThemeAsync();
        var afterToggle = await basePage.GetCurrentThemeAsync();

        // Assert
        initialTheme.ShouldBe("light", "Should start in light mode");
        afterToggle.ShouldBe("dark", "Should switch to dark mode");

        // Verify DOM class
        var hasDarkClass = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.classList.contains('dark')"
        );
        hasDarkClass.ShouldBeTrue("document.documentElement should have 'dark' class");
    }

    [Fact]
    public async Task ThemeToggleSwitchesFromDarkToLight()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");
        var basePage = new BasePage(Page);

        // Ensure starting in dark mode
        await Page.EvaluateAsync("() => { document.documentElement.classList.add('dark'); localStorage.setItem('theme', 'dark'); }");
        await Page.WaitForTimeoutAsync(100);

        var initialTheme = await basePage.GetCurrentThemeAsync();

        // Act
        await basePage.ToggleThemeAsync();
        var afterToggle = await basePage.GetCurrentThemeAsync();

        // Assert
        initialTheme.ShouldBe("dark", "Should start in dark mode");
        afterToggle.ShouldBe("light", "Should switch to light mode");

        // Verify DOM class removed
        var hasDarkClass = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.classList.contains('dark')"
        );
        hasDarkClass.ShouldBeFalse("document.documentElement should NOT have 'dark' class");
    }

    [Fact]
    public async Task ThemePersistsInLocalStorage()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");
        var basePage = new BasePage(Page);

        // Start with light mode
        await Page.EvaluateAsync("() => { document.documentElement.classList.remove('dark'); localStorage.setItem('theme', 'light'); }");

        // Act - Toggle to dark
        await basePage.ToggleThemeAsync();

        // Assert
        var storedTheme = await basePage.GetStoredThemeAsync();
        storedTheme.ShouldBe("dark", "Dark theme should be stored in localStorage");

        // Toggle back to light
        await basePage.ToggleThemeAsync();

        storedTheme = await basePage.GetStoredThemeAsync();
        storedTheme.ShouldBe("light", "Light theme should be stored in localStorage");
    }

    [Fact]
    public async Task ThemePersistsAcrossPageNavigation()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");
        var basePage = new BasePage(Page);

        // Set to dark mode
        await Page.EvaluateAsync("() => { document.documentElement.classList.add('dark'); localStorage.setItem('theme', 'dark'); }");
        await Page.WaitForTimeoutAsync(100);

        var initialTheme = await basePage.GetCurrentThemeAsync();
        initialTheme.ShouldBe("dark");

        // Act - Navigate to a different page
        await basePage.NavigateToComponentAsync("Button");
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
        await Page.WaitForBlazorReadyAsync();

        // Assert
        var themeAfterNav = await basePage.GetCurrentThemeAsync();
        themeAfterNav.ShouldBe("dark", "Theme should persist after navigation");
    }

    [Fact]
    public async Task DOMClassAppliedCorrectly()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");
        var basePage = new BasePage(Page);

        // Act - Toggle to dark mode
        await Page.EvaluateAsync("() => { document.documentElement.classList.remove('dark'); }");
        await basePage.ToggleThemeAsync();

        // Assert
        var classList = await Page.EvaluateAsync<string[]>(
            "() => Array.from(document.documentElement.classList)"
        );

        classList.ShouldContain("dark", "documentElement classList should contain 'dark'");

        // Toggle back to light
        await basePage.ToggleThemeAsync();

        classList = await Page.EvaluateAsync<string[]>(
            "() => Array.from(document.documentElement.classList)"
        );

        classList.ShouldNotContain("dark", "documentElement classList should NOT contain 'dark' in light mode");
    }
}
