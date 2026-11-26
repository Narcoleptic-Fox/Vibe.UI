using Microsoft.Playwright;
using Shouldly;
using Vibe.UI.Docs.E2E.Infrastructure;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Functional;

/// <summary>
/// The RadioGroup and ToggleGroup pages show their examples bound to enums with no items written.
/// These drive those examples and read back the bound value, so a page that quietly falls back to
/// strings, or a group that stops building its items from the enum, turns a test red.
/// </summary>
[Trait("Category", TestCategories.Functional)]
public class EnumGroupExamplesTests : E2ETestBase
{
    [Fact]
    public async Task RadioGroupBuildsItsOptionsFromTheEnumAndBindsTheValue()
    {
        await NavigateAndWaitForBlazorAsync("/components/radiogroup");
        var group = Page.GetByTestId("radio-enum-shipping").GetByRole(AriaRole.Radiogroup);

        (await group.GetByRole(AriaRole.Radio).AllAsync()).Count.ShouldBe(3);
        // The radio input is visually hidden under a styled control, so a person clicks the label.
        await group.GetByText("Next day", new() { Exact = true }).ClickAsync();

        await ShouldReadAsync("#radio-enum-shipping-value", "Value: Shipping.NextDay");
    }

    [Fact]
    public async Task NullableRadioGroupStartsWithNothingChecked()
    {
        await NavigateAndWaitForBlazorAsync("/components/radiogroup");
        var group = Page.GetByTestId("radio-enum-contact").GetByRole(AriaRole.Radiogroup);

        await ShouldReadAsync("#radio-enum-contact-value", "Value: null");
        foreach (var radio in await group.GetByRole(AriaRole.Radio).AllAsync())
        {
            (await radio.IsCheckedAsync()).ShouldBeFalse();
        }

        await group.GetByText("Text message", new() { Exact = true }).ClickAsync();
        await ShouldReadAsync("#radio-enum-contact-value", "Value: Contact.TextMessage");
    }

    [Fact]
    public async Task ToggleGroupBuildsItsItemsFromTheEnumForSeveralValues()
    {
        await NavigateAndWaitForBlazorAsync("/components/togglegroup");
        var group = Page.GetByTestId("toggle-enum-formats").GetByRole(AriaRole.Group);

        (await group.GetByRole(AriaRole.Button).AllInnerTextsAsync())
            .Select(text => text.Trim())
            .ShouldBe(["Bold", "Italic", "Underline", "Strikethrough", "Code"]);

        await group
            .GetByRole(AriaRole.Button, new() { Name = "Italic", Exact = true })
            .ClickAsync();
        await ShouldReadAsync("#toggle-enum-formats-value", "Active: Bold, Italic");
    }

    [Fact]
    public async Task NullableToggleGroupClearsWhenTheSelectedItemIsPressedAgain()
    {
        await NavigateAndWaitForBlazorAsync("/components/togglegroup");
        var group = Page.GetByRole(AriaRole.Group, new() { Name = "Text alignment", Exact = true });
        var left = group.GetByRole(AriaRole.Button, new() { Name = "Left", Exact = true });

        (await left.GetAttributeAsync("aria-pressed")).ShouldBe("true");
        await left.ClickAsync();

        await Page.GetByText("Alignment: None", new() { Exact = true }).WaitForAsync();
        (await left.GetAttributeAsync("aria-pressed")).ShouldBe("false");
    }

    private async Task ShouldReadAsync(string selector, string expected)
    {
        var element = Page.Locator(selector);
        for (var attempt = 0; attempt < 30; attempt++)
        {
            if ((await element.InnerTextAsync()).Trim() == expected)
            {
                return;
            }

            await Page.WaitForTimeoutAsync(100);
        }

        (await element.InnerTextAsync()).Trim().ShouldBe(expected);
    }
}
