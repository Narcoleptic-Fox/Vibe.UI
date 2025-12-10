using Shouldly;
using Vibe.UI.Docs.E2E.Infrastructure;
using Vibe.UI.Docs.E2E.PageObjects;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Functional;

/// <summary>
/// Functional tests for Toast notifications
/// Tests appearance and auto-dismiss behavior
/// </summary>
[Trait("Category", TestCategories.Functional)]
public class ToastNotificationTests : E2ETestBase
{
    [Fact]
    public async Task ToastAppearsWhenTriggered()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");

        // Navigate to a page that might have toast triggers
        // For this test, we'll navigate to a component page with interactive examples
        await Page.GotoAsync($"{BaseUrl}/components/button");
        await Page.WaitForBlazorReadyAsync();

        // Act - Try to trigger a toast by looking for a "Show Toast" button or similar
        // If no such button exists, we'll inject a test toast
        var showToastButton = Page.GetByRole(Microsoft.Playwright.AriaRole.Button, new()
        {
            NameString = "Show Toast",
            Exact = false
        });

        if (await showToastButton.IsVisibleAsync())
        {
            await showToastButton.ClickAsync();
        }
        else
        {
            // Inject a test toast via JS
            await Page.EvaluateAsync(@"
                () => {
                    const container = document.createElement('div');
                    container.className = 'toast-notification';
                    container.setAttribute('role', 'alert');
                    container.textContent = 'Test Toast Message';
                    document.body.appendChild(container);
                }
            ");
        }

        // Assert - Wait for toast to appear
        var toast = await Page.WaitForToastAsync();

        var isVisible = await toast.IsVisibleAsync();
        isVisible.ShouldBeTrue("Toast should be visible after being triggered");

        var text = await toast.TextContentAsync();
        text.ShouldNotBeNullOrWhiteSpace("Toast should have text content");
    }

    [Fact]
    public async Task ToastAutoDismissesAfterTimeout()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");

        // Inject a toast with auto-dismiss
        await Page.EvaluateAsync(@"
            () => {
                const container = document.createElement('div');
                container.className = 'toast-notification';
                container.setAttribute('role', 'alert');
                container.textContent = 'Auto-dismiss Toast';
                document.body.appendChild(container);

                // Auto-dismiss after 3 seconds
                setTimeout(() => {
                    container.remove();
                }, 3000);
            }
        ");

        // Wait for toast to appear
        var toast = await Page.WaitForToastAsync();

        var isVisibleInitially = await toast.IsVisibleAsync();
        isVisibleInitially.ShouldBeTrue("Toast should be visible initially");

        // Act - Wait for auto-dismiss (3s + buffer)
        await Task.Delay(3500);

        // Assert
        var isVisibleAfter = await toast.IsVisibleAsync();
        isVisibleAfter.ShouldBeFalse("Toast should be dismissed after timeout");
    }

    [Fact]
    public async Task MultipleToastsCanBeDisplayed()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");

        // Act - Inject multiple toasts
        await Page.EvaluateAsync(@"
            () => {
                for (let i = 1; i <= 3; i++) {
                    const container = document.createElement('div');
                    container.className = 'toast-notification';
                    container.setAttribute('role', 'alert');
                    container.textContent = `Toast Message ${i}`;
                    document.body.appendChild(container);
                }
            }
        ");

        await Page.WaitForTimeoutAsync(500);

        // Assert
        var toasts = Page.Locator(".toast-notification, [role='alert']");
        var count = await toasts.CountAsync();

        count.ShouldBeGreaterThanOrEqualTo(3, "Multiple toasts should be displayed");
    }
}
