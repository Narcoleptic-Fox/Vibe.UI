using Microsoft.AspNetCore.Components;

namespace Vibe.UI.Tests.Components.DataGrid;

/// <summary>
/// A host with no persistent component state service still gets a grid: it has no handoff, and
/// nothing else is different. The grid asks for the service and does not inject it, because an
/// injected service that is missing fails the component.
/// </summary>
/// <remarks>
/// That host is anyone's component test of a page that holds a grid. bUnit registers no such
/// service unless asked, and refuses to say how a component is rendered unless told. So without
/// the service the grid must not ask how it is rendered either.
/// </remarks>
public class VibeGridWithoutKeptStateTests : TestBase
{
    [Fact]
    public void A_test_that_says_nothing_about_rendering_still_gets_a_grid()
    {
        var page = Render<VibeGridFixture>();
        page.WaitForAssertion(() => page.FindAll("tbody tr[aria-rowindex]").Count.ShouldBe(10));

        page.Instance.Requests.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Without_the_service_the_grid_fetches_and_shows_its_rows(bool interactive)
    {
        Renderer.SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));
        Services.GetService(typeof(PersistentComponentState)).ShouldBeNull();

        var page = Render<VibeGridFixture>(p =>
        {
            if (!interactive)
            {
                p.SetAssignedRenderMode(
                    Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer
                );
            }
        });
        page.WaitForAssertion(() => page.FindAll("tbody tr[aria-rowindex]").Count.ShouldBe(10));

        page.Instance.Requests.Count.ShouldBe(1);
    }
}
