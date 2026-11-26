using System.Text;
using Vibe.UI.Configuration;

namespace Vibe.UI.Services.Theme;

/// <summary>
/// Service for generating theme CSS based on configuration.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Generates CSS for the configured theme.
    /// </summary>
    string GenerateThemeCss();
}

/// <summary>
/// Default implementation of theme service.
/// </summary>
/// <remarks>
/// The built-in themes are not written here. They live in <c>themes/*.json</c> and reach this
/// class through the generated <see cref="ThemePaletteData"/>.
/// </remarks>
public class ThemeService : IThemeService
{
    private const string CustomBaseColor = "Custom";

    // A custom theme changes only the colors it sets; everything else comes from this theme.
    private const string CustomStartsFrom = ThemePaletteData.Default;

    private readonly VibeThemeOptions _options;

    /// <summary>
    /// The names of the built-in themes, the default first. Use one as
    /// <see cref="VibeThemeOptions.BaseColor"/>, or pass it to <see cref="GenerateBuiltInThemeCss"/>.
    /// </summary>
    public static IReadOnlyList<string> BuiltInThemes { get; } =
        [ThemePaletteData.Default, .. ThemePaletteData.Names.Where(name => name != ThemePaletteData.Default)];

    /// <summary>
    /// The CSS an app configured with this built-in theme renders: its <c>:root</c> and
    /// <c>.dark</c> blocks. Lay it over the page after the theme already there to switch to it.
    /// </summary>
    /// <param name="theme">One of <see cref="BuiltInThemes"/>.</param>
    public static string GenerateBuiltInThemeCss(string theme)
    {
        if (!BuiltInThemes.Contains(theme))
        {
            throw new ArgumentException(
                $"Unknown theme: {theme}. Use one of {string.Join(", ", BuiltInThemes)}.",
                nameof(theme)
            );
        }

        return new ThemeService(new VibeThemeOptions { BaseColor = theme }).GenerateThemeCss();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeService"/> class.
    /// </summary>
    /// <param name="options">The theme options used to generate CSS.</param>
    public ThemeService(VibeThemeOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public string GenerateThemeCss()
    {
        var custom = _options.BaseColor == CustomBaseColor;
        var palette = custom ? CustomStartsFrom : _options.BaseColor;

        var light = ThemePaletteData.Find(palette, dark: false);
        var dark = ThemePaletteData.Find(palette, dark: true);
        if (light is null || dark is null)
        {
            throw new ArgumentException(
                $"Unknown base color: {_options.BaseColor}. "
                    + $"Use one of {string.Join(", ", ThemePaletteData.Names)}, or {CustomBaseColor}."
            );
        }

        if (custom)
        {
            light = ApplyCustomColors(light, _options.LightColors);
            dark = ApplyCustomColors(dark, _options.DarkColors);
        }

        var css = new StringBuilder();
        css.Append(":root {\n");
        AppendTokens(css, light);
        css.Append($"    --vibe-radius: {_options.BorderRadius};\n");
        css.Append("}\n\n.dark {\n");
        AppendTokens(css, dark);
        css.Append('}');

        return css.ToString();
    }

    private static void AppendTokens(StringBuilder css, KeyValuePair<string, string>[] tokens)
    {
        foreach (var (name, value) in tokens)
        {
            css.Append($"    --vibe-{name}: {value};\n");
        }
    }

    /// <summary>
    /// Lays the custom colors over a theme. A color that is not set keeps the theme's value,
    /// except the ones that follow another color: text on a surface follows the foreground, the
    /// hover fill follows secondary, and the focus ring and links follow primary. Surfaces do
    /// not follow the background: each is its own level of the page.
    /// </summary>
    private static KeyValuePair<string, string>[] ApplyCustomColors(
        KeyValuePair<string, string>[] palette,
        CustomThemeColors? custom
    )
    {
        if (custom is null)
        {
            return palette;
        }

        var start = palette.ToDictionary(token => token.Key, token => token.Value);

        var background = custom.Background ?? start["background"];
        var foreground = custom.Foreground ?? start["foreground"];
        var primary = custom.Primary ?? start["primary"];
        var secondary = custom.Secondary ?? start["secondary"];

        var colors = new Dictionary<string, string>
        {
            ["background"] = background,
            ["foreground"] = foreground,
            ["card"] = custom.Card ?? start["card"],
            ["card-foreground"] = custom.CardForeground ?? Follow(custom.Foreground, start["card-foreground"]),
            ["popover"] = custom.Popover ?? start["popover"],
            ["popover-foreground"] = custom.PopoverForeground ?? Follow(custom.Foreground, start["popover-foreground"]),
            ["primary"] = primary,
            ["primary-foreground"] = custom.PrimaryForeground ?? start["primary-foreground"],
            ["secondary"] = secondary,
            ["secondary-foreground"] = custom.SecondaryForeground ?? Follow(custom.Foreground, start["secondary-foreground"]),
            ["muted"] = custom.Muted ?? start["muted"],
            ["muted-foreground"] = custom.MutedForeground ?? start["muted-foreground"],
            ["accent"] = custom.Accent ?? Follow(custom.Secondary, start["accent"]),
            ["accent-foreground"] = custom.AccentForeground ?? Follow(custom.Foreground, start["accent-foreground"]),
            ["destructive"] = custom.Destructive ?? start["destructive"],
            ["destructive-foreground"] =
                custom.DestructiveForeground ?? start["destructive-foreground"],
            ["border"] = custom.Border ?? start["border"],
            ["input"] = custom.Input ?? start["input"],
            ["ring"] = custom.Ring ?? Follow(custom.Primary, start["ring"]),
            ["link"] = Follow(custom.Primary, start["link"]),
            ["sidebar-foreground"] = Follow(custom.Foreground, start["sidebar-foreground"]),
        };

        // A color that follows another takes the custom leader when there is one, and otherwise
        // keeps the palette's own value.
        static string Follow(string? customLeader, string paletteValue) => customLeader ?? paletteValue;

        // Keep the theme's token order, so the output reads the same for every theme. A token
        // with no custom property (status colors, the sidebar, charts) keeps the theme's value.
        return palette
            .Select(token => new KeyValuePair<string, string>(
                token.Key,
                colors.GetValueOrDefault(token.Key, token.Value)
            ))
            .ToArray();
    }
}
