using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Vibe.UI.Configuration;
using Vibe.UI.Services.Theme;

namespace Vibe.UI.Docs.E2E.Infrastructure;

/// <summary>
/// Shows the docs site in any built-in theme. The site is built with the default theme; an app
/// picks another with <c>AddVibeUI(o =&gt; o.BaseColor = "Ink")</c>, and <see cref="ThemeService"/>
/// writes that theme's <c>:root</c> and <c>.dark</c> blocks. This takes the same CSS from the same
/// service and lays it over the page, then reads the variables back to prove it took.
/// </summary>
public static partial class BuiltInThemes
{
    /// <summary>Every built-in theme. A theme added to <c>themes/</c> must be added here.</summary>
    public static readonly string[] Names = ["Tide", "Ink", "Dune"];

    private const string StyleId = "e2e-built-in-theme";

    /// <summary>The CSS an app configured with this theme renders.</summary>
    public static string Css(string theme) =>
        new ThemeService(new VibeThemeOptions { BaseColor = theme }).GenerateThemeCss();

    /// <summary>
    /// Applies the theme to the current page and checks that the page now resolves its colors,
    /// both on the page and inside a scoped <c>.dark</c> element. Call again after navigating.
    /// </summary>
    public static async Task ApplyBuiltInThemeAsync(this IPage page, string theme)
    {
        var css = Css(theme);

        // Appended at the end of the body, after the ThemeProvider's style, with the same
        // selectors, so it wins on order alone and scoped .dark areas follow it too.
        await page.EvaluateAsync(
            """
            ([id, css]) => {
                document.getElementById(id)?.remove();
                const style = document.createElement('style');
                style.id = id;
                style.textContent = css;
                document.body.appendChild(style);
            }
            """,
            new object[] { StyleId, css }
        );

        var expectedLight = Token(css, ":root", "background");
        var expectedDark = Token(css, ".dark", "background");
        var actual = await page.EvaluateAsync<string[]>(
            """
            () => {
                const read = element => getComputedStyle(element).getPropertyValue('--vibe-background').trim();
                const probe = document.createElement('div');
                probe.className = 'dark';
                document.body.appendChild(probe);
                const scopedDark = read(probe);
                probe.remove();
                const isDark = document.documentElement.classList.contains('dark');
                return [read(document.documentElement), scopedDark, isDark ? 'dark' : 'light'];
            }
            """
        );

        var expectedPage = actual[2] == "dark" ? expectedDark : expectedLight;
        if (actual[0] != expectedPage || actual[1] != expectedDark)
        {
            throw new InvalidOperationException(
                $"{theme} did not take: the page resolves --vibe-background to '{actual[0]}' "
                    + $"(expected '{expectedPage}') and a scoped .dark element to '{actual[1]}' "
                    + $"(expected '{expectedDark}')."
            );
        }
    }

    private static string Token(string css, string selector, string token)
    {
        var block = Regex.Match(css, Regex.Escape(selector) + @"\s*\{(?<body>[^}]*)\}");
        var value = Regex.Match(
            block.Groups["body"].Value,
            $@"--vibe-{Regex.Escape(token)}:\s*(?<value>[^;]+);"
        );
        if (!value.Success)
        {
            throw new InvalidOperationException(
                $"The theme CSS has no --vibe-{token} in {selector}."
            );
        }

        return value.Groups["value"].Value.Trim();
    }
}
