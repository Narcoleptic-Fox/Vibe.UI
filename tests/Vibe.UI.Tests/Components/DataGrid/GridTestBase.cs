using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Vibe.UI.Tests.Components.DataGrid;

/// <summary>
/// The grid asks how it is being rendered, because a page prerendered on the server is rendered
/// again, interactively, by a new grid, and the first keeps its page for the second. So its
/// tests say which render they are: interactive, unless a test says otherwise.
/// </summary>
/// <remarks>
/// Not done in <see cref="TestBase"/>: reading the renderer there builds the service provider,
/// and tests that register services after it would fail.
/// </remarks>
public abstract class GridTestBase : TestBase
{
    protected GridTestBase()
    {
        // Before the renderer is read, which is when the services are fixed.
        KeptState = AddBunitPersistentComponentState();
        RenderAs(interactive: true);
    }

    /// <summary>What a prerender keeps for the interactive render that follows it.</summary>
    protected BunitPersistentComponentState KeptState { get; }

    /// <summary>The render mode a prerendered component is given when one will follow.</summary>
    protected static IComponentRenderMode Interactive => RenderMode.InteractiveServer;

    protected void RenderAs(bool interactive) =>
        Renderer.SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));

    /// <summary>The name a grid keeps its prerendered page under.</summary>
    protected static string KeptPageName<TRow>(string? stateKey = null) =>
        $"Vibe.UI.VibeGrid:{typeof(TRow).FullName}:{stateKey}";

    /// <summary>What the grid kept, once the prerender is over. Null when it kept nothing.</summary>
    protected string? KeptAfterPrerender<TRow>(string? stateKey = null)
    {
        KeptState.TriggerOnPersisting();
        return KeptState.TryTake(KeptPageName<TRow>(stateKey), out string? kept) ? kept : null;
    }
}
