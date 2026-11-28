using Vibe.UI.Components;
using Row = Vibe.UI.Tests.Components.DataGrid.VibeGridOptionsFixture.Row;

namespace Vibe.UI.Tests.Components.DataGrid;

/// <summary>
/// The grid's less usual settings, and what it does when it is given too little or is taken
/// away while a question is still out.
/// </summary>
public class VibeGridOptionsTests : TestBase
{
    [Fact]
    public void Without_a_row_key_and_with_a_page_that_is_not_a_list_the_rows_are_still_drawn()
    {
        var page = Render<VibeGridOptionsFixture>();
        page.WaitForAssertion(() => page.FindAll("tbody tr[aria-rowindex]").Count.ShouldBe(10));

        page.Find("tbody tr[aria-rowindex] td").TextContent.Trim().ShouldBe("Row 1");
        page.Instance.Requests.ShouldBe(1);

        // The paginator's name is the one the page gave.
        page.Find(".vibe-grid-pager nav").GetAttribute("aria-label").ShouldBe("Result pages");
    }

    [Fact]
    public void With_the_paginator_switched_off_none_is_drawn_though_there_are_more_pages()
    {
        var page = Render<VibeGridOptionsFixture>(p => p.Add(f => f.ShowPagination, false));
        page.WaitForAssertion(() => page.FindAll("tbody tr[aria-rowindex]").Count.ShouldBe(10));

        page.FindAll(".vibe-grid-pager").ShouldBeEmpty();
        page.Instance.Requests.ShouldBe(1);
    }

    [Fact]
    public void A_grid_given_no_provider_says_so()
    {
        var error = Should.Throw<InvalidOperationException>(() => Render<VibeGrid<Row>>());

        error.Message.ShouldContain("ItemsProvider");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Leaving_the_page_cancels_the_question_still_out(bool providerThrowsWhenCancelled)
    {
        var page = Render<VibeGridSlowFixture>(p =>
            p.Add(f => f.ThrowsWhenCancelled, providerThrowsWhenCancelled));
        page.WaitForAssertion(() => page.Instance.Questions.Count.ShouldBe(1));
        // Held now: the fixture cannot be asked for anything once it is disposed.
        var questions = page.Instance.Questions;
        var question = questions[0];
        question.Request.CancellationToken.IsCancellationRequested.ShouldBeFalse();

        // The user left while the server was still answering. QuickGrid does not withdraw its
        // question when it is disposed, so the grid does.
        await DisposeComponentsAsync();

        question.Request.CancellationToken.IsCancellationRequested.ShouldBeTrue();

        // A provider that ignores the token answers anyway; one that honours it has thrown.
        // Either way nothing is left to fail, and nothing more is asked.
        question.Answer.TrySetResult(VibeGridSlowFixture.Page("Late", count: 10, total: 95));
        await Task.Yield();
        questions.Count.ShouldBe(1);
    }
}
