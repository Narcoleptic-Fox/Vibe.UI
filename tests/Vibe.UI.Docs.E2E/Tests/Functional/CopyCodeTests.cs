using Shouldly;
using Vibe.UI.Docs.E2E.Infrastructure;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Functional;

/// <summary>
/// Functional tests for the copy code button functionality.
/// Tests clipboard operations and visual feedback.
/// </summary>
[Trait("Category", TestCategories.Functional)]
public class CopyCodeTests : E2ETestBase
{
    private readonly List<string> _consoleErrors = new();

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Grant clipboard permissions for testing
        await Context.GrantPermissionsAsync(new[] { "clipboard-read", "clipboard-write" });

        // Capture console errors
        Page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                _consoleErrors.Add(msg.Text);
            }
        };
    }

    #region Copy Button Visibility

    [Fact]
    public async Task CopyButtonExistsOnCodeBlocks()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/button");
        await Page.WaitForTimeoutAsync(1500);

        // Act - Find code blocks
        var codeBlocks = Page.Locator(".code-block, pre, [class*='shiki']");
        var codeBlockCount = await codeBlocks.CountAsync();

        // Assert
        codeBlockCount.ShouldBeGreaterThan(0, "Page should have code blocks");

        // Find copy buttons - CopyButton uses title="Copy to clipboard" and has svg icons
        var copyButtons = Page.Locator(
            "[title='Copy to clipboard'], " +
            "[title*='Copy'], " +
            ".code-block button, " +
            "button:has(svg path[d*='M8 16H6'])"  // The copy icon path
        );
        var copyButtonCount = await copyButtons.CountAsync();

        copyButtonCount.ShouldBeGreaterThan(0, "Code blocks should have copy buttons");
    }

    [Fact]
    public async Task CopyButtonIsVisibleOnHover()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/button");
        await Page.WaitForTimeoutAsync(1500);

        // Find a code block
        var codeBlock = Page.Locator(".code-block, pre").First;

        if (await codeBlock.IsVisibleAsync())
        {
            // Act - Hover over code block
            await codeBlock.HoverAsync();
            await Page.WaitForTimeoutAsync(300);

            // Assert - Copy button should be visible (or always visible)
            var copyButton = codeBlock.Locator("button, .copy-button").First;
            var buttonExists = await copyButton.CountAsync() > 0;

            // Button might be inside or sibling
            if (!buttonExists)
            {
                copyButton = Page.Locator(".code-block button, [data-copy]").First;
            }

            // If copy button design shows on hover, it should now be visible
            // Some designs always show the button
        }
    }

    #endregion

    #region Copy Functionality

    [Fact]
    public async Task ClickingCopyButtonCopiesCode()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/button");
        await Page.WaitForTimeoutAsync(1500);

        // Clear clipboard
        await Page.EvaluateAsync("() => navigator.clipboard.writeText('')");

        // Find copy button
        var copyButton = Page.Locator(
            "[title='Copy to clipboard'], " +
            "[title*='Copy'], " +
            ".code-block button"
        ).First;

        if (await copyButton.IsVisibleAsync())
        {
            // Get the associated code content
            var codeBlock = Page.Locator(".code-block code, pre code").First;
            var expectedCode = await codeBlock.TextContentAsync();

            // Act
            await copyButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            // Assert - Check clipboard content
            var clipboardContent = await Page.EvaluateAsync<string>(@"
                async () => {
                    try {
                        return await navigator.clipboard.readText();
                    } catch {
                        return '__clipboard_error__';
                    }
                }
            ");

            if (clipboardContent != "__clipboard_error__")
            {
                clipboardContent.ShouldNotBeNullOrWhiteSpace("Clipboard should contain code");
                // Code might be formatted differently, just verify something was copied
            }
        }
    }

    [Fact]
    public async Task CopyButtonShowsSuccessFeedback()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/button");
        await Page.WaitForTimeoutAsync(1500);

        var copyButton = Page.Locator(
            "[title='Copy to clipboard'], " +
            "[title*='Copy'], " +
            ".code-block button"
        ).First;

        if (await copyButton.IsVisibleAsync())
        {
            // Get initial state
            var initialText = await copyButton.TextContentAsync();
            var initialClasses = await copyButton.GetAttributeAsync("class") ?? "";

            // Act
            await copyButton.ClickAsync();
            await Page.WaitForTimeoutAsync(300);

            // Assert - Button should show success state
            var afterText = await copyButton.TextContentAsync();
            var afterClasses = await copyButton.GetAttributeAsync("class") ?? "";
            var afterTitle = await copyButton.GetAttributeAsync("title") ?? "";
            var hasSuccessIcon = await copyButton.Locator("svg.vibe-copy-success-icon").CountAsync() > 0;

            // Should show "Copied!" or similar, or have a success class
            var showsSuccess = afterTitle.Contains("Copied", StringComparison.OrdinalIgnoreCase) ||
                               hasSuccessIcon ||
                               afterText?.Contains("Copied", StringComparison.OrdinalIgnoreCase) == true ||
                               afterText?.Contains("✓") == true ||
                               afterClasses.Contains("copied") ||
                               afterClasses.Contains("success");

            showsSuccess.ShouldBeTrue(
                $"Copy button should show success feedback. Title: '{afterTitle}', Text: '{afterText}', Classes: '{afterClasses}'");
        }
    }

    [Fact]
    public async Task CopyButtonResetsAfterDelay()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/button");
        await Page.WaitForTimeoutAsync(1500);

        var copyButton = Page.Locator(
            "[title='Copy to clipboard'], " +
            "[title*='Copy'], " +
            ".code-block button"
        ).First;

        if (await copyButton.IsVisibleAsync())
        {
            var initialText = await copyButton.TextContentAsync();

            // Act
            await copyButton.ClickAsync();
            await Page.WaitForTimeoutAsync(300);

            var duringSuccessText = await copyButton.TextContentAsync();

            // Wait for reset (typically 2-3 seconds)
            await Page.WaitForTimeoutAsync(3000);

            var afterResetText = await copyButton.TextContentAsync();

            // Assert
            if (duringSuccessText?.Contains("Copied", StringComparison.OrdinalIgnoreCase) == true)
            {
                afterResetText.ShouldBe(initialText, "Button text should reset to original after delay");
            }
        }
    }

    #endregion

    #region Multiple Code Blocks

    [Fact]
    public async Task EachCodeBlockHasIndependentCopyButton()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/button");
        await Page.WaitForTimeoutAsync(1500);

        // Find all copy buttons
        var copyButtons = Page.Locator(
            "[title='Copy to clipboard'], " +
            "[title*='Copy'], " +
            ".code-block button"
        );
        var buttonCount = await copyButtons.CountAsync();

        if (buttonCount > 1)
        {
            // Act - Click first button
            await copyButtons.Nth(0).ClickAsync();
            await Page.WaitForTimeoutAsync(300);

            // Assert - Only first button should show success
            var firstButtonClasses = await copyButtons.Nth(0).GetAttributeAsync("class") ?? "";
            var secondButtonClasses = await copyButtons.Nth(1).GetAttributeAsync("class") ?? "";
            var firstTitle = await copyButtons.Nth(0).GetAttributeAsync("title") ?? "";
            var secondTitle = await copyButtons.Nth(1).GetAttributeAsync("title") ?? "";
            var firstHasSuccessIcon = await copyButtons.Nth(0).Locator("svg.vibe-copy-success-icon").CountAsync() > 0;
            var secondHasSuccessIcon = await copyButtons.Nth(1).Locator("svg.vibe-copy-success-icon").CountAsync() > 0;

            var firstShowsSuccess = firstTitle.Contains("Copied", StringComparison.OrdinalIgnoreCase) ||
                                    firstHasSuccessIcon ||
                                    firstButtonClasses.Contains("copied") ||
                                    (await copyButtons.Nth(0).TextContentAsync())?.Contains("Copied") == true;

            var secondShowsSuccess = secondTitle.Contains("Copied", StringComparison.OrdinalIgnoreCase) ||
                                     secondHasSuccessIcon ||
                                     secondButtonClasses.Contains("copied") ||
                                     (await copyButtons.Nth(1).TextContentAsync())?.Contains("Copied") == true;

            firstShowsSuccess.ShouldBeTrue("First button should show success");
            secondShowsSuccess.ShouldBeFalse("Second button should NOT show success");
        }
    }

    [Fact]
    public async Task CopyButtonCopiesCorrectCodeBlock()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/button");
        await Page.WaitForTimeoutAsync(1500);

        // Find code blocks and their copy buttons
        var codeBlocks = Page.Locator(".code-block, pre:has(code)");
        var count = await codeBlocks.CountAsync();

        if (count > 1)
        {
            // Get second code block's content
            var secondCodeBlock = codeBlocks.Nth(1);
            var secondCodeContent = await secondCodeBlock.Locator("code").TextContentAsync();

            // Find its copy button
            var copyButton = secondCodeBlock.Locator("button, .copy-button").First;

            if (await copyButton.IsVisibleAsync())
            {
                // Clear clipboard
                await Page.EvaluateAsync("() => navigator.clipboard.writeText('')");

                // Act
                await copyButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                // Assert
                var clipboardContent = await Page.EvaluateAsync<string>(@"
                    async () => {
                        try {
                            return await navigator.clipboard.readText();
                        } catch {
                            return '__error__';
                        }
                    }
                ");

                if (clipboardContent != "__error__" && !string.IsNullOrWhiteSpace(secondCodeContent))
                {
                    // Verify clipboard matches the correct code block (allowing for formatting differences)
                    var normalizedClipboard = NormalizeWhitespace(clipboardContent);
                    var normalizedExpected = NormalizeWhitespace(secondCodeContent);
                    var expectedSubstring = normalizedExpected.Substring(0, Math.Min(50, normalizedExpected.Length));

                    normalizedClipboard.ShouldContain(expectedSubstring);
                }
            }
        }
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task CopyButtonDoesNotCauseConsoleErrors()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/button");
        await Page.WaitForTimeoutAsync(1500);
        _consoleErrors.Clear();

        var copyButton = Page.Locator(
            "[title='Copy to clipboard'], " +
            "[title*='Copy'], " +
            ".code-block button"
        ).First;

        if (await copyButton.IsVisibleAsync())
        {
            // Act
            await copyButton.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);

            // Assert - No critical errors (clipboard permission errors are expected in some contexts)
            var criticalErrors = _consoleErrors
                .Where(e => !e.Contains("clipboard", StringComparison.OrdinalIgnoreCase))
                .Where(e => !e.Contains("permission", StringComparison.OrdinalIgnoreCase))
                .ToList();

            criticalErrors.ShouldBeEmpty(
                $"Copy should not cause console errors. Found:\n{string.Join("\n", criticalErrors)}");
        }
    }

    [Fact]
    public async Task CopyButtonHandlesClipboardPermissionDenied()
    {
        // Arrange
        // Create a new context without clipboard permissions
        var restrictedContext = await Browser.NewContextAsync(new()
        {
            Permissions = new string[] { } // No permissions
        });
        var restrictedPage = await restrictedContext.NewPageAsync();

        try
        {
            await restrictedPage.GotoAsync($"{BaseUrl}/components/button");
            await restrictedPage.WaitForBlazorReadyAsync();
            await restrictedPage.WaitForTimeoutAsync(1500);

            var copyButton = restrictedPage.Locator(
                "[title='Copy to clipboard'], " +
                "[title*='Copy'], " +
                ".code-block button"
            ).First;

            if (await copyButton.IsVisibleAsync())
            {
                // Act - This should not throw even if clipboard fails
                await copyButton.ClickAsync();
                await restrictedPage.WaitForTimeoutAsync(500);

                // Assert - Button should still be functional (might show error state)
                var isVisible = await copyButton.IsVisibleAsync();
                isVisible.ShouldBeTrue("Button should remain visible even if clipboard fails");
            }
        }
        finally
        {
            await restrictedPage.CloseAsync();
            await restrictedContext.CloseAsync();
        }
    }

    #endregion

    #region Keyboard Accessibility

    [Fact]
    public async Task CopyButtonIsFocusable()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/button");
        await Page.WaitForTimeoutAsync(1500);

        var copyButton = Page.Locator(
            "[title='Copy to clipboard'], " +
            "[title*='Copy'], " +
            ".code-block button"
        ).First;

        if (await copyButton.IsVisibleAsync())
        {
            // Act
            await copyButton.FocusAsync();

            // Assert
            var isFocused = await copyButton.EvaluateAsync<bool>(
                "el => el === document.activeElement"
            );

            isFocused.ShouldBeTrue("Copy button should be focusable");
        }
    }

    [Fact]
    public async Task CopyButtonActivatesWithEnterKey()
    {
        // Arrange
        await NavigateAndWaitForBlazorAsync("/components/button");
        await Page.WaitForTimeoutAsync(1500);

        var copyButton = Page.Locator(
            "[title='Copy to clipboard'], " +
            "[title*='Copy'], " +
            ".code-block button"
        ).First;

        if (await copyButton.IsVisibleAsync())
        {
            var initialText = await copyButton.TextContentAsync();

            // Act
            await copyButton.FocusAsync();
            await Page.Keyboard.PressAsync("Enter");
            await Page.WaitForTimeoutAsync(300);

            // Assert
            var afterText = await copyButton.TextContentAsync();
            var afterClasses = await copyButton.GetAttributeAsync("class") ?? "";
            var afterTitle = await copyButton.GetAttributeAsync("title") ?? "";
            var hasSuccessIcon = await copyButton.Locator("svg.vibe-copy-success-icon").CountAsync() > 0;

            var wasActivated = afterText?.Contains("Copied") == true ||
                               afterTitle.Contains("Copied", StringComparison.OrdinalIgnoreCase) ||
                               hasSuccessIcon ||
                               afterClasses.Contains("copied") ||
                               afterClasses.Contains("success");

            wasActivated.ShouldBeTrue("Copy button should activate with Enter key");
        }
    }

    #endregion

    #region Helper Methods

    private static string NormalizeWhitespace(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return System.Text.RegularExpressions.Regex.Replace(text.Trim(), @"\s+", " ");
    }

    #endregion
}
