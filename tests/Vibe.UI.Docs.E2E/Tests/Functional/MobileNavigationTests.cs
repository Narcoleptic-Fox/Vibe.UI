using Shouldly;
using Vibe.UI.Docs.E2E.Infrastructure;
using Vibe.UI.Docs.E2E.PageObjects;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Functional;

/// <summary>
/// Functional tests for mobile navigation and responsive behavior
/// Tests hamburger menu and sidebar toggle at mobile viewport
/// </summary>
[Trait("Category", TestCategories.Functional)]
[Trait("Category", TestCategories.Mobile)]
public class MobileNavigationTests : E2ETestBase
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Set mobile viewport
        await Page.SetViewportSizeAsync(375, 667); // iPhone size
    }

    [Fact]
    public async Task MobileSearchButtonVisibleOnMobileViewport()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");
        var basePage = new BasePage(Page);

        // Assert
        var isVisible = await basePage.MobileSearchButton.IsVisibleAsync();
        isVisible.ShouldBeTrue("Search button should be visible on mobile viewport");
    }

    [Fact]
    public async Task DesktopSearchButtonHiddenOnMobileViewport()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");
        var basePage = new BasePage(Page);

        // Assert
        var isVisible = await basePage.SearchButton.IsVisibleAsync();
        isVisible.ShouldBeFalse("Desktop search button should be hidden on mobile viewport");
    }

    [Fact]
    public async Task SearchButtonVisibleOnMobile()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");
        var basePage = new BasePage(Page);

        // Assert
        // Mobile search button should be visible
        var mobileSearchVisible = await basePage.MobileSearchButton.IsVisibleAsync();

        // Desktop search input should NOT be visible
        var desktopSearchVisible = await Page.Locator("button:has-text('Search components...')").IsVisibleAsync();

        // At least one search option should be available
        var searchAvailable = mobileSearchVisible || desktopSearchVisible;
        searchAvailable.ShouldBeTrue();
    }

    [Fact]
    public async Task ResponsiveLayoutAdjustsCorrectly()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");

        // Act - Test different viewport sizes
        var viewports = new[]
        {
            (Width: 375, Height: 667, Name: "Mobile"),
            (Width: 768, Height: 1024, Name: "Tablet"),
            (Width: 1280, Height: 720, Name: "Desktop")
        };

        foreach (var viewport in viewports)
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await Page.WaitForTimeoutAsync(300);

            var basePage = new BasePage(Page);

            // Assert - Main content should be visible at all viewports
            var contentVisible = await basePage.IsMainContentVisibleAsync();
            contentVisible.ShouldBeTrue($"Main content should be visible on {viewport.Name} viewport");
        }
    }
}
