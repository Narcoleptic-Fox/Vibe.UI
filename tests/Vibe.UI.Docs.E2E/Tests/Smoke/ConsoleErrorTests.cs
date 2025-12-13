using Shouldly;
using Vibe.UI.Docs.E2E.Infrastructure;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Smoke;

/// <summary>
/// Tests that capture and fail on browser console errors.
/// These tests catch JavaScript exceptions, failed network requests, and other runtime errors
/// that don't break the page but indicate hidden problems.
/// </summary>
[Trait("Category", TestCategories.Smoke)]
public class ConsoleErrorTests : E2ETestBase
{
    private readonly List<string> _consoleErrors = new();
    private readonly List<string> _consoleWarnings = new();
    private readonly List<string> _failedRequests = new();

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Capture console messages
        Page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                _consoleErrors.Add($"[{msg.Type}] {msg.Text}");
            }
            else if (msg.Type == "warning")
            {
                _consoleWarnings.Add($"[{msg.Type}] {msg.Text}");
            }
        };

        // Capture failed requests
        Page.RequestFailed += (_, request) =>
        {
            _failedRequests.Add($"[{request.Method}] {request.Url} - {request.Failure}");
        };

        // Capture page errors (uncaught exceptions)
        Page.PageError += (_, error) =>
        {
            _consoleErrors.Add($"[PageError] {error}");
        };
    }

    [Fact]
    public async Task HomePageHasNoConsoleErrors()
    {
        // Arrange & Act
        await NavigateAndWaitForBlazorAsync("/");

        // Wait for any async operations
        await Page.WaitForTimeoutAsync(2000);

        // Assert
        var criticalErrors = FilterCriticalErrors(_consoleErrors);
        criticalErrors.ShouldBeEmpty(
            $"Home page should have no console errors. Found:\n{string.Join("\n", criticalErrors)}");
    }

    [Fact]
    public async Task HomePageHasNoFailedRequests()
    {
        // Arrange & Act
        await NavigateAndWaitForBlazorAsync("/");

        // Wait for network to settle
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

        // Assert
        var criticalFailures = FilterCriticalNetworkFailures(_failedRequests);
        criticalFailures.ShouldBeEmpty(
            $"Home page should have no failed requests. Found:\n{string.Join("\n", criticalFailures)}");
    }

    [Theory]
    [InlineData("/components/button")]
    [InlineData("/components/card")]
    [InlineData("/components/alert")]
    [InlineData("/components/modal")]
    [InlineData("/components/toast")]
    [InlineData("/components/dialog")]
    [InlineData("/components/tabs")]
    [InlineData("/components/dropdown")]
    public async Task ComponentPageHasNoConsoleErrors(string path)
    {
        // Arrange
        ClearCapturedMessages();

        // Act
        await NavigateAndWaitForBlazorAsync(path);
        await Page.WaitForTimeoutAsync(2000);

        // Assert
        var criticalErrors = FilterCriticalErrors(_consoleErrors);
        criticalErrors.ShouldBeEmpty(
            $"Page {path} should have no console errors. Found:\n{string.Join("\n", criticalErrors)}");
    }

    [Fact]
    public async Task ThemeToggleDoesNotCauseErrors()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");
        ClearCapturedMessages();

        // Act - Toggle theme multiple times
        var themeToggle = Page.Locator("[data-theme-toggle], .theme-toggle, button:has([class*='sun']), button:has([class*='moon'])").First;

        if (await themeToggle.IsVisibleAsync())
        {
            for (int i = 0; i < 3; i++)
            {
                await themeToggle.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }
        }

        // Assert
        var criticalErrors = FilterCriticalErrors(_consoleErrors);
        criticalErrors.ShouldBeEmpty(
            $"Theme toggle should not cause errors. Found:\n{string.Join("\n", criticalErrors)}");
    }

    [Fact]
    public async Task NavigationDoesNotCauseErrors()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");
        ClearCapturedMessages();

        // Act - Navigate to several pages
        var links = new[] { "/components/button", "/components/card", "/" };

        foreach (var link in links)
        {
            await Page.GotoAsync($"{BaseUrl}{link}");
            await Page.WaitForBlazorReadyAsync();
            await Page.WaitForTimeoutAsync(500);
        }

        // Assert
        var criticalErrors = FilterCriticalErrors(_consoleErrors);
        criticalErrors.ShouldBeEmpty(
            $"Navigation should not cause errors. Found:\n{string.Join("\n", criticalErrors)}");
    }

    [Fact]
    public async Task CommandPaletteDoesNotCauseErrors()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/");
        ClearCapturedMessages();

        // Act - Open and close command palette
        await Page.Keyboard.PressAsync("Control+k");
        await Page.WaitForTimeoutAsync(500);

        // Type a search
        await Page.Keyboard.TypeAsync("button");
        await Page.WaitForTimeoutAsync(500);

        // Close
        await Page.Keyboard.PressAsync("Escape");
        await Page.WaitForTimeoutAsync(300);

        // Assert
        var criticalErrors = FilterCriticalErrors(_consoleErrors);
        criticalErrors.ShouldBeEmpty(
            $"Command palette should not cause errors. Found:\n{string.Join("\n", criticalErrors)}");
    }

    [Fact]
    public async Task NoUnhandledPromiseRejections()
    {
        // Arrange
        var unhandledRejections = new List<string>();

        await Page.EvaluateAsync(@"
            window._unhandledRejections = [];
            window.addEventListener('unhandledrejection', (event) => {
                window._unhandledRejections.push(event.reason?.toString() || 'Unknown rejection');
            });
        ");

        // Act - Navigate and interact
        await NavigateAndWaitForBlazorAsync("/");
        await Page.WaitForTimeoutAsync(2000);

        // Get unhandled rejections
        var rejections = await Page.EvaluateAsync<string[]>("() => window._unhandledRejections || []");

        // Assert
        var filtered = rejections?.Where(r => !IsIgnorableRejection(r)).ToList() ?? new List<string>();
        filtered.ShouldBeEmpty(
            $"Should have no unhandled promise rejections. Found:\n{string.Join("\n", filtered)}");
    }

    #region Helper Methods

    private void ClearCapturedMessages()
    {
        _consoleErrors.Clear();
        _consoleWarnings.Clear();
        _failedRequests.Clear();
    }

    private static List<string> FilterCriticalErrors(List<string> errors)
    {
        // Filter out known non-critical errors
        return errors.Where(e => !IsIgnorableError(e)).ToList();
    }

    private static List<string> FilterCriticalNetworkFailures(List<string> failures)
    {
        // Filter out known non-critical failures
        return failures.Where(f => !IsIgnorableNetworkFailure(f)).ToList();
    }

    private static bool IsIgnorableError(string error)
    {
        var ignorable = new[]
        {
            // Browser extensions
            "chrome-extension://",
            "moz-extension://",

            // Known Blazor/WASM warnings that aren't critical
            "Blazor reconnection",
            "WebSocket connection",
            "Failed to fetch dynamically imported module",

            // Source map issues
            "Failed to load source map",
            "DevTools failed to load",

            // Third-party script issues
            "third_party",
            "analytics",
            "tracking",

            // Favicon
            "favicon.ico"
        };

        return ignorable.Any(i => error.Contains(i, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsIgnorableNetworkFailure(string failure)
    {
        var ignorable = new[]
        {
            // Common non-critical failures
            "favicon.ico",
            "analytics",
            "tracking",
            "fonts.googleapis",
            "fonts.gstatic",

            // CORS preflight that might fail
            "OPTIONS"
        };

        return ignorable.Any(i => failure.Contains(i, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsIgnorableRejection(string rejection)
    {
        var ignorable = new[]
        {
            // Known non-critical rejections
            "ResizeObserver loop",
            "Script error",
            "ChunkLoadError"
        };

        return ignorable.Any(i => rejection.Contains(i, StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}
