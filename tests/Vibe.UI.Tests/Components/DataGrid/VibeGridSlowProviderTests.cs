using AngleSharp.Dom;
using Vibe.UI.Components;

namespace Vibe.UI.Tests.Components.DataGrid;

/// <summary>
/// A real provider answers later, and two answers can arrive in the other order. The grid
/// remembers the last answer so that QuickGrid's repeated question costs nothing; these tests
/// are about which answer it may remember.
/// </summary>
public class VibeGridSlowProviderTests : GridTestBase
{
    private IRenderedComponent<VibeGridSlowFixture> page = default!;

    private List<VibeGridSlowFixture.Asked> Questions => page.Instance.Questions;

    private IReadOnlyList<IElement> DataRows() => page.FindAll("tbody tr[aria-rowindex]");

    private string FirstCell() => DataRows()[0].QuerySelector("td")!.TextContent.Trim();

    private Task Answer(int question, VibeGridPage<VibeGridSlowFixture.Row> answer) =>
        page.InvokeAsync(() => Questions[question].Answer.TrySetResult(answer));

    [Fact]
    public async Task An_answer_that_arrives_after_a_newer_one_is_never_shown()
    {
        page = Render<VibeGridSlowFixture>();
        page.WaitForAssertion(() => Questions.Count.ShouldBe(1));

        // The filter changed before the first answer came back, so the page asked again.
        // Not awaited: a refresh finishes when its answer comes, and the test gives that later.
        Task refresh = page.InvokeAsync(() => page.Instance.Grid.RefreshAsync(toFirstPage: true));
        page.WaitForAssertion(() => Questions.Count.ShouldBe(2));

        // The newer question is answered first, with a different total. The older answer,
        // for the filter that is gone, arrives last.
        await Answer(1, VibeGridSlowFixture.Page("New", count: 6, total: 6));
        page.WaitForAssertion(() => FirstCell().ShouldBe("New 1"));
        await Answer(0, VibeGridSlowFixture.Page("Old", count: 10, total: 95));
        await refresh;

        // QuickGrid asks again after a changed total, when the page next renders. Whatever
        // answers that, it must be the rows for the filter that is in force.
        page.Find("#poke").Click();
        page.Find("#poke").Click();

        page.WaitForAssertion(() => FirstCell().ShouldBe("New 1"));
        DataRows().Count.ShouldBe(6);
        Questions.Count.ShouldBe(2);
    }

    [Fact]
    public async Task A_provider_that_gives_up_when_cancelled_does_not_break_the_grid()
    {
        page = Render<VibeGridSlowFixture>(p => p.Add(f => f.ThrowsWhenCancelled, true));
        page.WaitForAssertion(() => Questions.Count.ShouldBe(1));

        // Asking again cancels the question still in flight, and that provider throws.
        Task refresh = page.InvokeAsync(() => page.Instance.Grid.RefreshAsync());
        page.WaitForAssertion(() => Questions.Count.ShouldBe(2));
        Questions[0].Request.CancellationToken.IsCancellationRequested.ShouldBeTrue();

        await Answer(1, VibeGridSlowFixture.Page("New", count: 6, total: 6));
        await refresh;

        page.WaitForAssertion(() => FirstCell().ShouldBe("New 1"));
        page.Find("#poke").Click();
        page.Find("#poke").TextContent.ShouldBe("Poked 1");
        DataRows().Count.ShouldBe(6);
    }

    [Fact]
    public async Task While_a_question_is_unanswered_the_same_question_is_not_asked_again()
    {
        page = Render<VibeGridSlowFixture>();
        page.WaitForAssertion(() => Questions.Count.ShouldBe(1));

        // The page renders again around the grid before the server has answered.
        page.Find("#poke").Click();
        page.Find("#poke").Click();

        Questions.Count.ShouldBe(1);

        await Answer(0, VibeGridSlowFixture.Page("Only", count: 10, total: 95));
        page.WaitForAssertion(() => FirstCell().ShouldBe("Only 1"));
        page.Find("#poke").Click();
        Questions.Count.ShouldBe(1);
    }
}
