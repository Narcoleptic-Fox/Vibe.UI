using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Vibe.UI.Tests;

/// <summary>
/// Base class for Vibe.UI component tests
/// </summary>
public abstract class TestBase : BunitContext
{
    protected TestBase()
    {
        // Register Vibe.UI services
        Services.AddVibeUI();

        // Setup JSInterop for tests
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}

/// <summary>Settings for every test in the assembly, whichever base class it uses.</summary>
internal static class TestDefaults
{
    /// <summary>
    /// How long a <c>WaitFor...</c> call may wait before it fails. It returns as soon as its
    /// condition holds, so this costs a passing test nothing. bUnit's default is one second, and
    /// on a busy CI runner a 1 ms or 20 ms timer has taken longer than that to fire and render:
    /// twice on 2026-11-27, in HoverCardTests and ToastTests, with "Check count: 1".
    /// </summary>
    internal static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

    // Before any test runs, so a class that does not derive from TestBase gets it too.
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Apply() => BunitContext.DefaultWaitTimeout = WaitTimeout;
}
