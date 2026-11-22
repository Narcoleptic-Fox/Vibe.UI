using System.Text.RegularExpressions;
using Vibe.UI.Configuration;
using Vibe.UI.Services.Theme;

namespace Vibe.UI.Tests.Theme;

/// <summary>
/// The theme definitions in <c>themes/*.json</c> are the one source of every color. These tests
/// hold the generated files to them, and hold the definitions to a common shape.
/// </summary>
public class ThemeGenerationTests
{
    // The tokens every theme starts with, in order. shadcn/ui themes use the same names.
    private static readonly string[] PaletteTokens =
    [
        "background",
        "foreground",
        "card",
        "card-foreground",
        "popover",
        "popover-foreground",
        "primary",
        "primary-foreground",
        "secondary",
        "secondary-foreground",
        "muted",
        "muted-foreground",
        "accent",
        "accent-foreground",
        "destructive",
        "destructive-foreground",
        "border",
        "input",
        "ring",
    ];

    [Fact]
    public void PaletteDataIsGeneratedFromTheDefinitions()
    {
        ThemeArtifacts.AssertUpToDate(
            ThemeArtifacts.PaletteDataPath,
            ThemeArtifacts.RenderPaletteData(
                ThemeArtifacts.LoadPalettes(),
                ThemeArtifacts.LoadDefault()
            )
        );
    }

    [Fact]
    public void StylesheetColorsAreGeneratedFromTheDefaultDefinition()
    {
        var stylesheet = ThemeArtifacts.ReadRepositoryFile(ThemeArtifacts.StylesheetPath);
        var definition = ThemeArtifacts.LoadDefault();

        ThemeArtifacts.AssertUpToDate(
            ThemeArtifacts.StylesheetPath,
            ThemeArtifacts.RenderStylesheet(stylesheet, definition)
        );
    }

    [Fact]
    public void EveryThemeDefinesTheSameTokensInTheSameOrderInBothModes()
    {
        var themes = ThemeArtifacts.LoadPalettes();
        themes.ShouldNotBeEmpty();

        // A theme that left a token out would silently keep the default theme's color for it.
        var expected = ThemeArtifacts.LoadDefault().Light.Select(token => token.Key).ToArray();
        expected.Take(PaletteTokens.Length).ShouldBe(PaletteTokens);

        foreach (var theme in themes)
        {
            theme.Light.Select(token => token.Key).ShouldBe(expected, $"{theme.Name} (light)");
            theme.Dark.Select(token => token.Key).ShouldBe(expected, $"{theme.Name} (dark)");
        }
    }

    [Fact]
    public void TheDefaultThemeIsTheDefaultBaseColor()
    {
        new VibeThemeOptions().BaseColor.ShouldBe(ThemeArtifacts.LoadDefault().Name);
    }

    [Fact]
    public void EveryDefinitionValueIsAnHslColor()
    {
        foreach (var (file, definition) in ThemeArtifacts.LoadAll())
        {
            foreach (var (name, value) in definition.Light.Concat(definition.Dark))
            {
                Regex
                    .IsMatch(value, @"^hsl\(\d+(\.\d+)? \d+(\.\d+)?% \d+(\.\d+)?%\)$")
                    .ShouldBeTrue(
                        $"themes/{file}.json: '{name}' is '{value}', expected the form hsl(H S% L%)"
                    );
            }
        }
    }

    [Fact]
    public void ThemeServiceWritesEachPaletteFromItsDefinition()
    {
        foreach (var palette in ThemeArtifacts.LoadPalettes())
        {
            var css = new ThemeService(
                new VibeThemeOptions { BaseColor = palette.Name }
            ).GenerateThemeCss();

            foreach (
                var (mode, tokens) in new[] { (":root", palette.Light), (".dark", palette.Dark) }
            )
            {
                var body = Regex
                    .Match(css, $@"(?ms)^{Regex.Escape(mode)} \{{(.*?)^\}}")
                    .Groups[1]
                    .Value;
                foreach (var (name, value) in tokens)
                {
                    body.ShouldContain(
                        $"--vibe-{name}: {value};",
                        customMessage: $"{palette.Name} {mode}"
                    );
                }
            }
        }
    }

    [Fact]
    public void UnknownBaseColorNamesTheAcceptedOnes()
    {
        var exception = Should.Throw<ArgumentException>(() =>
            new ThemeService(new VibeThemeOptions { BaseColor = "Chartreuse" }).GenerateThemeCss()
        );

        exception.Message.ShouldContain("Chartreuse");
        foreach (var theme in ThemeArtifacts.LoadPalettes())
        {
            exception.Message.ShouldContain(theme.Name);
        }

        exception.Message.ShouldContain("Custom");
    }

    [Fact]
    public void CustomBaseColorStartsFromTheDefaultThemeAndAppliesOnlyWhatIsSet()
    {
        var options = new VibeThemeOptions
        {
            BaseColor = "Custom",
            LightColors = new CustomThemeColors
            {
                Primary = "hsl(346 77% 40%)",
                Background = "hsl(30 20% 98%)",
            },
        };

        var css = new ThemeService(options).GenerateThemeCss();
        var defaultTheme = ThemeArtifacts.LoadDefault();
        var defaultCss = new ThemeService(
            new VibeThemeOptions { BaseColor = defaultTheme.Name }
        ).GenerateThemeCss();
        string Default(string token) => defaultTheme.Light.Single(pair => pair.Key == token).Value;

        var light = css[..css.IndexOf(".dark", StringComparison.Ordinal)];
        light.ShouldContain("--vibe-primary: hsl(346 77% 40%);");
        light.ShouldContain("--vibe-background: hsl(30 20% 98%);");
        // The focus ring and the link color follow the custom primary.
        light.ShouldContain("--vibe-ring: hsl(346 77% 40%);");
        light.ShouldContain("--vibe-link: hsl(346 77% 40%);");
        // Surfaces keep their own level: a custom page color does not repaint the cards.
        light.ShouldContain($"--vibe-card: {Default("card")};");
        // Anything else keeps the default theme's value, including tokens with no custom property.
        light.ShouldContain($"--vibe-border: {Default("border")};");
        light.ShouldContain($"--vibe-success-text: {Default("success-text")};");

        // Dark was not customized at all, so it is the default theme's dark colors unchanged.
        css[css.IndexOf(".dark", StringComparison.Ordinal)..]
            .ShouldBe(defaultCss[defaultCss.IndexOf(".dark", StringComparison.Ordinal)..]);
    }
}
