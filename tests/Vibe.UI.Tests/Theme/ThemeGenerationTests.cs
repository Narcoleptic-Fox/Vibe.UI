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
    // The tokens the theme service writes for a palette, in the order it writes them.
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
            ThemeArtifacts.RenderPaletteData(ThemeArtifacts.LoadPalettes())
        );
    }

    [Fact]
    public void StylesheetColorsAreGeneratedFromTheDefaultDefinition()
    {
        var stylesheet = ThemeArtifacts.ReadRepositoryFile(ThemeArtifacts.StylesheetPath);
        var definition = ThemeArtifacts.LoadAll()[ThemeArtifacts.DefaultTheme];

        ThemeArtifacts.AssertUpToDate(
            ThemeArtifacts.StylesheetPath,
            ThemeArtifacts.RenderStylesheet(stylesheet, definition)
        );
    }

    [Fact]
    public void EveryPaletteDefinesExactlyThePaletteTokensInBothModes()
    {
        var palettes = ThemeArtifacts.LoadPalettes();
        palettes.ShouldNotBeEmpty();

        foreach (var palette in palettes)
        {
            palette
                .Light.Select(token => token.Key)
                .ShouldBe(PaletteTokens, $"{palette.Name} (light)");
            palette
                .Dark.Select(token => token.Key)
                .ShouldBe(PaletteTokens, $"{palette.Name} (dark)");
        }
    }

    [Fact]
    public void DefaultDefinitionCoversEveryPaletteTokenAndBothModesAgree()
    {
        var definition = ThemeArtifacts.LoadAll()[ThemeArtifacts.DefaultTheme];
        var light = definition.Light.Select(token => token.Key).ToArray();
        var dark = definition.Dark.Select(token => token.Key).ToArray();

        dark.ShouldBe(light, "Light and dark must define the same tokens in the same order");
        foreach (var token in PaletteTokens)
        {
            light.ShouldContain(token);
        }
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
        exception.Message.ShouldContain("Slate");
        exception.Message.ShouldContain("Custom");
    }

    [Fact]
    public void CustomBaseColorStartsFromSlateAndAppliesOnlyWhatIsSet()
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
        var slate = new ThemeService(
            new VibeThemeOptions { BaseColor = "Slate" }
        ).GenerateThemeCss();

        var light = css[..css.IndexOf(".dark", StringComparison.Ordinal)];
        light.ShouldContain("--vibe-primary: hsl(346 77% 40%);");
        light.ShouldContain("--vibe-background: hsl(30 20% 98%);");
        // Surfaces that were not set follow the custom background, as cards do in every built-in palette.
        light.ShouldContain("--vibe-card: hsl(30 20% 98%);");
        // The focus ring follows the custom primary.
        light.ShouldContain("--vibe-ring: hsl(346 77% 40%);");
        // Anything else keeps the Slate value.
        light.ShouldContain("--vibe-border: hsl(214.3 31.8% 91.4%);");

        // Dark was not customized at all, so it is Slate's dark palette unchanged.
        css[css.IndexOf(".dark", StringComparison.Ordinal)..]
            .ShouldBe(slate[slate.IndexOf(".dark", StringComparison.Ordinal)..]);
    }
}
