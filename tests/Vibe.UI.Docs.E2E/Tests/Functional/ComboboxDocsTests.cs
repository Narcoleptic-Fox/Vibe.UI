using Microsoft.Playwright;
using Shouldly;
using Vibe.UI.Docs.E2E.Infrastructure;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Functional;

/// <summary>
/// The Combobox page's live examples, driven in a browser. Each example prints its bound value
/// next to it, so these check what the component reports, not only what it shows.
/// </summary>
[Trait("Category", TestCategories.Functional)]
public class ComboboxDocsTests : E2ETestBase
{
    [Fact]
    public async Task PickingATextOptionBindsItsValue()
    {
        await NavigateAndWaitForBlazorAsync("/components/combobox");

        await PickAsync("combobox-framework", "Vue");

        await ShouldShowAsync("combobox-framework-value", "Value: vue");
        (await Page.Locator("#combobox-framework").InputValueAsync()).ShouldBe("Vue");
    }

    [Fact]
    public async Task TypedOptionBindsTheGuidAndEmptyingTheBoxGivesNull()
    {
        await NavigateAndWaitForBlazorAsync("/components/combobox");
        await ShouldShowAsync("combobox-owner-value", "Value: null");

        await PickAsync("combobox-owner", "Kenji Mori");
        await ShouldShowAsync(
            "combobox-owner-value",
            "Value: 0b9e4a77-5c2d-4f8e-8b13-6a2e9d1c7f42"
        );

        await Page.Locator("#combobox-owner").FillAsync(string.Empty);
        await ShouldShowAsync("combobox-owner-value", "Value: null");
    }

    [Fact]
    public async Task EnumWithNoOptionsListsItsValues()
    {
        await NavigateAndWaitForBlazorAsync("/components/combobox");

        await Page.Locator("#combobox-state").ClickAsync();
        var listbox = Page.Locator("#combobox-state-listbox");
        await listbox.WaitForAsync();
        (await listbox.GetByRole(AriaRole.Option).AllInnerTextsAsync())
            .Select(text => text.Trim())
            .ShouldBe(["Not started", "In progress", "Blocked", "Done"]);

        await listbox
            .GetByRole(AriaRole.Option, new() { Name = "In progress", Exact = true })
            .ClickAsync();
        await ShouldShowAsync("combobox-state-value", "Value: TaskState.InProgress");
    }

    [Fact]
    public async Task FreeTextIsConvertedToTheValueTypeAndTextThatDoesNotConvertIsIgnored()
    {
        await NavigateAndWaitForBlazorAsync("/components/combobox");
        var pageSize = Page.Locator("#combobox-page-size");

        await pageSize.FillAsync("40");
        await pageSize.PressAsync("Enter");
        await ShouldShowAsync("combobox-page-size-value", "Value: 40");

        await pageSize.FillAsync("abc");
        await pageSize.PressAsync("Enter");
        await Page.WaitForTimeoutAsync(200);
        await ShouldShowAsync("combobox-page-size-value", "Value: 40");
    }

    [Fact]
    public async Task ClearableStringAcceptsFreeTextAndEmptiesToNull()
    {
        await NavigateAndWaitForBlazorAsync("/components/combobox");
        var city = Page.Locator("#combobox-city");

        await city.FillAsync("Nairobi");
        await city.PressAsync("Enter");
        await ShouldShowAsync("combobox-city-value", "Value: Nairobi");

        await city.FillAsync(string.Empty);
        await ShouldShowAsync("combobox-city-value", "Value: null");
    }

    private async Task PickAsync(string comboboxId, string optionLabel)
    {
        await Page.Locator($"#{comboboxId}").ClickAsync();
        var listbox = Page.Locator($"#{comboboxId}-listbox");
        await listbox.WaitForAsync();
        await listbox
            .GetByRole(AriaRole.Option, new() { Name = optionLabel, Exact = true })
            .ClickAsync();
        await listbox.WaitForAsync(new() { State = WaitForSelectorState.Detached });
    }

    private async Task ShouldShowAsync(string outputId, string expected)
    {
        var output = Page.Locator($"#{outputId}");
        try
        {
            await Page.WaitForFunctionAsync(
                "([id, text]) => document.getElementById(id)?.textContent.trim() === text",
                new object[] { outputId, expected },
                new() { Timeout = 5000 }
            );
        }
        catch (TimeoutException)
        {
            // Fall through so the failure message shows what was there instead.
        }

        (await output.InnerTextAsync()).Trim().ShouldBe(expected);
    }
}
