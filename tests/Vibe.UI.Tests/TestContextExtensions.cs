using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Vibe.UI.Services;
using Vibe.UI.Themes.Services;
using Vibe.UI.Themes.Models;

namespace Vibe.UI.Tests;

/// <summary>
/// Extension methods for test context setup
/// </summary>
public static class TestContextExtensions
{
    /// <summary>
    /// Adds Vibe.UI services to the test context
    /// </summary>
    public static TestContext AddVibeUIServices(this TestContext ctx)
    {
        ctx.Services.AddSingleton<ThemeManager>(sp =>
        {
            var mockJsRuntime = sp.GetRequiredService<IJSRuntime>();
            return new ThemeManager(mockJsRuntime);
        });

        ctx.Services.AddScoped<IDialogService, DialogService>();
        ctx.Services.AddScoped<IToastService, ToastService>();

        return ctx;
    }

    /// <summary>
    /// Adds a mock theme manager to the test context
    /// </summary>
    public static TestContext AddMockThemeManager(this TestContext ctx)
    {
        var mockThemeManager = new Mock<ThemeManager>(MockBehavior.Loose, ctx.JSInterop.JSRuntime, new ThemeOptions());
        ctx.Services.AddSingleton(mockThemeManager.Object);
        return ctx;
    }
}
