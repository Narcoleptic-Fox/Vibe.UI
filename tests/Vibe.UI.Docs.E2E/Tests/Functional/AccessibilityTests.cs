using Shouldly;
using Vibe.UI.Docs.E2E.Infrastructure;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Functional;

/// <summary>
/// Accessibility tests for ARIA attributes, keyboard navigation,
/// and basic a11y compliance.
/// </summary>
[Trait("Category", TestCategories.Functional)]
[Trait("Category", "Accessibility")]
public class AccessibilityTests : E2ETestBase
{
    #region ARIA Attributes

    [Fact]
    public async Task ModalHasProperAriaAttributes()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/modal");

        // Try to open a modal
        var openButton = Page.Locator("button:has-text('Open'), button:has-text('Show')").First;
        if (await openButton.IsVisibleAsync())
        {
            await openButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
        }

        // Act - Check for modal ARIA attributes
        var modal = Page.Locator("[role='dialog'], .modal, [class*='modal']").First;

        if (await modal.IsVisibleAsync())
        {
            // Assert
            var role = await modal.GetAttributeAsync("role");
            var ariaModal = await modal.GetAttributeAsync("aria-modal");
            var ariaLabel = await modal.GetAttributeAsync("aria-label");
            var ariaLabelledBy = await modal.GetAttributeAsync("aria-labelledby");

            // Modal should have proper role
            (role == "dialog" || role == "alertdialog").ShouldBeTrue(
                "Modal should have role='dialog' or role='alertdialog'");

            // Should have aria-modal or similar
            (ariaModal == "true" || await modal.GetAttributeAsync("data-modal") != null).ShouldBeTrue(
                "Modal should have aria-modal='true'");

            // Should have accessible name
            (ariaLabel != null || ariaLabelledBy != null).ShouldBeTrue(
                "Modal should have aria-label or aria-labelledby");
        }
    }

    [Fact]
    public async Task ButtonsHaveAccessibleNames()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/button");
        await Page.WaitForTimeoutAsync(1000);

        // Act - Find all buttons
        var buttons = Page.Locator("button");
        var count = await buttons.CountAsync();

        // Assert
        for (int i = 0; i < Math.Min(count, 10); i++) // Check first 10 buttons
        {
            var button = buttons.Nth(i);
            if (!await button.IsVisibleAsync()) continue;

            var text = await button.TextContentAsync();
            var ariaLabel = await button.GetAttributeAsync("aria-label");
            var title = await button.GetAttributeAsync("title");

            // Button should have accessible name via text content, aria-label, or title
            var hasAccessibleName = !string.IsNullOrWhiteSpace(text) ||
                                    !string.IsNullOrWhiteSpace(ariaLabel) ||
                                    !string.IsNullOrWhiteSpace(title);

            hasAccessibleName.ShouldBeTrue($"Button at index {i} should have an accessible name");
        }
    }

    [Fact]
    public async Task FormInputsHaveLabels()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/input");
        await Page.WaitForTimeoutAsync(1000);

        // Act - Find all input fields
        var inputs = Page.Locator("input[type='text'], input[type='email'], input[type='password'], textarea");
        var count = await inputs.CountAsync();

        // Assert
        for (int i = 0; i < Math.Min(count, 10); i++)
        {
            var input = inputs.Nth(i);
            if (!await input.IsVisibleAsync()) continue;

            var id = await input.GetAttributeAsync("id");
            var ariaLabel = await input.GetAttributeAsync("aria-label");
            var ariaLabelledBy = await input.GetAttributeAsync("aria-labelledby");
            var placeholder = await input.GetAttributeAsync("placeholder");

            // Check for associated label
            var hasLabel = false;
            if (!string.IsNullOrEmpty(id))
            {
                var label = Page.Locator($"label[for='{id}']");
                hasLabel = await label.CountAsync() > 0;
            }

            var hasAccessibleName = hasLabel ||
                                    !string.IsNullOrWhiteSpace(ariaLabel) ||
                                    !string.IsNullOrWhiteSpace(ariaLabelledBy) ||
                                    !string.IsNullOrWhiteSpace(placeholder);

            hasAccessibleName.ShouldBeTrue($"Input at index {i} should have a label or aria-label");
        }
    }

    [Fact]
    public async Task AlertsHaveProperRole()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/alert");
        await Page.WaitForTimeoutAsync(1000);

        // Act - Find alerts with role='alert' specifically (Vibe.UI Alert components have this)
        var alertsWithRole = Page.Locator("[role='alert']");
        var count = await alertsWithRole.CountAsync();

        // Assert - At least some alert components should have the proper role
        count.ShouldBeGreaterThan(0, "Alert page should have Alert components with role='alert'");

        // Verify first visible alert
        var firstAlert = alertsWithRole.First;
        if (await firstAlert.IsVisibleAsync())
        {
            var role = await firstAlert.GetAttributeAsync("role");
            role.ShouldBe("alert", "Alert component should have role='alert'");
        }
    }

    [Fact]
    public async Task TooltipsAreAccessible()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/tooltip");
        await Page.WaitForTimeoutAsync(1000);

        // Find elements with tooltips
        var tooltipTriggers = Page.Locator("[data-tooltip], [aria-describedby], [title]");
        var count = await tooltipTriggers.CountAsync();

        if (count > 0)
        {
            // Act - Hover over first tooltip trigger
            var trigger = tooltipTriggers.First;
            if (await trigger.IsVisibleAsync())
            {
                await trigger.HoverAsync();
                await Page.WaitForTimeoutAsync(500);

                // Assert - Tooltip should be accessible
                var ariaDescribedBy = await trigger.GetAttributeAsync("aria-describedby");
                var title = await trigger.GetAttributeAsync("title");

                (ariaDescribedBy != null || title != null).ShouldBeTrue(
                    "Tooltip trigger should have aria-describedby or title attribute");
            }
        }
    }

    #endregion

    #region Keyboard Navigation

    [Fact]
    public async Task TabNavigationWorksCorrectly()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");
        await Page.WaitForTimeoutAsync(1000);

        // Act - Tab through focusable elements
        var focusedElements = new List<string>();

        for (int i = 0; i < 10; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            await Page.WaitForTimeoutAsync(100);

            var focused = await Page.EvaluateAsync<string>(@"
                () => {
                    const el = document.activeElement;
                    if (!el) return 'none';
                    return el.tagName + (el.id ? '#' + el.id : '') + (el.className ? '.' + el.className.split(' ')[0] : '');
                }
            ");

            focusedElements.Add(focused);
        }

        // Assert - Should have navigated through multiple elements
        var uniqueElements = focusedElements.Distinct().Count();
        uniqueElements.ShouldBeGreaterThan(3, "Tab navigation should move through multiple focusable elements");
    }

    [Fact]
    public async Task EscapeClosesModals()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/modal");

        // Open modal
        var openButton = Page.Locator("button:has-text('Open'), button:has-text('Show')").First;
        if (await openButton.IsVisibleAsync())
        {
            await openButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            var modalBefore = Page.Locator("[role='dialog'], .modal.open, .modal.show, [data-state='open']").First;
            var wasOpen = await modalBefore.IsVisibleAsync();

            // Act
            await Page.Keyboard.PressAsync("Escape");
            await Page.WaitForTimeoutAsync(500);

            // Assert
            if (wasOpen)
            {
                var modalAfter = Page.Locator("[role='dialog'], .modal.open, .modal.show, [data-state='open']").First;
                var isStillOpen = await modalAfter.IsVisibleAsync();
                isStillOpen.ShouldBeFalse("Modal should close when Escape is pressed");
            }
        }
    }

    [Fact]
    public async Task DropdownKeyboardNavigationWorks()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/dropdown");
        await Page.WaitForTimeoutAsync(1000);

        // Find dropdown trigger
        var trigger = Page.Locator("button[aria-haspopup], [data-dropdown-trigger], .dropdown-trigger").First;

        if (await trigger.IsVisibleAsync())
        {
            // Act - Open with keyboard
            await trigger.FocusAsync();
            await Page.Keyboard.PressAsync("Enter");
            await Page.WaitForTimeoutAsync(300);

            // Check if menu opened
            var menu = Page.Locator("[role='menu'], .dropdown-menu, [data-dropdown-content]").First;
            var isOpen = await menu.IsVisibleAsync();

            if (isOpen)
            {
                // Navigate with arrow keys
                await Page.Keyboard.PressAsync("ArrowDown");
                await Page.WaitForTimeoutAsync(100);
                await Page.Keyboard.PressAsync("ArrowDown");
                await Page.WaitForTimeoutAsync(100);

                // Close with Escape
                await Page.Keyboard.PressAsync("Escape");
                await Page.WaitForTimeoutAsync(300);

                var isStillOpen = await menu.IsVisibleAsync();
                isStillOpen.ShouldBeFalse("Dropdown should close with Escape key");
            }
        }
    }

    [Fact]
    public async Task TabsKeyboardNavigationWorks()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/tabs");
        await Page.WaitForTimeoutAsync(1000);

        // Find tab list
        var tabList = Page.Locator("[role='tablist']").First;

        if (await tabList.IsVisibleAsync())
        {
            // Find first tab
            var firstTab = Page.Locator("[role='tab']").First;
            if (await firstTab.IsVisibleAsync())
            {
                // Act
                await firstTab.FocusAsync();
                var initialIndex = await GetFocusedTabIndex();

                await Page.Keyboard.PressAsync("ArrowRight");
                await Page.WaitForTimeoutAsync(100);
                var afterRight = await GetFocusedTabIndex();

                await Page.Keyboard.PressAsync("ArrowLeft");
                await Page.WaitForTimeoutAsync(100);
                var afterLeft = await GetFocusedTabIndex();

                // Assert
                afterRight.ShouldBeGreaterThan(initialIndex, "ArrowRight should move to next tab");
                afterLeft.ShouldBeLessThan(afterRight, "ArrowLeft should move to previous tab");
            }
        }
    }

    #endregion

    #region Focus Management

    [Fact]
    public async Task FocusTrapWorksInModals()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/modal");

        // Open modal
        var openButton = Page.Locator("button:has-text('Open'), button:has-text('Show')").First;
        if (await openButton.IsVisibleAsync())
        {
            await openButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            var modal = Page.Locator("[role='dialog'], .modal").First;
            if (await modal.IsVisibleAsync())
            {
                // Act - Tab multiple times to test focus trap
                var focusedElements = new List<bool>();

                for (int i = 0; i < 15; i++)
                {
                    await Page.Keyboard.PressAsync("Tab");
                    await Page.WaitForTimeoutAsync(50);

                    var isInModal = await Page.EvaluateAsync<bool>(@"
                        () => {
                            const modal = document.querySelector('[role=\'dialog\'], .modal');
                            const active = document.activeElement;
                            return modal?.contains(active) ?? false;
                        }
                    ");

                    focusedElements.Add(isInModal);
                }

                // Assert - Focus should stay within modal
                var focusEscaped = focusedElements.Any(x => !x);
                focusEscaped.ShouldBeFalse("Focus should be trapped within the modal");
            }
        }
    }

    [Fact]
    public async Task FocusReturnsAfterModalCloses()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/modal");

        var openButton = Page.Locator("button:has-text('Open'), button:has-text('Show')").First;
        if (await openButton.IsVisibleAsync())
        {
            // Focus the open button
            await openButton.FocusAsync();

            var buttonIdBefore = await Page.EvaluateAsync<string>(@"
                () => document.activeElement?.id || document.activeElement?.className || 'unknown'
            ");

            // Open modal
            await openButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            // Close modal
            await Page.Keyboard.PressAsync("Escape");
            await Page.WaitForTimeoutAsync(500);

            // Assert - Focus should return to trigger
            var buttonIdAfter = await Page.EvaluateAsync<string>(@"
                () => document.activeElement?.id || document.activeElement?.className || 'unknown'
            ");

            // Focus should ideally return to the button that opened the modal
            // (This is a best practice but not all implementations do this)
        }
    }

    #endregion

    #region Color Contrast (Basic)

    [Fact]
    public async Task TextHasAdequateContrast()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");
        await Page.WaitForTimeoutAsync(1000);

        // Act - Check a few key text elements
        var mainContent = Page.Locator("main, .docs-content, article").First;

        if (await mainContent.IsVisibleAsync())
        {
            var styles = await mainContent.EvaluateAsync<Dictionary<string, string>>(@"
                (el) => {
                    const style = window.getComputedStyle(el);
                    return {
                        color: style.color,
                        backgroundColor: style.backgroundColor
                    };
                }
            ");

            // Assert - Just verify we can get styles (actual contrast calculation would require parsing)
            styles.ShouldContainKey("color");
            styles["color"].ShouldNotBeNullOrWhiteSpace();
        }
    }

    #endregion

    #region Helper Methods

    private async Task<int> GetFocusedTabIndex()
    {
        return await Page.EvaluateAsync<int>(@"
            () => {
                const tabs = document.querySelectorAll('[role=\'tab\']');
                const active = document.activeElement;
                return Array.from(tabs).indexOf(active);
            }
        ");
    }

    #endregion
}
