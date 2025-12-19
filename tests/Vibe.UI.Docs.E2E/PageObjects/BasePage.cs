using Microsoft.Playwright;

namespace Vibe.UI.Docs.E2E.PageObjects;

/// <summary>
/// Base page object containing common elements across all pages
/// Follows the Page Object Model pattern for maintainability
/// </summary>
public class BasePage
{
    protected readonly IPage Page;

    public BasePage(IPage page)
    {
        Page = page;
    }

    // Common Header Elements
    public ILocator Logo => Page.Locator("a[href='/'] img[alt='Vibe.UI'], a[href] img[alt='Vibe.UI']").First;
    public ILocator LogoLink => Page.Locator("a").Filter(new() { Has = Page.GetByAltText("Vibe.UI") });
    public ILocator SearchButton => Page.Locator("button:has-text('Search components')").First;
    public ILocator MobileSearchButton => Page.GetByLabel("Search");
    public ILocator ThemeToggle => Page.Locator("button.vibe-theme-toggle, button[aria-label='Toggle theme'], [data-theme-toggle]").First;
    public ILocator GitHubLink => Page.GetByRole(AriaRole.Link, new() { NameString = "View on GitHub" });

    // Mobile Navigation
    // Main Content
    public ILocator MainContent => Page.Locator("main").First;

    /// <summary>
    /// Click the search button to open command palette
    /// </summary>
    public async Task OpenSearchAsync()
    {
        // Try desktop search button first, fallback to mobile
        if (await SearchButton.IsVisibleAsync())
        {
            await SearchButton.ClickAsync();
        }
        else if (await MobileSearchButton.IsVisibleAsync())
        {
            await MobileSearchButton.ClickAsync();
        }
    }

    /// <summary>
    /// Toggle theme using the theme toggle button
    /// </summary>
    public async Task ToggleThemeAsync()
    {
        var before = await GetCurrentThemeAsync();

        await ThemeToggle.ClickAsync();

        await Page.WaitForFunctionAsync(
            @"(before) => {
                const current = document.documentElement.classList.contains('dark') ? 'dark' : 'light';
                return current !== before;
            }",
            before,
            new() { Timeout = 5000 }
        );
    }

    /// <summary>
    /// Get current theme from DOM
    /// </summary>
    public async Task<string> GetCurrentThemeAsync()
    {
        var hasDarkClass = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.classList.contains('dark')"
        );
        return hasDarkClass ? "dark" : "light";
    }

    /// <summary>
    /// Get theme from localStorage
    /// </summary>
    public async Task<string?> GetStoredThemeAsync()
    {
        return await Page.EvaluateAsync<string?>(
            "() => localStorage.getItem('vibe-theme') || localStorage.getItem('theme')"
        );
    }

    /// <summary>
    /// Navigate to a component page
    /// </summary>
    public async Task NavigateToComponentAsync(string componentName)
    {
        var link = Page.GetByRole(AriaRole.Link, new()
        {
            NameString = componentName,
            Exact = false
        });
        await link.ClickAsync();
    }

    /// <summary>
    /// Check if the main content is visible.
    /// </summary>
    public async Task<bool> IsMainContentVisibleAsync()
    {
        return await MainContent.IsVisibleAsync();
    }
}
