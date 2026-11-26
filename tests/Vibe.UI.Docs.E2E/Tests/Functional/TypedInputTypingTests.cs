using Microsoft.Playwright;
using Shouldly;
using Vibe.UI.Docs.E2E.Infrastructure;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Functional;

/// <summary>
/// What only a real browser can show: a number box reports an empty value while it holds a
/// half-typed "-", and a render that echoes that back erases the minus sign. Component tests feed
/// the handler any string and never see it. Keys are pressed one at a time, with a render between
/// them, because a burst of keys hides the fault.
/// </summary>
[Trait("Category", TestCategories.Functional)]
public class TypedInputTypingTests : E2ETestBase
{
    private const string Readout = "#typed-readout";

    private async Task<ILocator> FocusEmptiedAsync(string id)
    {
        await NavigateAndWaitForBlazorAsync("/components/input");
        var input = Page.Locator($"#{id}");
        await input.ScrollIntoViewIfNeededAsync();
        await input.FocusAsync();
        await input.PressAsync("ControlOrMeta+a");
        return input;
    }

    private async Task PressAndLetRenderAsync(ILocator input, string key)
    {
        await input.PressAsync(key);
        // Two frames: one for the event to reach .NET, one for the render to come back.
        await Page.EvaluateAsync(
            "() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(() => setTimeout(done, 150))))"
        );
    }

    [Fact]
    public async Task NegativeDecimalTypedOneKeyAtATimeKeepsItsMinusSignAndTrailingZero()
    {
        var price = await FocusEmptiedAsync("typed-price");

        await PressAndLetRenderAsync(price, "-");
        (await price.EvaluateAsync<bool>("element => element.validity.badInput")).ShouldBeTrue(
            "the half-typed minus sign should still be in the box"
        );

        foreach (var key in new[] { "2", ".", "5", "0" })
        {
            await PressAndLetRenderAsync(price, key);
        }

        (await price.InputValueAsync()).ShouldBe("-2.50");
        (await Page.Locator(Readout).InnerTextAsync()).ShouldContain("price = -2.50");
    }

    [Fact]
    public async Task ClearingARequiredNumberKeepsTheValueAndSaysWhatIsWrongOnBlur()
    {
        var quantity = await FocusEmptiedAsync("typed-quantity");

        await PressAndLetRenderAsync(quantity, "Delete");
        (await quantity.GetAttributeAsync("aria-invalid")).ShouldBeNull();

        await PressAndLetRenderAsync(quantity, "Tab");

        (await quantity.GetAttributeAsync("aria-invalid")).ShouldBe("true");
        (await quantity.InputValueAsync()).ShouldBe(string.Empty);
        (await Page.Locator(Readout).InnerTextAsync()).ShouldContain("quantity = 3");
        var message = Page.Locator($"#{await quantity.GetAttributeAsync("aria-errormessage")}");
        (await message.InnerTextAsync()).ShouldBe("Enter a whole number");
    }

    [Fact]
    public async Task ClearingAnOptionalNumberGivesNull()
    {
        var discount = await FocusEmptiedAsync("typed-discount");

        foreach (var key in new[] { "1", ".", "5" })
        {
            await PressAndLetRenderAsync(discount, key);
        }
        (await Page.Locator(Readout).InnerTextAsync()).ShouldContain("discount = 1.5");

        await discount.PressAsync("ControlOrMeta+a");
        await PressAndLetRenderAsync(discount, "Delete");
        await PressAndLetRenderAsync(discount, "Tab");

        (await discount.GetAttributeAsync("aria-invalid")).ShouldBeNull();
        (await Page.Locator(Readout).InnerTextAsync()).ShouldContain("discount = null");
    }

    [Fact]
    public async Task DateAndTimeBoxesAcceptTheValuesTheyAreGiven()
    {
        await NavigateAndWaitForBlazorAsync("/components/input");

        // A browser shows an empty box when it is handed a date or time in a format it rejects.
        (await Page.Locator("#typed-due").InputValueAsync()).ShouldBe("2026-12-01");
        (await Page.Locator("#typed-starts").InputValueAsync()).ShouldStartWith("09:30");
        (await Page.Locator("#typed-shipped").InputValueAsync()).ShouldBe(string.Empty);

        await Page.Locator("#typed-shipped").FillAsync("2026-12-24");
        await Page.Locator("#typed-shipped").BlurAsync();

        await Assertions.Expect(Page.Locator(Readout)).ToContainTextAsync("shipped = 2026-12-24");
    }
}
