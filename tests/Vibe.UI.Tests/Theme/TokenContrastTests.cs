using System.Globalization;
using System.Text.RegularExpressions;
using Vibe.UI.Configuration;
using Vibe.UI.Services.Theme;

namespace Vibe.UI.Tests.Theme;

/// <summary>
/// Gates the color tokens on WCAG 2 AA contrast. Text pairs need 4.5:1 (SC 1.4.3) and the
/// boundaries that identify a control need 3:1 (SC 1.4.11). The values are read from the
/// shipped stylesheet and from the theme service output, so a token edited in either place
/// is measured here.
/// </summary>
public partial class TokenContrastTests
{
    private const double Text = 4.5;
    private const double NonText = 3.0;

    private static readonly string[] BaseColors =
    [
        "Slate",
        "Gray",
        "Zinc",
        "Neutral",
        "Stone",
        "Blue",
    ];

    // Foreground token, background token, required ratio.
    private static readonly (string Foreground, string Background, double Minimum)[] Pairs =
    [
        ("foreground", "background", Text),
        ("card-foreground", "card", Text),
        ("popover-foreground", "popover", Text),
        ("primary-foreground", "primary", Text),
        ("secondary-foreground", "secondary", Text),
        ("accent-foreground", "accent", Text),
        ("muted-foreground", "background", Text),
        ("muted-foreground", "card", Text),
        ("muted-foreground", "muted", Text),
        ("sidebar-foreground", "sidebar-background", Text),
        // Status colors are used as a fill under their -foreground...
        ("destructive-foreground", "destructive", Text),
        ("success-foreground", "success", Text),
        ("warning-foreground", "warning", Text),
        ("info-foreground", "info", Text),
        // ...and as text or icons on the page and on muted surfaces.
        ("destructive", "background", Text),
        ("destructive", "muted", Text),
        ("success", "background", Text),
        ("success", "muted", Text),
        ("warning", "background", Text),
        ("warning", "muted", Text),
        ("info", "background", Text),
        ("info", "muted", Text),
        // Control boundaries and the focus indicator.
        ("input", "background", NonText),
        ("input", "card", NonText),
        ("input", "muted", NonText),
        ("ring", "background", NonText),
        ("primary", "background", NonText),
    ];

    public static IEnumerable<object[]> Cases()
    {
        var sources = new List<string> { "vibe-base.css" };
        sources.AddRange(BaseColors.Select(color => $"ThemeService:{color}"));

        foreach (var source in sources)
        {
            foreach (var theme in new[] { "light", "dark" })
            {
                foreach (var pair in Pairs)
                {
                    yield return [source, theme, pair.Foreground, pair.Background, pair.Minimum];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TokenPairMeetsWcag2Contrast(
        string source,
        string theme,
        string foreground,
        string background,
        double minimum
    )
    {
        var tokens = LoadTokens(source, theme);

        tokens.ShouldContainKey(
            foreground,
            $"{source} ({theme}) does not define --vibe-{foreground}"
        );
        tokens.ShouldContainKey(
            background,
            $"{source} ({theme}) does not define --vibe-{background}"
        );

        var ratio = ContrastRatio(tokens[foreground], tokens[background]);

        ratio.ShouldBeGreaterThanOrEqualTo(
            minimum,
            $"{source} ({theme}): --vibe-{foreground} {tokens[foreground]} on --vibe-{background} "
                + $"{tokens[background]} is {ratio:0.00}:1, needs {minimum:0.0}:1"
        );
    }

    [Fact]
    public void ContrastRatioMatchesTheWcagReferenceValues()
    {
        // Guards the measuring code itself: black on white is 21:1 by definition, and
        // #767676 on white is the well-known 4.54:1 boundary grey.
        ContrastRatio("hsl(0 0% 0%)", "hsl(0 0% 100%)").ShouldBe(21.0, 0.01);
        ContrastRatio("hsl(0 0% 46.3%)", "hsl(0 0% 100%)").ShouldBe(4.54, 0.03);
        ContrastRatio("hsl(0 100% 50%)", "hsl(0 0% 100%)").ShouldBe(4.0, 0.01);
    }

    private static Dictionary<string, string> LoadTokens(string source, string theme)
    {
        // The theme service's output is loaded after vibe-base.css and overrides it, so its
        // tokens are measured on top of the base values, as a browser would resolve them.
        var tokens = ReadTheme(File.ReadAllText(BaseCssPath()), theme);

        if (source.StartsWith("ThemeService:", StringComparison.Ordinal))
        {
            var service = new ThemeService(
                new VibeThemeOptions { BaseColor = source["ThemeService:".Length..] }
            );
            foreach (var (name, value) in ReadTheme(service.GenerateThemeCss(), theme))
            {
                tokens[name] = value;
            }
        }

        return tokens;
    }

    private static Dictionary<string, string> ReadTheme(string css, string theme)
    {
        var tokens = ReadBlock(css, ":root");

        if (theme == "dark")
        {
            foreach (var (name, value) in ReadBlock(css, ".dark"))
            {
                tokens[name] = value;
            }
        }

        return tokens;
    }

    private static Dictionary<string, string> ReadBlock(string css, string selector)
    {
        var match = Regex.Match(
            css,
            $@"(?m)^{Regex.Escape(selector)}\s*\{{(?<body>.*?)^\}}",
            RegexOptions.Singleline
        );
        match.Success.ShouldBeTrue($"No '{selector}' block found");

        return ColorToken()
            .Matches(match.Groups["body"].Value)
            .ToDictionary(
                token => token.Groups["name"].Value,
                token => token.Groups["value"].Value
            );
    }

    private static double ContrastRatio(string first, string second)
    {
        var a = RelativeLuminance(first);
        var b = RelativeLuminance(second);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    private static double RelativeLuminance(string hsl)
    {
        var match = Hsl().Match(hsl);
        match.Success.ShouldBeTrue($"'{hsl}' is not an hsl() color this test can measure");

        var hue = Parse(match.Groups["h"].Value) / 360.0;
        var saturation = Parse(match.Groups["s"].Value) / 100.0;
        var lightness = Parse(match.Groups["l"].Value) / 100.0;

        var q =
            lightness < 0.5
                ? lightness * (1 + saturation)
                : lightness + saturation - (lightness * saturation);
        var p = (2 * lightness) - q;

        return (0.2126 * Linear(Channel(p, q, hue + (1.0 / 3))))
            + (0.7152 * Linear(Channel(p, q, hue)))
            + (0.0722 * Linear(Channel(p, q, hue - (1.0 / 3))));
    }

    private static double Channel(double p, double q, double t)
    {
        t =
            t < 0 ? t + 1
            : t > 1 ? t - 1
            : t;

        return t switch
        {
            < 1.0 / 6 => p + ((q - p) * 6 * t),
            < 1.0 / 2 => q,
            < 2.0 / 3 => p + ((q - p) * ((2.0 / 3) - t) * 6),
            _ => p,
        };
    }

    private static double Linear(double channel) =>
        channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);

    private static double Parse(string value) => double.Parse(value, CultureInfo.InvariantCulture);

    private static string BaseCssPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Vibe.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull($"Could not find Vibe.slnx above '{AppContext.BaseDirectory}'");
        return Path.Combine(
            directory.FullName,
            "src",
            "Vibe.UI",
            "wwwroot",
            "css",
            "vibe-base.css"
        );
    }

    [GeneratedRegex(@"--vibe-(?<name>[a-z0-9-]+):\s*(?<value>hsl\([^)]*\))")]
    private static partial Regex ColorToken();

    [GeneratedRegex(@"hsl\(\s*(?<h>[\d.]+)\s+(?<s>[\d.]+)%\s+(?<l>[\d.]+)%\s*\)")]
    private static partial Regex Hsl();
}
