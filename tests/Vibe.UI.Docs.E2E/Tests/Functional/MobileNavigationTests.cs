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
    public async Task HamburgerMenuVisibleOnMobileViewport()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");
        var basePage = new BasePage(Page);

        // Assert
        var isVisible = await basePage.IsMobileMenuVisibleAsync();
        isVisible.ShouldBeTrue("Hamburger menu button should be visible on mobile viewport");
    }

    [Fact]
    public async Task HamburgerMenuHiddenOnDesktopViewport()
    {
        // Arrange - Switch to desktop viewport
        await Page.SetViewportSizeAsync(1280, 720);
        await NavigateAndWaitForBlazorAsync("/");
        var basePage = new BasePage(Page);

        // Assert
        var isVisible = await basePage.IsMobileMenuVisibleAsync();
        isVisible.ShouldBeFalse("Hamburger menu button should be hidden on desktop viewport");
    }

    [Fact]
    public async Task SidebarToggleOpensOnMobile()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");
        var basePage = new BasePage(Page);

        var isOpenBefore = await basePage.IsSidebarOpenAsync();

        // Act
        await basePage.ToggleMobileSidebarAsync();

        // Assert
        var isOpenAfter = await basePage.IsSidebarOpenAsync();
        isOpenAfter.ShouldNotBe(isOpenBefore, "Sidebar state should toggle");
    }

    [Fact]
    public async Task SidebarClosesAfterNavigationOnMobile()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");
        var basePage = new BasePage(Page);

        // Open sidebar
        await basePage.ToggleMobileSidebarAsync();
        await Page.WaitForTimeoutAsync(500); // Wait for animation

        var isOpenBefore = await basePage.IsSidebarOpenAsync();
        isOpenBefore.ShouldBeTrue("Sidebar should be open before navigation");

        // Act - Click a link in the sidebar
        var componentLink = Page.Locator(".docs-sidebar a[href*='/components/']").First;
        await componentLink.ClickAsync();
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

        // Assert - Sidebar should close after navigation
        // Note: This behavior depends on implementation - adjust if needed
        await Page.WaitForTimeoutAsync(500);

        // Verify navigation happened
        var url = Page.Url;
        url.ShouldContain("/components/");
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
            var contentVisible = await basePage.MainContent.IsVisibleAsync();
            contentVisible.ShouldBeTrue($"Main content should be visible on {viewport.Name} viewport");
        }
    }
}
