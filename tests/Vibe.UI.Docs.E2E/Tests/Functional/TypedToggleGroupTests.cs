using Microsoft.Playwright;
using Vibe.UI.Docs.E2E.Infrastructure;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Functional;

/// <summary>
/// The typed examples on the ToggleGroup page, in a real browser. Each check reads the value the
/// page reports, not only which button looks pressed.
/// </summary>
[Trait("Category", TestCategories.Functional)]
public class TypedToggleGroupTests : E2ETestBase
{
    private ILocator Readout => Page.Locator("#toggle-typed-readout");

    private ILocator Group(string testId) => Page.GetByTestId(testId).GetByRole(AriaRole.Group);

    private static ILocator Item(ILocator group, string label) =>
        group.GetByRole(AriaRole.Button, new() { Name = label, Exact = true });

    [Fact]
    public async Task ANullableEnumGroupBuildsItsItemsAndClearsWhenPressedAgain()
    {
        await NavigateAndWaitForBlazorAsync("/components/togglegroup");
        var group = Group("toggle-typed-enum");

        await Assertions.Expect(group.GetByRole(AriaRole.Button)).ToHaveCountAsync(3);
        await Assertions.Expect(Item(group, "Left")).ToHaveAttributeAsync("aria-pressed", "true");

        await Item(group, "Right").ClickAsync();
        await Assertions.Expect(Readout).ToContainTextAsync("alignment = Right");

        await Item(group, "Right").ClickAsync();
        await Assertions.Expect(Readout).ToContainTextAsync("alignment = null");
    }

    [Fact]
    public async Task AMultipleEnumGroupReportsTheListOfPressedValues()
    {
        await NavigateAndWaitForBlazorAsync("/components/togglegroup");
        var group = Group("toggle-typed-days");

        await Assertions.Expect(group.GetByRole(AriaRole.Button)).ToHaveCountAsync(7);
        await Assertions.Expect(Readout).ToContainTextAsync("days = Monday, Wednesday");

        await Item(group, "Friday").ClickAsync();
        await Item(group, "Monday").ClickAsync();

        await Assertions.Expect(Readout).ToContainTextAsync("days = Wednesday, Friday");
    }

    [Fact]
    public async Task AnIntGroupKeepsItsValueWhenTheSelectedItemIsPressedAgain()
    {
        await NavigateAndWaitForBlazorAsync("/components/togglegroup");
        var group = Group("toggle-typed-int");

        await Item(group, "50").ClickAsync();
        await Assertions.Expect(Readout).ToContainTextAsync("page size = 50");

        await Item(group, "50").ClickAsync();

        await Assertions.Expect(Item(group, "50")).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(Readout).ToContainTextAsync("page size = 50");
    }
}
