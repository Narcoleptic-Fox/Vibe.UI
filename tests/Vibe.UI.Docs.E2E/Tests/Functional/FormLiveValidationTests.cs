using Microsoft.Playwright;
using Vibe.UI.Docs.E2E.Infrastructure;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Functional;

/// <summary>
/// The Form page's preview in a real browser: typed inputs tell the form when a field changes, so
/// a message clears when the field is put right, and a number being typed is not judged halfway.
/// Keys are pressed one at a time; a burst hides what happens between them.
/// </summary>
[Trait("Category", TestCategories.Functional)]
public class FormLiveValidationTests : E2ETestBase
{
    private ILocator Name => Page.Locator("#name");

    private ILocator TeamSize => Page.Locator("#team-size");

    private ILocator Error(string fieldId) => Page.Locator($"#{fieldId}-error");

    [Fact]
    public async Task AMessageAppearsAndClearsAsTheFieldIsLeft()
    {
        // No submit here: the fields carry the required attribute, so the browser's own check
        // stops an empty form before Blazor sees it (checked: with noValidate set on the form,
        // the same click shows both messages). Every message below comes from the form being
        // told a field changed.
        await NavigateAndWaitForBlazorAsync("/components/form");

        await Name.ClickAsync();
        await Name.PressSequentiallyAsync("Ada", new() { Delay = 60 });
        await Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(Error("name")).ToHaveCountAsync(0);

        await Name.FillAsync(string.Empty);
        await Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(Error("name")).ToHaveTextAsync("Name is required");

        await Name.ClickAsync();
        await Name.PressSequentiallyAsync("Grace", new() { Delay = 60 });
        // Enter commits the text as leaving the field does.
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Error("name")).ToHaveCountAsync(0);

        // A field that was never touched has not been judged.
        await Assertions.Expect(Error("team-size")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task ANumberIsJudgedWhenItIsLeftNotWhileItIsTyped()
    {
        await NavigateAndWaitForBlazorAsync("/components/form");

        await TeamSize.ClickAsync();
        await Page.Keyboard.PressAsync("7");
        await Page.Keyboard.PressAsync("0");
        await Page.Keyboard.PressAsync("0");
        await Page.WaitForTimeoutAsync(300);

        // 700 is out of range, but it may be on its way to something else: nothing yet.
        await Assertions.Expect(Error("team-size")).ToHaveCountAsync(0);

        await Page.Keyboard.PressAsync("Tab");
        await Assertions
            .Expect(Error("team-size"))
            .ToHaveTextAsync("Team size must be between 1 and 500");

        await TeamSize.ClickAsync();
        await Page.Keyboard.PressAsync("Backspace");
        await Page.WaitForTimeoutAsync(300);

        // 70 is fine, but the message stays until the field is left: it is judged once, there.
        await Assertions
            .Expect(Error("team-size"))
            .ToHaveTextAsync("Team size must be between 1 and 500");

        await Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(Error("team-size")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task AHalfTypedNegativeNumberKeepsItsMinusSignAndRaisesNoMessage()
    {
        await NavigateAndWaitForBlazorAsync("/components/form");

        await TeamSize.ClickAsync();
        await Page.Keyboard.PressAsync("-");
        await Page.WaitForTimeoutAsync(300);

        await Assertions.Expect(Error("team-size")).ToHaveCountAsync(0);

        await Page.Keyboard.PressAsync("5");
        await Assertions.Expect(TeamSize).ToHaveValueAsync("-5");
        await Page.Keyboard.PressAsync("Tab");

        await Assertions
            .Expect(Error("team-size"))
            .ToHaveTextAsync("Team size must be between 1 and 500");
    }
}
