namespace Vibe.CSS.Generator;

/// <summary>
/// Configuration for the Vibe.CSS generator.
/// Defines all available utilities, spacing scales, and color palettes.
/// </summary>
public class VibeConfig
{
    /// <summary>
    /// Prefix for all CSS classes (default: "vibe")
    /// </summary>
    public string Prefix { get; set; } = "vibe";

    /// <summary>
    /// Spacing scale values (used for margin, padding, gap)
    /// Maps to CSS variables: --vibe-spacing-{key}
    /// </summary>
    public Dictionary<string, string> SpacingScale { get; set; } = new()
    {
        ["0"] = "0",
        ["px"] = "1px",
        ["0.5"] = "0.125rem",
        ["1"] = "0.25rem",
        ["1.5"] = "0.375rem",
        ["2"] = "0.5rem",
        ["2.5"] = "0.625rem",
        ["3"] = "0.75rem",
        ["4"] = "1rem",
        ["5"] = "1.25rem",
        ["6"] = "1.5rem",
        ["7"] = "1.75rem",
        ["8"] = "2rem",
        ["9"] = "2.25rem",
        ["10"] = "2.5rem",
        ["11"] = "2.75rem",
        ["12"] = "3rem",
        ["14"] = "3.5rem",
        ["16"] = "4rem",
        ["20"] = "5rem",
        ["24"] = "6rem",
        ["28"] = "7rem",
        ["32"] = "8rem",
        ["36"] = "9rem",
        ["40"] = "10rem",
        ["44"] = "11rem",
        ["48"] = "12rem",
        ["52"] = "13rem",
        ["56"] = "14rem",
        ["60"] = "15rem",
        ["64"] = "16rem",
        ["72"] = "18rem",
        ["80"] = "20rem",
        ["96"] = "24rem"
    };

    /// <summary>
    /// Width/Height sizing values
    /// </summary>
    public Dictionary<string, string> SizingScale { get; set; } = new()
    {
        ["0"] = "0",
        ["px"] = "1px",
        ["0.5"] = "0.125rem",
        ["1"] = "0.25rem",
        ["1.5"] = "0.375rem",
        ["2"] = "0.5rem",
        ["2.5"] = "0.625rem",
        ["3"] = "0.75rem",
        ["4"] = "1rem",
        ["5"] = "1.25rem",
        ["6"] = "1.5rem",
        ["7"] = "1.75rem",
        ["8"] = "2rem",
        ["9"] = "2.25rem",
        ["10"] = "2.5rem",
        ["11"] = "2.75rem",
        ["12"] = "3rem",
        ["14"] = "3.5rem",
        ["16"] = "4rem",
        ["20"] = "5rem",
        ["24"] = "6rem",
        ["28"] = "7rem",
        ["32"] = "8rem",
        ["36"] = "9rem",
        ["40"] = "10rem",
        ["44"] = "11rem",
        ["48"] = "12rem",
        ["52"] = "13rem",
        ["56"] = "14rem",
        ["60"] = "15rem",
        ["64"] = "16rem",
        ["72"] = "18rem",
        ["80"] = "20rem",
        ["96"] = "24rem",
        // Fractions
        ["1/2"] = "50%",
        ["1/3"] = "33.333333%",
        ["2/3"] = "66.666667%",
        ["1/4"] = "25%",
        ["2/4"] = "50%",
        ["3/4"] = "75%",
        ["1/5"] = "20%",
        ["2/5"] = "40%",
        ["3/5"] = "60%",
        ["4/5"] = "80%",
        ["1/6"] = "16.666667%",
        ["5/6"] = "83.333333%",
        ["1/12"] = "8.333333%",
        ["full"] = "100%",
        ["screen"] = "100vw",
        ["svw"] = "100svw",
        ["lvw"] = "100lvw",
        ["dvw"] = "100dvw",
        ["min"] = "min-content",
        ["max"] = "max-content",
        ["fit"] = "fit-content",
        ["auto"] = "auto"
    };

    /// <summary>
    /// Height-specific overrides (screen = 100vh for height)
    /// </summary>
    public Dictionary<string, string> HeightOverrides { get; set; } = new()
    {
        ["screen"] = "100vh",
        ["svh"] = "100svh",
        ["lvh"] = "100lvh",
        ["dvh"] = "100dvh"
    };

    /// <summary>
    /// Font size scale
    /// </summary>
    public Dictionary<string, (string Size, string LineHeight)> FontSizes { get; set; } = new()
    {
        ["xs"] = ("0.75rem", "1rem"),
        ["sm"] = ("0.875rem", "1.25rem"),
        ["base"] = ("1rem", "1.5rem"),
        ["lg"] = ("1.125rem", "1.75rem"),
        ["xl"] = ("1.25rem", "1.75rem"),
        ["2xl"] = ("1.5rem", "2rem"),
        ["3xl"] = ("1.875rem", "2.25rem"),
        ["4xl"] = ("2.25rem", "2.5rem"),
        ["5xl"] = ("3rem", "1"),
        ["6xl"] = ("3.75rem", "1"),
        ["7xl"] = ("4.5rem", "1"),
        ["8xl"] = ("6rem", "1"),
        ["9xl"] = ("8rem", "1")
    };

    /// <summary>
    /// Font weights
    /// </summary>
    public Dictionary<string, string> FontWeights { get; set; } = new()
    {
        ["thin"] = "100",
        ["extralight"] = "200",
        ["light"] = "300",
        ["normal"] = "400",
        ["medium"] = "500",
        ["semibold"] = "600",
        ["bold"] = "700",
        ["extrabold"] = "800",
        ["black"] = "900"
    };

    /// <summary>
    /// Border radius values
    /// </summary>
    public Dictionary<string, string> BorderRadius { get; set; } = new()
    {
        ["none"] = "0",
        ["sm"] = "0.125rem",
        [""] = "0.25rem",
        ["md"] = "0.375rem",
        ["lg"] = "0.5rem",
        ["xl"] = "0.75rem",
        ["2xl"] = "1rem",
        ["3xl"] = "1.5rem",
        ["full"] = "9999px"
    };

    /// <summary>
    /// Opacity values
    /// </summary>
    public Dictionary<string, string> Opacity { get; set; } = new()
    {
        ["0"] = "0",
        ["5"] = "0.05",
        ["10"] = "0.1",
        ["15"] = "0.15",
        ["20"] = "0.2",
        ["25"] = "0.25",
        ["30"] = "0.3",
        ["35"] = "0.35",
        ["40"] = "0.4",
        ["45"] = "0.45",
        ["50"] = "0.5",
        ["55"] = "0.55",
        ["60"] = "0.6",
        ["65"] = "0.65",
        ["70"] = "0.7",
        ["75"] = "0.75",
        ["80"] = "0.8",
        ["85"] = "0.85",
        ["90"] = "0.9",
        ["95"] = "0.95",
        ["100"] = "1"
    };

    /// <summary>
    /// Z-index values
    /// </summary>
    public Dictionary<string, string> ZIndex { get; set; } = new()
    {
        ["0"] = "0",
        ["10"] = "10",
        ["20"] = "20",
        ["30"] = "30",
        ["40"] = "40",
        ["50"] = "50",
        ["auto"] = "auto"
    };

    /// <summary>
    /// Grid column spans
    /// </summary>
    public int MaxGridColumns { get; set; } = 12;

    /// <summary>
    /// Responsive breakpoints
    /// </summary>
    public Dictionary<string, string> Breakpoints { get; set; } = new()
    {
        ["sm"] = "640px",
        ["md"] = "768px",
        ["lg"] = "1024px",
        ["xl"] = "1280px",
        ["2xl"] = "1536px"
    };

    /// <summary>
    /// Enable responsive variants
    /// </summary>
    public bool EnableResponsive { get; set; } = true;

    /// <summary>
    /// Enable hover/focus variants
    /// </summary>
    public bool EnableStateVariants { get; set; } = true;

    /// <summary>
    /// Enable dark mode variants
    /// </summary>
    public bool EnableDarkMode { get; set; } = true;
}
