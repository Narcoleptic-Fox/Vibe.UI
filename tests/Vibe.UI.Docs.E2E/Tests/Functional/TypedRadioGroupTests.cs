using Microsoft.Playwright;
using Vibe.UI.Docs.E2E.Infrastructure;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Functional;

/// <summary>
/// The typed examples on the RadioGroup page, in a real browser: items take their type from the
/// group, and an enum builds its own items. Each check reads the value the page reports, not only
/// which radio looks checked.
/// </summary>
[Trait("Category", TestCategories.Functional)]
public class TypedRadioGroupTests : E2ETestBase
{
    private ILocator Readout => Page.Locator("#radio-typed-readout");

    private ILocator Group(string testId) =>
        Page.GetByTestId(testId).GetByRole(AriaRole.Radiogroup);

    // The radio input is visually hidden under a styled control, so a person clicks the label.
    private static Task PickAsync(ILocator group, string label) =>
        group.GetByText(label, new() { Exact = true }).ClickAsync();

    [Fact]
    public async Task AnEnumGroupBuildsItsItemsAndReportsTheEnumValue()
    {
        await NavigateAndWaitForBlazorAsync("/components/radiogroup");
        var group = Group("radio-typed-enum");

        await Assertions.Expect(group.GetByRole(AriaRole.Radio)).ToHaveCountAsync(3);
        await Assertions.Expect(group.GetByLabel("Comfortably spaced")).ToBeCheckedAsync();

        await PickAsync(group, "Roomy");

        await Assertions.Expect(Readout).ToContainTextAsync("density = Roomy");
    }

    [Fact]
    public async Task AnIntGroupReportsTheNumberOfTheItemPicked()
    {
        await NavigateAndWaitForBlazorAsync("/components/radiogroup");
        var group = Group("radio-typed-int");

        await Assertions.Expect(group.GetByLabel("Team of five")).ToBeCheckedAsync();

        await PickAsync(group, "Department");

        await Assertions.Expect(Readout).ToContainTextAsync("seats = 25");
    }

    [Fact]
    public async Task ANullableBoolGroupStartsUncheckedAndReportsTheChoice()
    {
        await NavigateAndWaitForBlazorAsync("/components/radiogroup");
        var group = Group("radio-typed-bool");

        await Assertions.Expect(group.Locator("input:checked")).ToHaveCountAsync(0);
        await Assertions.Expect(Readout).ToContainTextAsync("subscribed = null");

        await PickAsync(group, "Not subscribed");

        await Assertions.Expect(Readout).ToContainTextAsync("subscribed = False");
    }
}
