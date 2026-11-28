using Microsoft.AspNetCore.Components;

namespace Vibe.UI.Tests.Components.DataGrid;

/// <summary>
/// The grid asks whether it is rendering interactively, because only a render that is not is
/// followed by one that is. So its tests say which they are: interactive, unless a test about the
/// server's prerender says otherwise.
/// </summary>
/// <remarks>
/// Not done in <see cref="TestBase"/>: reading the renderer there builds the service provider,
/// and tests that register services after it would fail.
/// </remarks>
public abstract class GridTestBase : TestBase
{
    protected GridTestBase() => RenderAs(interactive: true);

    protected void RenderAs(bool interactive) =>
        Renderer.SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));
}
