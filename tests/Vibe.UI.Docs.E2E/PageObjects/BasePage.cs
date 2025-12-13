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
    public ILocator SearchButton => Page.GetByPlaceholder("Search components...");
    public ILocator MobileSearchButton => Page.GetByLabel("Search");
    public ILocator ThemeToggle => Page.Locator("button").Filter(new() { Has = Page.Locator("svg").First });
    public ILocator GitHubLink => Page.GetByRole(AriaRole.Link, new() { NameString = "View on GitHub" });

    // Mobile Navigation
    public ILocator MobileMenuButton => Page.GetByLabel("Toggle menu");

    // Sidebar
    public ILocator Sidebar => Page.Locator(".docs-sidebar");
    public ILocator SidebarNav => Page.Locator(".sidebar-nav");

    // Main Content
    public ILocator MainContent => Page.Locator(".docs-content");
    public ILocator ContentWrapper => Page.Locator(".content-wrapper");

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
        await ThemeToggle.ClickAsync();
        // Wait for theme transition
        await Page.WaitForTimeoutAsync(300);
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
            "() => localStorage.getItem('theme')"
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
    /// Toggle mobile sidebar
    /// </summary>
    public async Task ToggleMobileSidebarAsync()
    {
        await MobileMenuButton.ClickAsync();
        await Page.WaitForTimeoutAsync(300); // Wait for animation
    }

    /// <summary>
    /// Check if mobile menu is visible
    /// </summary>
    public async Task<bool> IsMobileMenuVisibleAsync()
    {
        return await MobileMenuButton.IsVisibleAsync();
    }

    /// <summary>
    /// Check if sidebar is open (mobile)
    /// </summary>
    public async Task<bool> IsSidebarOpenAsync()
    {
        var sidebar = await Sidebar.GetAttributeAsync("class");
        return sidebar?.Contains("open") ?? false;
    }
}
