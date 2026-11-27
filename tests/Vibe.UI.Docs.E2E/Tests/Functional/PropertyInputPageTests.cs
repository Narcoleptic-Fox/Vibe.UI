using Microsoft.Playwright;
using Vibe.UI.Docs.E2E.Infrastructure;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Functional;

/// <summary>
/// The PropertyInput page's preview in a real browser. The page prints the model inside the form,
/// from the model itself, so each check below needs two things to work: the field writes the
/// model, and the form is told and re-renders. A field that stops doing either turns a test red.
/// Text fields commit when they are left, so each one is followed by Tab.
/// </summary>
[Trait("Category", TestCategories.Functional)]
public class PropertyInputPageTests : E2ETestBase
{
    private const string Route = "/components/propertyinput";

    private ILocator ModelValue(string key) => Page.Locator($"#pi-model-{key}");

    [Fact]
    public async Task TypingIntoAFieldWritesTheModel()
    {
        await NavigateAndWaitForBlazorAsync(Route);
        await Assertions.Expect(ModelValue("name")).ToHaveTextAsync("null");

        await Page.Locator("#pi-name").FillAsync("Ada Lovelace");
        await Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(ModelValue("name")).ToHaveTextAsync("Ada Lovelace");

        await Page.Locator("#pi-team-size").FillAsync("12");
        await Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(ModelValue("team-size")).ToHaveTextAsync("12");

        await Page.Locator("#pi-start").FillAsync("2026-12-01");
        await Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(ModelValue("start")).ToHaveTextAsync("2026-12-01");
    }

    [Fact]
    public async Task AMultilineStringIsATextAreaThatWritesTheModel()
    {
        await NavigateAndWaitForBlazorAsync(Route);
        // [DataType(DataType.MultilineText)] on the model picks the TextArea.
        var notes = Page.Locator("textarea#pi-notes");
        await Assertions.Expect(notes).ToHaveCountAsync(1);
        await Assertions.Expect(ModelValue("notes")).ToHaveTextAsync("null");

        await notes.FillAsync("Two seats to start");
        await Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(ModelValue("notes")).ToHaveTextAsync("Two seats to start");
    }

    [Fact]
    public async Task TheCallSiteOverridesTheLabelAndTheLimit()
    {
        await NavigateAndWaitForBlazorAsync(Route);

        // [Range(1, 500)] on the model; Maximum="50" and Label="Seats needed" on the line.
        var teamSize = Page.GetByLabel("Seats needed", new() { Exact = true });
        await Assertions.Expect(teamSize).ToHaveAttributeAsync("max", "50");
        await Assertions.Expect(teamSize).ToHaveAttributeAsync("min", "1");
    }

    [Fact]
    public async Task PickingAnEnumOptionWritesItAndShowsTheFieldThatDependsOnIt()
    {
        await NavigateAndWaitForBlazorAsync(Route);
        var companyNumber = Page.Locator("#pi-company-number");
        await Assertions.Expect(companyNumber).ToHaveCountAsync(0);

        await Page.Locator("#pi-plan").SelectOptionAsync("Enterprise");
        await Assertions.Expect(ModelValue("plan")).ToHaveTextAsync("Enterprise");
        await Assertions.Expect(companyNumber).ToBeVisibleAsync();

        await Page.Locator("#pi-plan").SelectOptionAsync("Team");
        await Assertions.Expect(ModelValue("plan")).ToHaveTextAsync("Team");
        await Assertions.Expect(companyNumber).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task TickingTheCheckboxWritesTheBool()
    {
        await NavigateAndWaitForBlazorAsync(Route);
        await Assertions.Expect(ModelValue("news")).ToHaveTextAsync("false");

        await Page.GetByText("Send me product news", new() { Exact = true }).ClickAsync();
        await Assertions.Expect(ModelValue("news")).ToHaveTextAsync("true");

        await Page.GetByText("Send me product news", new() { Exact = true }).ClickAsync();
        await Assertions.Expect(ModelValue("news")).ToHaveTextAsync("false");
    }

    [Fact]
    public async Task ANullableBoolIsYesNoOrNotAnswered()
    {
        await NavigateAndWaitForBlazorAsync(Route);
        var customer = Page.Locator("#pi-customer");
        await Assertions.Expect(ModelValue("customer")).ToHaveTextAsync("null");

        await customer.SelectOptionAsync(new SelectOptionValue { Label = "Yes" });
        await Assertions.Expect(ModelValue("customer")).ToHaveTextAsync("true");

        await customer.SelectOptionAsync(new SelectOptionValue { Label = "No" });
        await Assertions.Expect(ModelValue("customer")).ToHaveTextAsync("false");

        await customer.SelectOptionAsync(string.Empty);
        await Assertions.Expect(ModelValue("customer")).ToHaveTextAsync("null");
    }

    [Fact]
    public async Task ANamedControlWritesTheModelAndTellsTheForm()
    {
        await NavigateAndWaitForBlazorAsync(Route);
        await Assertions.Expect(ModelValue("billing")).ToHaveTextAsync("Monthly");

        // The radio input is visually hidden under a styled control, so a person clicks the label.
        await Page.GetByText("Yearly", new() { Exact = true }).ClickAsync();
        await Assertions.Expect(ModelValue("billing")).ToHaveTextAsync("Yearly");
    }

    [Fact]
    public async Task SubmittingEmptySaysEachRequiredMessageOnce()
    {
        await NavigateAndWaitForBlazorAsync(Route);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign up", Exact = true })
            .ClickAsync();

        await Assertions
            .Expect(Page.GetByTestId("pi-status"))
            .ToHaveTextAsync("Review the highlighted fields.");
        // Once on the page: under the field, and not again in a summary.
        await Assertions
            .Expect(Page.GetByText("The Full name field is required.", new() { Exact = true }))
            .ToHaveCountAsync(1);
        await Assertions
            .Expect(Page.GetByText("The Work email field is required.", new() { Exact = true }))
            .ToHaveCountAsync(1);
        await Assertions
            .Expect(Page.Locator("#pi-name-error"))
            .ToHaveTextAsync("The Full name field is required.");

        // Putting the field right clears its message without submitting again.
        await Page.Locator("#pi-name").FillAsync("Ada Lovelace");
        await Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(Page.Locator("#pi-name-error")).ToHaveCountAsync(0);
    }
}
