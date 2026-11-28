using Vibe.UI.Components;

namespace Vibe.UI.Tests;

public class TestDefaultsTests
{
    [Fact]
    public void A_wait_is_given_the_bound_the_suite_sets_not_bUnits_one_second()
    {
        // Not derived from TestBase, on purpose: the bound has to hold for every test class.
        BunitContext.DefaultWaitTimeout.ShouldBe(TestDefaults.WaitTimeout);
        TestDefaults.WaitTimeout.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void A_timer_that_takes_longer_than_one_second_is_still_waited_for()
    {
        using var context = new BunitContext();
        context.Services.AddVibeUI();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var closeCount = 0;

        // Closes itself after 1.5 s: under bUnit's own default of one second this wait throws,
        // which is what a slow runner did to a 20 ms timer.
        var cut = context.Render<Toast>(parameters => parameters
            .Add(p => p.Description, "Slow")
            .Add(p => p.Duration, 1500)
            .Add(p => p.OnClose, () => closeCount++));

        cut.WaitForAssertion(() => closeCount.ShouldBe(1));
    }
}
