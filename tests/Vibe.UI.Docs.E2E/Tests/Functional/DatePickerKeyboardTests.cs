using Microsoft.Playwright;
using Shouldly;
using Vibe.UI.Docs.E2E.Infrastructure;
using Xunit;

namespace Vibe.UI.Docs.E2E.Tests.Functional;

[Trait("Category", TestCategories.Functional)]
[Trait("Category", "Accessibility")]
public class DatePickerKeyboardTests : E2ETestBase
{
    // The pickers move focus after they render, through a script call. A focus check therefore
    // waits for it: read straight after the popup shows, the answer depends on the machine.
    [Fact]
    public async Task DatePickerSupportsKeyboardSelectionAndRestoresInputFocus()
    {
        await NavigateAndWaitForBlazorAsync("/components/datepicker");

        var preview = Page.Locator("main section").First;
        var input = preview.Locator(".date-input-wrapper input");

        await input.FocusAsync();
        await input.PressAsync("Enter");

        var popup = preview.Locator(".date-popup");
        await popup.WaitForAsync();
        var activeDay = preview.Locator(".date-day[tabindex='0']");
        await Assertions.Expect(activeDay).ToBeFocusedAsync();
        (await activeDay.EvaluateAsync<string>("element => getComputedStyle(element).outlineStyle")).ShouldNotBe("none");

        var initialDayId = await activeDay.GetAttributeAsync("id");
        await activeDay.PressAsync("ArrowRight");

        var nextActiveDay = preview.Locator(".date-day[tabindex='0']");
        (await nextActiveDay.GetAttributeAsync("id")).ShouldNotBe(initialDayId);
        await Assertions.Expect(nextActiveDay).ToBeFocusedAsync();

        await nextActiveDay.PressAsync("Escape");
        await popup.WaitForAsync(new() { State = WaitForSelectorState.Detached });
        var inputId = await input.GetAttributeAsync("id");
        await Page.WaitForFunctionAsync(
            "elementId => document.activeElement?.id === elementId",
            inputId);
        await Assertions.Expect(input).ToBeFocusedAsync();

        await input.PressAsync("Enter");
        activeDay = preview.Locator(".date-day[tabindex='0']");
        await activeDay.PressAsync("Enter");

        await popup.WaitForAsync(new() { State = WaitForSelectorState.Detached });
        (await input.InputValueAsync()).ShouldNotBeNullOrWhiteSpace();
        await preview.GetByText("Date selected:", new() { Exact = false }).WaitForAsync();
    }

    [Fact]
    public async Task DateRangePickerPublishesAppliedRangeAndRestoresEndInputFocus()
    {
        await NavigateAndWaitForBlazorAsync("/components/daterangepicker");

        var preview = Page.Locator("main section").First;
        var inputs = preview.Locator(".daterange-inputs input");
        var startInput = inputs.Nth(0);
        var endInput = inputs.Nth(1);

        await startInput.FocusAsync();
        await startInput.PressAsync("Enter");

        var popup = preview.Locator(".daterange-popup");
        await popup.WaitForAsync();
        var activeDay = preview.Locator(".daterange-day[tabindex='0']");
        await Assertions.Expect(activeDay).ToBeFocusedAsync();

        await activeDay.PressAsync("Enter");
        var activeEndDay = preview.Locator(".daterange-calendar").Nth(1).Locator(".daterange-day[tabindex='0']");
        await Assertions.Expect(activeEndDay).ToBeFocusedAsync();

        await activeEndDay.PressAsync("ArrowRight");
        activeEndDay = preview.Locator(".daterange-calendar").Nth(1).Locator(".daterange-day[tabindex='0']");
        await activeEndDay.PressAsync("Enter");
        await preview.Locator(".daterange-apply-btn").ClickAsync();

        await popup.WaitForAsync(new() { State = WaitForSelectorState.Detached });
        (await startInput.InputValueAsync()).ShouldNotBeNullOrWhiteSpace();
        (await endInput.InputValueAsync()).ShouldNotBeNullOrWhiteSpace();
        await preview.GetByText("Selected Range:", new() { Exact = false }).WaitForAsync();

        await endInput.FocusAsync();
        await endInput.PressAsync("Enter");
        await preview.Locator(".daterange-popup").WaitForAsync();
        await Page.Keyboard.PressAsync("Escape");
        await preview.Locator(".daterange-popup").WaitForAsync(new() { State = WaitForSelectorState.Detached });
        var endInputId = await endInput.GetAttributeAsync("id");
        await Page.WaitForFunctionAsync(
            "elementId => document.activeElement?.id === elementId",
            endInputId);
        await Assertions.Expect(endInput).ToBeFocusedAsync();
    }

    [Theory]
    [InlineData("/components/datepicker", ".date-input-wrapper input", ".date-day[tabindex='0']")]
    [InlineData("/components/daterangepicker", ".daterange-inputs input", ".daterange-day[tabindex='0']")]
    public async Task VerticalDayGridArrowsDoNotScrollAndUnrelatedKeysRemainUnblocked(
        string route,
        string inputSelector,
        string activeDaySelector)
    {
        await NavigateAndWaitForBlazorAsync(route);

        var preview = Page.Locator("main section").First;
        var input = preview.Locator(inputSelector).First;
        await input.FocusAsync();
        await input.PressAsync("Enter");

        var activeDay = preview.Locator(activeDaySelector);
        await activeDay.WaitForAsync();
        await Assertions.Expect(activeDay).ToBeFocusedAsync();

        var initialDayId = await activeDay.GetAttributeAsync("id");
        initialDayId.ShouldNotBeNullOrWhiteSpace();
        var initialScrollPosition = await Page.EvaluateAsync<int>(
            """
            () => {
                const scrollingElement = document.scrollingElement;
                document.documentElement.style.scrollBehavior = 'auto';
                const target = Math.floor((scrollingElement.scrollHeight - scrollingElement.clientHeight) / 2);
                scrollingElement.scrollTop = target;
                return scrollingElement.scrollTop;
            }
            """);
        initialScrollPosition.ShouldBeGreaterThan(0, "The docs page must be scrollable for this regression test.");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.WaitForFunctionAsync(
            "elementId => document.activeElement?.id !== elementId",
            initialDayId);
        await Page.WaitForTimeoutAsync(150);
        (await Page.EvaluateAsync<int>("() => document.scrollingElement.scrollTop"))
            .ShouldBe(initialScrollPosition);

        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.WaitForFunctionAsync(
            "elementId => document.activeElement?.id === elementId",
            initialDayId);
        await Page.WaitForTimeoutAsync(150);
        (await Page.EvaluateAsync<int>("() => document.scrollingElement.scrollTop"))
            .ShouldBe(initialScrollPosition);

        activeDay = preview.Locator(activeDaySelector);
        var unrelatedKeyWasPrevented = await activeDay.EvaluateAsync<bool>(
            """
            element => {
                const event = new KeyboardEvent('keydown', {
                    key: 'PageDown',
                    bubbles: true,
                    cancelable: true
                });
                element.dispatchEvent(event);
                return event.defaultPrevented;
            }
            """);
        unrelatedKeyWasPrevented.ShouldBeFalse("Only handled day-grid navigation keys should be canceled.");
    }

    // A slow focus call (a slow device, or an Interactive Server circuit) leaves a window between
    // a handled arrow key and the focus landing on the new day. Keys pressed in that window must
    // still reach the picker, move from the new day, and not scroll the page. The focus calls are
    // held back by a gate the test opens one call at a time, so the window is as long as needed.
    [Theory]
    [InlineData("/components/datepicker", ".date-input-wrapper input", ".date-day[tabindex='0']")]
    [InlineData("/components/daterangepicker", ".daterange-inputs input", ".daterange-day[tabindex='0']")]
    public async Task ArrowKeysWhileTheFocusCallIsSlow_KeepFocusOnTheMovedToDay(
        string route,
        string inputSelector,
        string activeDaySelector)
    {
        await GateFocusCallsAsync();
        await NavigateAndWaitForBlazorAsync(route);

        var preview = Page.Locator("main section").First;
        var input = preview.Locator(inputSelector).First;
        await input.FocusAsync();
        await input.PressAsync("Enter");
        var tabStop = preview.Locator(activeDaySelector);
        await Assertions.Expect(tabStop).ToBeFocusedAsync();
        (await Page.EvaluateAsync<int>("() => window.__vibeFocusCalls.started")).ShouldBeGreaterThan(0, "the gate must wrap the picker's focus calls");

        // Walk to the last day of the shown month, so the next ArrowRight changes month on any date.
        var day = await FocusedDayAsync(tabStop);
        while (day.AddDays(1).Month == day.Month)
        {
            await Page.Keyboard.PressAsync("ArrowRight");
            day = day.AddDays(1);
            await WaitForFocusOnDayAsync(day);
        }

        var scrollTop = await Page.EvaluateAsync<int>(
            """
            () => {
                const scrollingElement = document.scrollingElement;
                document.documentElement.style.scrollBehavior = 'auto';
                scrollingElement.scrollTop = Math.floor((scrollingElement.scrollHeight - scrollingElement.clientHeight) / 2);
                return scrollingElement.scrollTop;
            }
            """);
        scrollTop.ShouldBeGreaterThan(0, "The docs page must be scrollable for this test.");

        await Page.EvaluateAsync("() => { window.__vibeFocusCalls.closed = true; }");

        // Across the month: the grid is rebuilt, and focus must not be left on the body.
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForFocusOnDayAsync(day.AddDays(1));
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForFocusOnDayAsync(day.AddDays(3));

        // Within the month.
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForFocusOnDayAsync(day.AddDays(5));

        (await Page.EvaluateAsync<int>("() => window.__vibeFocusCalls.held.length")).ShouldBe(5, "every move's focus call is still held");
        (await Page.EvaluateAsync<int>("() => document.scrollingElement.scrollTop")).ShouldBe(scrollTop);

        // A held call for an earlier day must not pull focus back when it finally runs.
        await Page.EvaluateAsync("() => window.__vibeFocusCalls.releaseOne()");
        await Page.WaitForFunctionAsync("() => window.__vibeFocusCalls.finished === window.__vibeFocusCalls.started - 4");
        await WaitForFocusOnDayAsync(day.AddDays(5));
        (await FocusedDayAsync(tabStop)).ShouldBe(day.AddDays(5));

        await Page.EvaluateAsync("() => window.__vibeFocusCalls.releaseAll()");
        await Page.WaitForFunctionAsync("() => window.__vibeFocusCalls.finished === window.__vibeFocusCalls.started");
        await WaitForFocusOnDayAsync(day.AddDays(5));
        (await Page.EvaluateAsync<int>("() => document.scrollingElement.scrollTop")).ShouldBe(scrollTop);
    }

    // Decision, not accident: clicking a month button moves focus to the first selectable day of
    // the month it shows, as the pickers' own month handlers ask, with no keyboard focus ring.
    // The grid guard's focus rescue must not change that end state in any browser. In WebKit a
    // click leaves focus on the body rather than the button, so there the rescue is what
    // focuses that day first; elsewhere the button keeps focus until the picker's call lands.
    [Theory]
    [InlineData("/components/datepicker", ".date-input-wrapper input", ".date-day[tabindex='0']", ".date-nav-btn", ".date-grid")]
    [InlineData("/components/daterangepicker", ".daterange-inputs input", ".daterange-day[tabindex='0']", ".daterange-nav-btn", ".daterange-grid")]
    public async Task MouseMonthNavigationAfterKeyboardFocus_FocusesTheFirstSelectableDayWithoutAFocusRing(
        string route,
        string inputSelector,
        string activeDaySelector,
        string navButtonSelector,
        string gridSelector)
    {
        await NavigateAndWaitForBlazorAsync(route);

        var preview = Page.Locator("main section").First;
        var input = preview.Locator(inputSelector).First;
        await input.FocusAsync();
        await input.PressAsync("Enter");
        await Assertions.Expect(preview.Locator(activeDaySelector)).ToBeFocusedAsync();

        var firstGrid = preview.Locator(gridSelector).First;
        var firstSelectableDay = firstGrid.Locator("[role='gridcell']:not([disabled])").First;

        await preview.Locator(navButtonSelector).Last.ClickAsync();
        await Assertions.Expect(firstSelectableDay).ToBeFocusedAsync();
        (await firstSelectableDay.EvaluateAsync<bool>("element => element.matches(':focus-visible')")).ShouldBeFalse();

        await preview.Locator(navButtonSelector).First.ClickAsync();
        await Assertions.Expect(firstSelectableDay).ToBeFocusedAsync();
        (await firstSelectableDay.EvaluateAsync<bool>("element => element.matches(':focus-visible')")).ShouldBeFalse();
    }

    // Wraps vibe-dom.js's focusElement: while window.__vibeFocusCalls.closed is set, each call is
    // held until the test releases it.
    private async Task GateFocusCallsAsync()
    {
        const string original = "export function focusElement(elementId) {";
        await Page.RouteAsync("**/_content/Vibe.UI/js/vibe-dom.js", async request =>
        {
            var response = await request.FetchAsync();
            var script = await response.TextAsync();
            var occurrences = script.Split(original).Length - 1;
            occurrences.ShouldBe(1, "vibe-dom.js must export focusElement exactly once for the gate to wrap it");
            script = script.Replace(original, "function focusElementUngated(elementId) {")
                + """

                const gate = (window.__vibeFocusCalls = {
                  closed: false,
                  started: 0,
                  finished: 0,
                  held: [],
                  releaseOne() { this.held.shift()?.(); },
                  releaseAll() { while (this.held.length > 0) this.releaseOne(); },
                });

                export function focusElement(elementId) {
                  gate.started++;
                  const run = () => focusElementUngated(elementId).finally(() => gate.finished++);
                  return gate.closed
                    ? new Promise((resolve) => gate.held.push(() => run().then(resolve)))
                    : run();
                }
                """;
            await request.FulfillAsync(new() { Response = response, Body = script });
        });
    }

    private static async Task<DateTime> FocusedDayAsync(ILocator tabStop)
    {
        var id = await tabStop.GetAttributeAsync("id");
        id.ShouldNotBeNull();
        return DateTime.ParseExact(id[^8..], "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
    }

    private Task WaitForFocusOnDayAsync(DateTime day) =>
        Page.WaitForFunctionAsync(
            "suffix => document.activeElement?.getAttribute('role') === 'gridcell' && document.activeElement.id.endsWith(suffix)",
            $"-day-{day:yyyyMMdd}",
            new() { Timeout = 5000 });
}
