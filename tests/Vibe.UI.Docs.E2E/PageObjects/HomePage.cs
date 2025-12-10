using Microsoft.Playwright;

namespace Vibe.UI.Docs.E2E.PageObjects;

/// <summary>
/// Page object for the home/index page
/// </summary>
public class HomePage : BasePage
{
    public HomePage(IPage page) : base(page)
    {
    }

    // Hero Section
    public ILocator HeroTitle => Page.GetByRole(AriaRole.Heading, new() { Level = 1 });
    public ILocator HeroSubtitle => Page.Locator(".hero-subtitle, .text-xl, .text-lg").First;

    // Call-to-Action Buttons
    public ILocator GetStartedButton => Page.GetByRole(AriaRole.Link, new()
    {
        NameString = "Get Started",
        Exact = false
    });

    public ILocator ComponentsButton => Page.GetByRole(AriaRole.Link, new()
    {
        NameString = "Components",
        Exact = false
    });

    // Feature Cards (if present)
    public ILocator FeatureCards => Page.Locator(".feature-card, .card");

    /// <summary>
    /// Navigate to the home page
    /// </summary>
    public async Task NavigateAsync()
    {
        await Page.GotoAsync("/");
    }

    /// <summary>
    /// Click the "Get Started" CTA button
    /// </summary>
    public async Task ClickGetStartedAsync()
    {
        await GetStartedButton.ClickAsync();
    }

    /// <summary>
    /// Click the "Components" link/button
    /// </summary>
    public async Task ClickComponentsAsync()
    {
        await ComponentsButton.ClickAsync();
    }

    /// <summary>
    /// Get the hero title text
    /// </summary>
    public async Task<string> GetHeroTitleAsync()
    {
        return await HeroTitle.TextContentAsync() ?? string.Empty;
    }

    /// <summary>
    /// Check if the page is loaded and ready
    /// </summary>
    public async Task<bool> IsLoadedAsync()
    {
        // Check if key elements are visible
        var logoVisible = await Logo.IsVisibleAsync();
        var heroVisible = await HeroTitle.IsVisibleAsync();
        return logoVisible && heroVisible;
    }
}
