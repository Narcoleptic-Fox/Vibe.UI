using System.Text.RegularExpressions;

namespace Vibe.CSS.Generator;

/// <summary>
/// JIT generator for CSS utilities. Given a class name, generates the corresponding CSS rule.
/// </summary>
public partial class UtilityGenerator
{
    private readonly VibeConfig _config;
    private readonly string _prefix;

    public UtilityGenerator(VibeConfig? config = null)
    {
        _config = config ?? new VibeConfig();
        _prefix = _config.Prefix;
    }

    /// <summary>
    /// Try to generate a CSS rule for the given class name.
    /// Returns null if the class name is not recognized.
    /// </summary>
    public CssRule? Generate(string className)
    {
        // Check for variants FIRST (they come before the prefix)
        // e.g., "hover:vibe-bg-primary" or "sm:vibe-flex"
        var (variant, afterVariant) = ExtractVariant(className);

        // Now work with the part after the variant
        var name = afterVariant;

        // Strip prefix if present
        if (name.StartsWith($"{_prefix}-"))
        {
            name = name[(_prefix.Length + 1)..];
        }
        else if (!string.IsNullOrEmpty(_prefix))
        {
            // Class doesn't match our prefix
            return null;
        }

        // Try to generate the base rule
        var rule = GenerateBaseRule(name, className);

        if (rule == null)
            return null;

        // Apply variant if present
        if (variant != null)
        {
            rule = ApplyVariant(rule, variant, className);
        }

        return rule;
    }

    private (string? Variant, string BaseName) ExtractVariant(string name)
    {
        // Check for state variants: hover:, focus:, active:, etc.
        var stateVariants = new[] { "hover", "focus", "active", "disabled", "visited", "focus-visible", "focus-within", "first", "last", "odd", "even" };
        foreach (var v in stateVariants)
        {
            if (name.StartsWith($"{v}:"))
            {
                return (v, name[(v.Length + 1)..]);
            }
        }

        // Check for responsive variants: sm:, md:, lg:, xl:, 2xl:
        foreach (var bp in _config.Breakpoints.Keys)
        {
            if (name.StartsWith($"{bp}:"))
            {
                return ($"@{bp}", name[(bp.Length + 1)..]);
            }
        }

        // Check for dark mode
        if (name.StartsWith("dark:"))
        {
            return ("dark", name[5..]);
        }

        return (null, name);
    }

    private CssRule? ApplyVariant(CssRule rule, string variant, string fullClassName)
    {
        var selector = $".{EscapeSelector(fullClassName)}";

        // State variants (pseudo-classes)
        if (variant is "hover" or "focus" or "active" or "disabled" or "visited" or "focus-visible" or "focus-within")
        {
            return rule with
            {
                Selector = $"{selector}:{variant}",
                Order = rule.Order + CssOrder.StateVariants
            };
        }

        // Structural pseudo-classes
        if (variant is "first")
        {
            return rule with
            {
                Selector = $"{selector}:first-child",
                Order = rule.Order + CssOrder.StateVariants
            };
        }

        if (variant is "last")
        {
            return rule with
            {
                Selector = $"{selector}:last-child",
                Order = rule.Order + CssOrder.StateVariants
            };
        }

        if (variant is "odd")
        {
            return rule with
            {
                Selector = $"{selector}:nth-child(odd)",
                Order = rule.Order + CssOrder.StateVariants
            };
        }

        if (variant is "even")
        {
            return rule with
            {
                Selector = $"{selector}:nth-child(even)",
                Order = rule.Order + CssOrder.StateVariants
            };
        }

        // Dark mode
        if (variant == "dark")
        {
            return rule with
            {
                Selector = $".dark {selector}",
                Order = rule.Order + CssOrder.StateVariants
            };
        }

        // Responsive variants
        if (variant.StartsWith("@") && _config.Breakpoints.TryGetValue(variant[1..], out var minWidth))
        {
            return rule with
            {
                Selector = selector,
                MediaQuery = $"@media (min-width: {minWidth})",
                Order = rule.Order + CssOrder.ResponsiveVariants
            };
        }

        return rule;
    }

    private CssRule? GenerateBaseRule(string name, string fullClassName)
    {
        var selector = $".{EscapeSelector(fullClassName)}";

        // Try each generator in order
        return TryGenerateDisplay(name, selector)
            ?? TryGenerateFlexbox(name, selector)
            ?? TryGenerateGrid(name, selector)
            ?? TryGenerateSpacing(name, selector)
            ?? TryGenerateSizing(name, selector)
            ?? TryGenerateTypography(name, selector)
            ?? TryGenerateColor(name, selector)
            ?? TryGenerateBorder(name, selector)
            ?? TryGenerateEffects(name, selector)
            ?? TryGenerateLayout(name, selector)
            ?? TryGenerateInteractivity(name, selector)
            ?? TryGenerateArbitrary(name, selector);
    }

    #region Display Utilities

    private CssRule? TryGenerateDisplay(string name, string selector)
    {
        var displays = new Dictionary<string, string>
        {
            ["hidden"] = "none",
            ["block"] = "block",
            ["inline"] = "inline",
            ["inline-block"] = "inline-block",
            ["flex"] = "flex",
            ["inline-flex"] = "inline-flex",
            ["grid"] = "grid",
            ["inline-grid"] = "inline-grid",
            ["contents"] = "contents",
            ["flow-root"] = "flow-root",
            ["table"] = "table",
            ["table-row"] = "table-row",
            ["table-cell"] = "table-cell"
        };

        if (displays.TryGetValue(name, out var value))
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = $"display: {value};",
                Order = CssOrder.Layout
            };
        }

        return null;
    }

    #endregion

    #region Flexbox Utilities

    private CssRule? TryGenerateFlexbox(string name, string selector)
    {
        // Flex direction
        var directions = new Dictionary<string, string>
        {
            ["flex-row"] = "row",
            ["flex-row-reverse"] = "row-reverse",
            ["flex-col"] = "column",
            ["flex-col-reverse"] = "column-reverse"
        };

        if (directions.TryGetValue(name, out var dir))
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = $"flex-direction: {dir};",
                Order = CssOrder.Flexbox
            };
        }

        // Flex wrap
        var wraps = new Dictionary<string, string>
        {
            ["flex-wrap"] = "wrap",
            ["flex-wrap-reverse"] = "wrap-reverse",
            ["flex-nowrap"] = "nowrap"
        };

        if (wraps.TryGetValue(name, out var wrap))
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = $"flex-wrap: {wrap};",
                Order = CssOrder.Flexbox
            };
        }

        // Flex grow/shrink
        var flexValues = new Dictionary<string, string>
        {
            ["flex-1"] = "1 1 0%",
            ["flex-auto"] = "1 1 auto",
            ["flex-initial"] = "0 1 auto",
            ["flex-none"] = "none",
            ["grow"] = "1",
            ["grow-0"] = "0",
            ["shrink"] = "1",
            ["shrink-0"] = "0"
        };

        if (flexValues.TryGetValue(name, out var flex))
        {
            var prop = name.StartsWith("grow") ? "flex-grow" :
                       name.StartsWith("shrink") ? "flex-shrink" : "flex";
            return new CssRule
            {
                Selector = selector,
                Declarations = $"{prop}: {flex};",
                Order = CssOrder.Flexbox
            };
        }

        // Align items
        if (name.StartsWith("items-"))
        {
            var alignItems = new Dictionary<string, string>
            {
                ["items-start"] = "flex-start",
                ["items-end"] = "flex-end",
                ["items-center"] = "center",
                ["items-baseline"] = "baseline",
                ["items-stretch"] = "stretch"
            };

            if (alignItems.TryGetValue(name, out var align))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"align-items: {align};",
                    Order = CssOrder.Flexbox
                };
            }
        }

        // Justify content
        if (name.StartsWith("justify-"))
        {
            var justifyContent = new Dictionary<string, string>
            {
                ["justify-start"] = "flex-start",
                ["justify-end"] = "flex-end",
                ["justify-center"] = "center",
                ["justify-between"] = "space-between",
                ["justify-around"] = "space-around",
                ["justify-evenly"] = "space-evenly",
                ["justify-stretch"] = "stretch"
            };

            if (justifyContent.TryGetValue(name, out var justify))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"justify-content: {justify};",
                    Order = CssOrder.Flexbox
                };
            }
        }

        // Align self
        if (name.StartsWith("self-"))
        {
            var alignSelf = new Dictionary<string, string>
            {
                ["self-auto"] = "auto",
                ["self-start"] = "flex-start",
                ["self-end"] = "flex-end",
                ["self-center"] = "center",
                ["self-stretch"] = "stretch",
                ["self-baseline"] = "baseline"
            };

            if (alignSelf.TryGetValue(name, out var self))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"align-self: {self};",
                    Order = CssOrder.Flexbox
                };
            }
        }

        // Gap
        if (name.StartsWith("gap-"))
        {
            var gapValue = name[4..];

            // gap-x- and gap-y-
            if (gapValue.StartsWith("x-") && _config.SpacingScale.TryGetValue(gapValue[2..], out var gapX))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"column-gap: {gapX};",
                    Order = CssOrder.Flexbox
                };
            }

            if (gapValue.StartsWith("y-") && _config.SpacingScale.TryGetValue(gapValue[2..], out var gapY))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"row-gap: {gapY};",
                    Order = CssOrder.Flexbox
                };
            }

            // Regular gap
            if (_config.SpacingScale.TryGetValue(gapValue, out var gap))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"gap: {gap};",
                    Order = CssOrder.Flexbox
                };
            }
        }

        return null;
    }

    #endregion

    #region Grid Utilities

    private CssRule? TryGenerateGrid(string name, string selector)
    {
        // Grid template columns
        var colsMatch = GridColsRegex().Match(name);
        if (colsMatch.Success)
        {
            var cols = colsMatch.Groups[1].Value;
            if (cols == "none")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "grid-template-columns: none;",
                    Order = CssOrder.Grid
                };
            }
            if (cols == "subgrid")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "grid-template-columns: subgrid;",
                    Order = CssOrder.Grid
                };
            }
            if (int.TryParse(cols, out var numCols) && numCols >= 1 && numCols <= _config.MaxGridColumns)
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"grid-template-columns: repeat({numCols}, minmax(0, 1fr));",
                    Order = CssOrder.Grid
                };
            }
        }

        // Grid template rows
        var rowsMatch = GridRowsRegex().Match(name);
        if (rowsMatch.Success)
        {
            var rows = rowsMatch.Groups[1].Value;
            if (rows == "none")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "grid-template-rows: none;",
                    Order = CssOrder.Grid
                };
            }
            if (rows == "subgrid")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "grid-template-rows: subgrid;",
                    Order = CssOrder.Grid
                };
            }
            if (int.TryParse(rows, out var numRows) && numRows >= 1 && numRows <= _config.MaxGridColumns)
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"grid-template-rows: repeat({numRows}, minmax(0, 1fr));",
                    Order = CssOrder.Grid
                };
            }
        }

        // Column span
        var colSpanMatch = ColSpanRegex().Match(name);
        if (colSpanMatch.Success)
        {
            var span = colSpanMatch.Groups[1].Value;
            if (span == "auto")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "grid-column: auto;",
                    Order = CssOrder.Grid
                };
            }
            if (span == "full")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "grid-column: 1 / -1;",
                    Order = CssOrder.Grid
                };
            }
            if (int.TryParse(span, out var numSpan) && numSpan >= 1 && numSpan <= _config.MaxGridColumns)
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"grid-column: span {numSpan} / span {numSpan};",
                    Order = CssOrder.Grid
                };
            }
        }

        // Row span
        var rowSpanMatch = RowSpanRegex().Match(name);
        if (rowSpanMatch.Success)
        {
            var span = rowSpanMatch.Groups[1].Value;
            if (span == "auto")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "grid-row: auto;",
                    Order = CssOrder.Grid
                };
            }
            if (span == "full")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "grid-row: 1 / -1;",
                    Order = CssOrder.Grid
                };
            }
            if (int.TryParse(span, out var numSpan) && numSpan >= 1 && numSpan <= _config.MaxGridColumns)
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"grid-row: span {numSpan} / span {numSpan};",
                    Order = CssOrder.Grid
                };
            }
        }

        return null;
    }

    [GeneratedRegex(@"^grid-cols-(.+)$")]
    private static partial Regex GridColsRegex();

    [GeneratedRegex(@"^grid-rows-(.+)$")]
    private static partial Regex GridRowsRegex();

    [GeneratedRegex(@"^col-span-(.+)$")]
    private static partial Regex ColSpanRegex();

    [GeneratedRegex(@"^row-span-(.+)$")]
    private static partial Regex RowSpanRegex();

    #endregion

    #region Spacing Utilities (Margin & Padding)

    private CssRule? TryGenerateSpacing(string name, string selector)
    {
        // Padding
        if (name.StartsWith("p"))
        {
            var rule = TryGeneratePadding(name, selector);
            if (rule != null) return rule;
        }

        // Margin
        if (name.StartsWith("m") || name.StartsWith("-m"))
        {
            var rule = TryGenerateMargin(name, selector);
            if (rule != null) return rule;
        }

        // Space between (space-x-*, space-y-*)
        if (name.StartsWith("space-"))
        {
            return TryGenerateSpaceBetween(name, selector);
        }

        return null;
    }

    private CssRule? TryGeneratePadding(string name, string selector)
    {
        // p-{size}, px-{size}, py-{size}, pt-{size}, pr-{size}, pb-{size}, pl-{size}
        var paddingPatterns = new Dictionary<string, string>
        {
            ["p-"] = "padding",
            ["px-"] = "padding-left|padding-right",
            ["py-"] = "padding-top|padding-bottom",
            ["pt-"] = "padding-top",
            ["pr-"] = "padding-right",
            ["pb-"] = "padding-bottom",
            ["pl-"] = "padding-left",
            ["ps-"] = "padding-inline-start",
            ["pe-"] = "padding-inline-end"
        };

        foreach (var (prefix, props) in paddingPatterns)
        {
            if (name.StartsWith(prefix))
            {
                var value = name[prefix.Length..];
                if (_config.SpacingScale.TryGetValue(value, out var size))
                {
                    var declarations = string.Join(" ", props.Split('|').Select(p => $"{p}: {size};"));
                    return new CssRule
                    {
                        Selector = selector,
                        Declarations = declarations,
                        Order = CssOrder.Spacing
                    };
                }
            }
        }

        return null;
    }

    private CssRule? TryGenerateMargin(string name, string selector)
    {
        var isNegative = name.StartsWith("-");
        var baseName = isNegative ? name[1..] : name;

        var marginPatterns = new Dictionary<string, string>
        {
            ["m-"] = "margin",
            ["mx-"] = "margin-left|margin-right",
            ["my-"] = "margin-top|margin-bottom",
            ["mt-"] = "margin-top",
            ["mr-"] = "margin-right",
            ["mb-"] = "margin-bottom",
            ["ml-"] = "margin-left",
            ["ms-"] = "margin-inline-start",
            ["me-"] = "margin-inline-end"
        };

        foreach (var (prefix, props) in marginPatterns)
        {
            if (baseName.StartsWith(prefix))
            {
                var value = baseName[prefix.Length..];

                // Handle auto
                if (value == "auto")
                {
                    var declarations = string.Join(" ", props.Split('|').Select(p => $"{p}: auto;"));
                    return new CssRule
                    {
                        Selector = selector,
                        Declarations = declarations,
                        Order = CssOrder.Spacing
                    };
                }

                if (_config.SpacingScale.TryGetValue(value, out var size))
                {
                    var actualSize = isNegative ? $"-{size}" : size;
                    var declarations = string.Join(" ", props.Split('|').Select(p => $"{p}: {actualSize};"));
                    return new CssRule
                    {
                        Selector = selector,
                        Declarations = declarations,
                        Order = CssOrder.Spacing
                    };
                }
            }
        }

        return null;
    }

    private CssRule? TryGenerateSpaceBetween(string name, string selector)
    {
        // space-x-{size}, space-y-{size}
        if (name.StartsWith("space-x-"))
        {
            var value = name[8..];
            if (_config.SpacingScale.TryGetValue(value, out var size))
            {
                return new CssRule
                {
                    Selector = $"{selector} > :not([hidden]) ~ :not([hidden])",
                    Declarations = $"--tw-space-x-reverse: 0; margin-right: calc({size} * var(--tw-space-x-reverse)); margin-left: calc({size} * calc(1 - var(--tw-space-x-reverse)));",
                    Order = CssOrder.Spacing
                };
            }
        }

        if (name.StartsWith("space-y-"))
        {
            var value = name[8..];
            if (_config.SpacingScale.TryGetValue(value, out var size))
            {
                return new CssRule
                {
                    Selector = $"{selector} > :not([hidden]) ~ :not([hidden])",
                    Declarations = $"--tw-space-y-reverse: 0; margin-bottom: calc({size} * var(--tw-space-y-reverse)); margin-top: calc({size} * calc(1 - var(--tw-space-y-reverse)));",
                    Order = CssOrder.Spacing
                };
            }
        }

        return null;
    }

    #endregion

    #region Sizing Utilities (Width & Height)

    private CssRule? TryGenerateSizing(string name, string selector)
    {
        // Width
        if (name.StartsWith("w-"))
        {
            var value = name[2..];
            if (TryGetSizeValue(value, false, out var size))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"width: {size};",
                    Order = CssOrder.Sizing
                };
            }
        }

        // Height
        if (name.StartsWith("h-"))
        {
            var value = name[2..];
            if (TryGetSizeValue(value, true, out var size))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"height: {size};",
                    Order = CssOrder.Sizing
                };
            }
        }

        // Min-width
        if (name.StartsWith("min-w-"))
        {
            var value = name[6..];
            if (TryGetSizeValue(value, false, out var size))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"min-width: {size};",
                    Order = CssOrder.Sizing
                };
            }
        }

        // Min-height
        if (name.StartsWith("min-h-"))
        {
            var value = name[6..];
            if (TryGetSizeValue(value, true, out var size))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"min-height: {size};",
                    Order = CssOrder.Sizing
                };
            }
        }

        // Max-width
        if (name.StartsWith("max-w-"))
        {
            var value = name[6..];

            // Special max-width values
            var maxWidths = new Dictionary<string, string>
            {
                ["none"] = "none",
                ["xs"] = "20rem",
                ["sm"] = "24rem",
                ["md"] = "28rem",
                ["lg"] = "32rem",
                ["xl"] = "36rem",
                ["2xl"] = "42rem",
                ["3xl"] = "48rem",
                ["4xl"] = "56rem",
                ["5xl"] = "64rem",
                ["6xl"] = "72rem",
                ["7xl"] = "80rem",
                ["full"] = "100%",
                ["min"] = "min-content",
                ["max"] = "max-content",
                ["fit"] = "fit-content",
                ["prose"] = "65ch",
                ["screen-sm"] = "640px",
                ["screen-md"] = "768px",
                ["screen-lg"] = "1024px",
                ["screen-xl"] = "1280px",
                ["screen-2xl"] = "1536px"
            };

            if (maxWidths.TryGetValue(value, out var maxW))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"max-width: {maxW};",
                    Order = CssOrder.Sizing
                };
            }

            if (TryGetSizeValue(value, false, out var size))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"max-width: {size};",
                    Order = CssOrder.Sizing
                };
            }
        }

        // Max-height
        if (name.StartsWith("max-h-"))
        {
            var value = name[6..];
            if (TryGetSizeValue(value, true, out var size))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"max-height: {size};",
                    Order = CssOrder.Sizing
                };
            }
        }

        // Size (width + height)
        if (name.StartsWith("size-"))
        {
            var value = name[5..];
            if (TryGetSizeValue(value, false, out var size))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"width: {size}; height: {size};",
                    Order = CssOrder.Sizing
                };
            }
        }

        return null;
    }

    private bool TryGetSizeValue(string key, bool isHeight, out string value)
    {
        // Check height overrides first (for screen -> vh)
        if (isHeight && _config.HeightOverrides.TryGetValue(key, out value!))
        {
            return true;
        }

        // Check sizing scale
        if (_config.SizingScale.TryGetValue(key, out value!))
        {
            return true;
        }

        // Check spacing scale (for numeric values)
        if (_config.SpacingScale.TryGetValue(key, out value!))
        {
            return true;
        }

        value = string.Empty;
        return false;
    }

    #endregion

    #region Typography Utilities

    private CssRule? TryGenerateTypography(string name, string selector)
    {
        // Font size
        if (name.StartsWith("text-"))
        {
            var value = name[5..];

            // Check if it's a font size
            if (_config.FontSizes.TryGetValue(value, out var fontSize))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"font-size: {fontSize.Size}; line-height: {fontSize.LineHeight};",
                    Order = CssOrder.Typography
                };
            }

            // Text alignment
            var alignments = new Dictionary<string, string>
            {
                ["left"] = "left",
                ["center"] = "center",
                ["right"] = "right",
                ["justify"] = "justify",
                ["start"] = "start",
                ["end"] = "end"
            };

            if (alignments.TryGetValue(value, out var alignment))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"text-align: {alignment};",
                    Order = CssOrder.Typography
                };
            }

            // Text transform
            var transforms = new Dictionary<string, string>
            {
                ["uppercase"] = "uppercase",
                ["lowercase"] = "lowercase",
                ["capitalize"] = "capitalize",
                ["normal-case"] = "none"
            };

            if (transforms.TryGetValue(value, out var transform))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"text-transform: {transform};",
                    Order = CssOrder.Typography
                };
            }

            // Text decoration
            var decorations = new Dictionary<string, string>
            {
                ["underline"] = "underline",
                ["overline"] = "overline",
                ["line-through"] = "line-through",
                ["no-underline"] = "none"
            };

            if (decorations.TryGetValue(value, out var decoration))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"text-decoration-line: {decoration};",
                    Order = CssOrder.Typography
                };
            }

            // Text wrap
            if (value is "wrap" or "nowrap" or "balance" or "pretty")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"text-wrap: {value};",
                    Order = CssOrder.Typography
                };
            }

            // Text overflow (ellipsis, clip)
            if (value == "ellipsis")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "text-overflow: ellipsis;",
                    Order = CssOrder.Typography
                };
            }

            if (value == "clip")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "text-overflow: clip;",
                    Order = CssOrder.Typography
                };
            }
        }

        // Font weight
        if (name.StartsWith("font-"))
        {
            var value = name[5..];
            if (_config.FontWeights.TryGetValue(value, out var weight))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"font-weight: {weight};",
                    Order = CssOrder.Typography
                };
            }

            // Font style
            if (value == "italic")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "font-style: italic;",
                    Order = CssOrder.Typography
                };
            }

            if (value == "not-italic")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "font-style: normal;",
                    Order = CssOrder.Typography
                };
            }

            // Font family
            if (value == "sans")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "font-family: ui-sans-serif, system-ui, sans-serif, \"Apple Color Emoji\", \"Segoe UI Emoji\", \"Segoe UI Symbol\", \"Noto Color Emoji\";",
                    Order = CssOrder.Typography
                };
            }

            if (value == "serif")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "font-family: ui-serif, Georgia, Cambria, \"Times New Roman\", Times, serif;",
                    Order = CssOrder.Typography
                };
            }

            if (value == "mono")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, \"Liberation Mono\", \"Courier New\", monospace;",
                    Order = CssOrder.Typography
                };
            }
        }

        // Leading (line-height)
        if (name.StartsWith("leading-"))
        {
            var value = name[8..];
            var lineHeights = new Dictionary<string, string>
            {
                ["none"] = "1",
                ["tight"] = "1.25",
                ["snug"] = "1.375",
                ["normal"] = "1.5",
                ["relaxed"] = "1.625",
                ["loose"] = "2"
            };

            if (lineHeights.TryGetValue(value, out var lh))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"line-height: {lh};",
                    Order = CssOrder.Typography
                };
            }

            // Numeric line heights
            if (_config.SpacingScale.TryGetValue(value, out var spacing))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"line-height: {spacing};",
                    Order = CssOrder.Typography
                };
            }
        }

        // Tracking (letter-spacing)
        if (name.StartsWith("tracking-"))
        {
            var value = name[9..];
            var letterSpacings = new Dictionary<string, string>
            {
                ["tighter"] = "-0.05em",
                ["tight"] = "-0.025em",
                ["normal"] = "0em",
                ["wide"] = "0.025em",
                ["wider"] = "0.05em",
                ["widest"] = "0.1em"
            };

            if (letterSpacings.TryGetValue(value, out var ls))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"letter-spacing: {ls};",
                    Order = CssOrder.Typography
                };
            }
        }

        // Truncate
        if (name == "truncate")
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = "overflow: hidden; text-overflow: ellipsis; white-space: nowrap;",
                Order = CssOrder.Typography
            };
        }

        // Whitespace
        if (name.StartsWith("whitespace-"))
        {
            var value = name[11..];
            var whitespaces = new[] { "normal", "nowrap", "pre", "pre-line", "pre-wrap", "break-spaces" };
            if (whitespaces.Contains(value))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"white-space: {value};",
                    Order = CssOrder.Typography
                };
            }
        }

        // Word break
        if (name.StartsWith("break-"))
        {
            var value = name[6..];
            if (value == "normal")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "overflow-wrap: normal; word-break: normal;",
                    Order = CssOrder.Typography
                };
            }
            if (value == "words")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "overflow-wrap: break-word;",
                    Order = CssOrder.Typography
                };
            }
            if (value == "all")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "word-break: break-all;",
                    Order = CssOrder.Typography
                };
            }
            if (value == "keep")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = "word-break: keep-all;",
                    Order = CssOrder.Typography
                };
            }
        }

        return null;
    }

    #endregion

    #region Color Utilities

    private CssRule? TryGenerateColor(string name, string selector)
    {
        // Text color
        if (name.StartsWith("text-"))
        {
            var colorPart = name[5..];
            if (TryParseColor(colorPart, out var color))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"color: {color};",
                    Order = CssOrder.Background
                };
            }
        }

        // Background color
        if (name.StartsWith("bg-"))
        {
            var colorPart = name[3..];
            if (TryParseColor(colorPart, out var color))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"background-color: {color};",
                    Order = CssOrder.Background
                };
            }
        }

        // Border color
        if (name.StartsWith("border-") && !name.StartsWith("border-t-") && !name.StartsWith("border-r-") &&
            !name.StartsWith("border-b-") && !name.StartsWith("border-l-"))
        {
            var colorPart = name[7..];
            if (TryParseColor(colorPart, out var color))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"border-color: {color};",
                    Order = CssOrder.Border
                };
            }
        }

        // Ring color
        if (name.StartsWith("ring-") && !name.StartsWith("ring-offset-"))
        {
            var colorPart = name[5..];
            if (TryParseColor(colorPart, out var color))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"--tw-ring-color: {color};",
                    Order = CssOrder.Border
                };
            }
        }

        // Accent color (for form controls)
        if (name.StartsWith("accent-"))
        {
            var colorPart = name[7..];
            if (TryParseColor(colorPart, out var color))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"accent-color: {color};",
                    Order = CssOrder.Background
                };
            }
        }

        // Caret color
        if (name.StartsWith("caret-"))
        {
            var colorPart = name[6..];
            if (TryParseColor(colorPart, out var color))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"caret-color: {color};",
                    Order = CssOrder.Background
                };
            }
        }

        return null;
    }

    private static bool TryParseColor(string colorSpec, out string value)
    {
        value = string.Empty;

        // Check for opacity modifier (e.g., red-500/50)
        var opacity = "1";
        var colorPart = colorSpec;

        var slashIndex = colorSpec.LastIndexOf('/');
        if (slashIndex > 0)
        {
            colorPart = colorSpec[..slashIndex];
            var opacityStr = colorSpec[(slashIndex + 1)..];

            if (int.TryParse(opacityStr, out var opacityInt))
            {
                opacity = (opacityInt / 100.0).ToString("0.##");
            }
        }

        // Check special colors first
        if (VibeColors.TryGetSpecial(colorPart, out var special))
        {
            value = special;
            return true;
        }

        // Check semantic colors (primary, secondary, etc.) - use CSS variables
        var semanticColors = new[] { "primary", "secondary", "muted", "accent", "destructive", "success", "warning", "info", "background", "foreground", "card", "popover", "border", "input", "ring" };
        if (semanticColors.Contains(colorPart))
        {
            value = $"var(--vibe-{colorPart})";
            return true;
        }

        // Check for foreground variants
        if (colorPart.EndsWith("-foreground"))
        {
            var baseName = colorPart[..^11]; // Remove "-foreground"
            if (semanticColors.Contains(baseName))
            {
                value = $"var(--vibe-{baseName}-foreground)";
                return true;
            }
        }

        // Parse palette color (e.g., "red-500")
        var lastDash = colorPart.LastIndexOf('-');
        if (lastDash > 0)
        {
            var colorName = colorPart[..lastDash];
            var shadeStr = colorPart[(lastDash + 1)..];

            if (int.TryParse(shadeStr, out var shade) && VibeColors.TryGetColor(colorName, shade, out var hex))
            {
                if (opacity != "1")
                {
                    // Convert hex to rgb and apply opacity
                    value = HexToRgba(hex, opacity);
                }
                else
                {
                    value = hex;
                }
                return true;
            }
        }

        return false;
    }

    private static string HexToRgba(string hex, string opacity)
    {
        hex = hex.TrimStart('#');
        var r = Convert.ToInt32(hex[..2], 16);
        var g = Convert.ToInt32(hex[2..4], 16);
        var b = Convert.ToInt32(hex[4..6], 16);
        return $"rgb({r} {g} {b} / {opacity})";
    }

    #endregion

    #region Border Utilities

    private CssRule? TryGenerateBorder(string name, string selector)
    {
        // Border width
        if (name == "border")
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = "border-width: 1px;",
                Order = CssOrder.Border
            };
        }

        if (name == "border-0")
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = "border-width: 0px;",
                Order = CssOrder.Border
            };
        }

        var borderWidths = new Dictionary<string, string>
        {
            ["border-2"] = "2px",
            ["border-4"] = "4px",
            ["border-8"] = "8px"
        };

        if (borderWidths.TryGetValue(name, out var bw))
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = $"border-width: {bw};",
                Order = CssOrder.Border
            };
        }

        // Directional borders
        var borderDirs = new[] { ("border-t", "border-top-width"), ("border-r", "border-right-width"), ("border-b", "border-bottom-width"), ("border-l", "border-left-width") };

        foreach (var (prefix, prop) in borderDirs)
        {
            if (name == prefix)
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"{prop}: 1px;",
                    Order = CssOrder.Border
                };
            }

            if (name == $"{prefix}-0")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"{prop}: 0px;",
                    Order = CssOrder.Border
                };
            }

            if (name == $"{prefix}-2")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"{prop}: 2px;",
                    Order = CssOrder.Border
                };
            }

            if (name == $"{prefix}-4")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"{prop}: 4px;",
                    Order = CssOrder.Border
                };
            }

            if (name == $"{prefix}-8")
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"{prop}: 8px;",
                    Order = CssOrder.Border
                };
            }
        }

        // Border radius
        if (name.StartsWith("rounded"))
        {
            return TryGenerateBorderRadius(name, selector);
        }

        // Border style
        var borderStyles = new Dictionary<string, string>
        {
            ["border-solid"] = "solid",
            ["border-dashed"] = "dashed",
            ["border-dotted"] = "dotted",
            ["border-double"] = "double",
            ["border-hidden"] = "hidden",
            ["border-none"] = "none"
        };

        if (borderStyles.TryGetValue(name, out var bs))
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = $"border-style: {bs};",
                Order = CssOrder.Border
            };
        }

        // Divide utilities (borders between children)
        if (name.StartsWith("divide-"))
        {
            return TryGenerateDivide(name, selector);
        }

        // Ring utilities
        if (name.StartsWith("ring"))
        {
            return TryGenerateRing(name, selector);
        }

        return null;
    }

    private CssRule? TryGenerateBorderRadius(string name, string selector)
    {
        // Simple rounded values
        if (_config.BorderRadius.TryGetValue(name == "rounded" ? "" : name.Replace("rounded-", ""), out var radius))
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = $"border-radius: {radius};",
                Order = CssOrder.Border
            };
        }

        // Directional radius
        var cornerPrefixes = new Dictionary<string, string>
        {
            ["rounded-t-"] = "border-top-left-radius|border-top-right-radius",
            ["rounded-r-"] = "border-top-right-radius|border-bottom-right-radius",
            ["rounded-b-"] = "border-bottom-left-radius|border-bottom-right-radius",
            ["rounded-l-"] = "border-top-left-radius|border-bottom-left-radius",
            ["rounded-tl-"] = "border-top-left-radius",
            ["rounded-tr-"] = "border-top-right-radius",
            ["rounded-bl-"] = "border-bottom-left-radius",
            ["rounded-br-"] = "border-bottom-right-radius"
        };

        foreach (var (prefix, props) in cornerPrefixes)
        {
            if (name.StartsWith(prefix))
            {
                var value = name[prefix.Length..];
                if (_config.BorderRadius.TryGetValue(value, out var r))
                {
                    var declarations = string.Join(" ", props.Split('|').Select(p => $"{p}: {r};"));
                    return new CssRule
                    {
                        Selector = selector,
                        Declarations = declarations,
                        Order = CssOrder.Border
                    };
                }
            }
        }

        return null;
    }

    private CssRule? TryGenerateDivide(string name, string selector)
    {
        // divide-x, divide-y
        if (name == "divide-x")
        {
            return new CssRule
            {
                Selector = $"{selector} > :not([hidden]) ~ :not([hidden])",
                Declarations = "border-right-width: 0px; border-left-width: 1px;",
                Order = CssOrder.Border
            };
        }

        if (name == "divide-y")
        {
            return new CssRule
            {
                Selector = $"{selector} > :not([hidden]) ~ :not([hidden])",
                Declarations = "border-top-width: 1px; border-bottom-width: 0px;",
                Order = CssOrder.Border
            };
        }

        return null;
    }

    private CssRule? TryGenerateRing(string name, string selector)
    {
        // ring (default)
        if (name == "ring")
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = "box-shadow: var(--tw-ring-inset) 0 0 0 calc(3px + var(--tw-ring-offset-width)) var(--tw-ring-color);",
                Order = CssOrder.Effects
            };
        }

        // ring-0 through ring-8
        var ringWidths = new Dictionary<string, string>
        {
            ["ring-0"] = "0px",
            ["ring-1"] = "1px",
            ["ring-2"] = "2px",
            ["ring-4"] = "4px",
            ["ring-8"] = "8px"
        };

        if (ringWidths.TryGetValue(name, out var rw))
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = $"box-shadow: var(--tw-ring-inset) 0 0 0 calc({rw} + var(--tw-ring-offset-width)) var(--tw-ring-color);",
                Order = CssOrder.Effects
            };
        }

        // ring-inset
        if (name == "ring-inset")
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = "--tw-ring-inset: inset;",
                Order = CssOrder.Effects
            };
        }

        return null;
    }

    #endregion

    #region Effects Utilities

    private CssRule? TryGenerateEffects(string name, string selector)
    {
        // Box shadow
        var shadows = new Dictionary<string, string>
        {
            ["shadow-sm"] = "0 1px 2px 0 rgb(0 0 0 / 0.05)",
            ["shadow"] = "0 1px 3px 0 rgb(0 0 0 / 0.1), 0 1px 2px -1px rgb(0 0 0 / 0.1)",
            ["shadow-md"] = "0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1)",
            ["shadow-lg"] = "0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1)",
            ["shadow-xl"] = "0 20px 25px -5px rgb(0 0 0 / 0.1), 0 8px 10px -6px rgb(0 0 0 / 0.1)",
            ["shadow-2xl"] = "0 25px 50px -12px rgb(0 0 0 / 0.25)",
            ["shadow-inner"] = "inset 0 2px 4px 0 rgb(0 0 0 / 0.05)",
            ["shadow-none"] = "0 0 #0000"
        };

        if (shadows.TryGetValue(name, out var shadow))
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = $"box-shadow: {shadow};",
                Order = CssOrder.Effects
            };
        }

        // Opacity
        if (name.StartsWith("opacity-"))
        {
            var value = name[8..];
            if (_config.Opacity.TryGetValue(value, out var op))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"opacity: {op};",
                    Order = CssOrder.Effects
                };
            }
        }

        // Transition
        var transitions = new Dictionary<string, string>
        {
            ["transition-none"] = "transition-property: none;",
            ["transition-all"] = "transition-property: all; transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1); transition-duration: 150ms;",
            ["transition"] = "transition-property: color, background-color, border-color, text-decoration-color, fill, stroke, opacity, box-shadow, transform, filter, backdrop-filter; transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1); transition-duration: 150ms;",
            ["transition-colors"] = "transition-property: color, background-color, border-color, text-decoration-color, fill, stroke; transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1); transition-duration: 150ms;",
            ["transition-opacity"] = "transition-property: opacity; transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1); transition-duration: 150ms;",
            ["transition-shadow"] = "transition-property: box-shadow; transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1); transition-duration: 150ms;",
            ["transition-transform"] = "transition-property: transform; transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1); transition-duration: 150ms;"
        };

        if (transitions.TryGetValue(name, out var transition))
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = transition,
                Order = CssOrder.Effects
            };
        }

        // Duration
        if (name.StartsWith("duration-"))
        {
            var value = name[9..];
            if (int.TryParse(value, out var ms))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"transition-duration: {ms}ms;",
                    Order = CssOrder.Effects
                };
            }
        }

        // Ease
        var easings = new Dictionary<string, string>
        {
            ["ease-linear"] = "linear",
            ["ease-in"] = "cubic-bezier(0.4, 0, 1, 1)",
            ["ease-out"] = "cubic-bezier(0, 0, 0.2, 1)",
            ["ease-in-out"] = "cubic-bezier(0.4, 0, 0.2, 1)"
        };

        if (easings.TryGetValue(name, out var easing))
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = $"transition-timing-function: {easing};",
                Order = CssOrder.Effects
            };
        }

        // Animation
        var animations = new Dictionary<string, string>
        {
            ["animate-none"] = "animation: none;",
            ["animate-spin"] = "animation: spin 1s linear infinite;",
            ["animate-ping"] = "animation: ping 1s cubic-bezier(0, 0, 0.2, 1) infinite;",
            ["animate-pulse"] = "animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;",
            ["animate-bounce"] = "animation: bounce 1s infinite;"
        };

        if (animations.TryGetValue(name, out var animation))
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = animation,
                Order = CssOrder.Effects
            };
        }

        return null;
    }

    #endregion

    #region Layout Utilities

    private CssRule? TryGenerateLayout(string name, string selector)
    {
        // Position
        var positions = new Dictionary<string, string>
        {
            ["static"] = "static",
            ["fixed"] = "fixed",
            ["absolute"] = "absolute",
            ["relative"] = "relative",
            ["sticky"] = "sticky"
        };

        if (positions.TryGetValue(name, out var pos))
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = $"position: {pos};",
                Order = CssOrder.Layout
            };
        }

        // Inset utilities (top, right, bottom, left)
        var insetPrefixes = new Dictionary<string, string>
        {
            ["inset-"] = "inset",
            ["inset-x-"] = "left|right",
            ["inset-y-"] = "top|bottom",
            ["top-"] = "top",
            ["right-"] = "right",
            ["bottom-"] = "bottom",
            ["left-"] = "left"
        };

        foreach (var (prefix, props) in insetPrefixes)
        {
            if (name.StartsWith(prefix))
            {
                var value = name[prefix.Length..];
                if (TryGetInsetValue(value, out var insetValue))
                {
                    var declarations = string.Join(" ", props.Split('|').Select(p => $"{p}: {insetValue};"));
                    return new CssRule
                    {
                        Selector = selector,
                        Declarations = declarations,
                        Order = CssOrder.Layout
                    };
                }
            }
        }

        // Z-index
        if (name.StartsWith("z-"))
        {
            var value = name[2..];
            if (_config.ZIndex.TryGetValue(value, out var z))
            {
                return new CssRule
                {
                    Selector = selector,
                    Declarations = $"z-index: {z};",
                    Order = CssOrder.Layout
                };
            }
        }

        // Overflow
        var overflows = new Dictionary<string, string>
        {
            ["overflow-auto"] = "overflow: auto;",
            ["overflow-hidden"] = "overflow: hidden;",
            ["overflow-clip"] = "overflow: clip;",
            ["overflow-visible"] = "overflow: visible;",
            ["overflow-scroll"] = "overflow: scroll;",
            ["overflow-x-auto"] = "overflow-x: auto;",
            ["overflow-y-auto"] = "overflow-y: auto;",
            ["overflow-x-hidden"] = "overflow-x: hidden;",
            ["overflow-y-hidden"] = "overflow-y: hidden;",
            ["overflow-x-clip"] = "overflow-x: clip;",
            ["overflow-y-clip"] = "overflow-y: clip;",
            ["overflow-x-visible"] = "overflow-x: visible;",
            ["overflow-y-visible"] = "overflow-y: visible;",
            ["overflow-x-scroll"] = "overflow-x: scroll;",
            ["overflow-y-scroll"] = "overflow-y: scroll;"
        };

        if (overflows.TryGetValue(name, out var overflow))
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = overflow,
                Order = CssOrder.Layout
            };
        }

        // Object fit
        var objectFits = new Dictionary<string, string>
        {
            ["object-contain"] = "contain",
            ["object-cover"] = "cover",
            ["object-fill"] = "fill",
            ["object-none"] = "none",
            ["object-scale-down"] = "scale-down"
        };

        if (objectFits.TryGetValue(name, out var fit))
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = $"object-fit: {fit};",
                Order = CssOrder.Layout
            };
        }

        // Visibility
        if (name == "visible")
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = "visibility: visible;",
                Order = CssOrder.Layout
            };
        }

        if (name == "invisible")
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = "visibility: hidden;",
                Order = CssOrder.Layout
            };
        }

        if (name == "collapse")
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = "visibility: collapse;",
                Order = CssOrder.Layout
            };
        }

        return null;
    }

    private bool TryGetInsetValue(string key, out string value)
    {
        value = string.Empty;

        // auto
        if (key == "auto")
        {
            value = "auto";
            return true;
        }

        // full
        if (key == "full")
        {
            value = "100%";
            return true;
        }

        // fractions
        var fractions = new Dictionary<string, string>
        {
            ["1/2"] = "50%",
            ["1/3"] = "33.333333%",
            ["2/3"] = "66.666667%",
            ["1/4"] = "25%",
            ["2/4"] = "50%",
            ["3/4"] = "75%"
        };

        if (fractions.TryGetValue(key, out var frac))
        {
            value = frac;
            return true;
        }

        // spacing scale
        if (_config.SpacingScale.TryGetValue(key, out var spacing))
        {
            value = spacing;
            return true;
        }

        return false;
    }

    #endregion

    #region Interactivity Utilities

    private CssRule? TryGenerateInteractivity(string name, string selector)
    {
        // Cursor
        var cursors = new Dictionary<string, string>
        {
            ["cursor-auto"] = "auto",
            ["cursor-default"] = "default",
            ["cursor-pointer"] = "pointer",
            ["cursor-wait"] = "wait",
            ["cursor-text"] = "text",
            ["cursor-move"] = "move",
            ["cursor-help"] = "help",
            ["cursor-not-allowed"] = "not-allowed",
            ["cursor-none"] = "none",
            ["cursor-context-menu"] = "context-menu",
            ["cursor-progress"] = "progress",
            ["cursor-cell"] = "cell",
            ["cursor-crosshair"] = "crosshair",
            ["cursor-vertical-text"] = "vertical-text",
            ["cursor-alias"] = "alias",
            ["cursor-copy"] = "copy",
            ["cursor-no-drop"] = "no-drop",
            ["cursor-grab"] = "grab",
            ["cursor-grabbing"] = "grabbing",
            ["cursor-all-scroll"] = "all-scroll",
            ["cursor-col-resize"] = "col-resize",
            ["cursor-row-resize"] = "row-resize",
            ["cursor-n-resize"] = "n-resize",
            ["cursor-e-resize"] = "e-resize",
            ["cursor-s-resize"] = "s-resize",
            ["cursor-w-resize"] = "w-resize",
            ["cursor-ne-resize"] = "ne-resize",
            ["cursor-nw-resize"] = "nw-resize",
            ["cursor-se-resize"] = "se-resize",
            ["cursor-sw-resize"] = "sw-resize",
            ["cursor-ew-resize"] = "ew-resize",
            ["cursor-ns-resize"] = "ns-resize",
            ["cursor-nesw-resize"] = "nesw-resize",
            ["cursor-nwse-resize"] = "nwse-resize",
            ["cursor-zoom-in"] = "zoom-in",
            ["cursor-zoom-out"] = "zoom-out"
        };

        if (cursors.TryGetValue(name, out var cursor))
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = $"cursor: {cursor};",
                Order = CssOrder.Interactivity
            };
        }

        // Pointer events
        if (name == "pointer-events-none")
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = "pointer-events: none;",
                Order = CssOrder.Interactivity
            };
        }

        if (name == "pointer-events-auto")
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = "pointer-events: auto;",
                Order = CssOrder.Interactivity
            };
        }

        // User select
        var userSelects = new Dictionary<string, string>
        {
            ["select-none"] = "none",
            ["select-text"] = "text",
            ["select-all"] = "all",
            ["select-auto"] = "auto"
        };

        if (userSelects.TryGetValue(name, out var userSelect))
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = $"user-select: {userSelect};",
                Order = CssOrder.Interactivity
            };
        }

        // Touch action
        var touchActions = new Dictionary<string, string>
        {
            ["touch-auto"] = "auto",
            ["touch-none"] = "none",
            ["touch-pan-x"] = "pan-x",
            ["touch-pan-left"] = "pan-left",
            ["touch-pan-right"] = "pan-right",
            ["touch-pan-y"] = "pan-y",
            ["touch-pan-up"] = "pan-up",
            ["touch-pan-down"] = "pan-down",
            ["touch-pinch-zoom"] = "pinch-zoom",
            ["touch-manipulation"] = "manipulation"
        };

        if (touchActions.TryGetValue(name, out var touchAction))
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = $"touch-action: {touchAction};",
                Order = CssOrder.Interactivity
            };
        }

        // Resize
        var resizes = new Dictionary<string, string>
        {
            ["resize-none"] = "none",
            ["resize-y"] = "vertical",
            ["resize-x"] = "horizontal",
            ["resize"] = "both"
        };

        if (resizes.TryGetValue(name, out var resize))
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = $"resize: {resize};",
                Order = CssOrder.Interactivity
            };
        }

        // Scroll behavior
        if (name == "scroll-auto")
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = "scroll-behavior: auto;",
                Order = CssOrder.Interactivity
            };
        }

        if (name == "scroll-smooth")
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = "scroll-behavior: smooth;",
                Order = CssOrder.Interactivity
            };
        }

        // Screen reader only
        if (name == "sr-only")
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = "position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0, 0, 0, 0); white-space: nowrap; border-width: 0;",
                Order = CssOrder.Interactivity
            };
        }

        if (name == "not-sr-only")
        {
            return new CssRule
            {
                Selector = selector,
                Declarations = "position: static; width: auto; height: auto; padding: 0; margin: 0; overflow: visible; clip: auto; white-space: normal;",
                Order = CssOrder.Interactivity
            };
        }

        return null;
    }

    #endregion

    #region Arbitrary Value Utilities

    private CssRule? TryGenerateArbitrary(string name, string selector)
    {
        // Arbitrary values: property-[value]
        // e.g., w-[500px], bg-[#ff0000], text-[14px]
        var match = ArbitraryValueRegex().Match(name);
        if (!match.Success)
            return null;

        var property = match.Groups[1].Value;
        var value = match.Groups[2].Value;

        // Map property prefixes to CSS properties
        var propertyMap = new Dictionary<string, (string Css, int Order)>
        {
            ["w"] = ("width", CssOrder.Sizing),
            ["h"] = ("height", CssOrder.Sizing),
            ["min-w"] = ("min-width", CssOrder.Sizing),
            ["min-h"] = ("min-height", CssOrder.Sizing),
            ["max-w"] = ("max-width", CssOrder.Sizing),
            ["max-h"] = ("max-height", CssOrder.Sizing),
            ["p"] = ("padding", CssOrder.Spacing),
            ["pt"] = ("padding-top", CssOrder.Spacing),
            ["pr"] = ("padding-right", CssOrder.Spacing),
            ["pb"] = ("padding-bottom", CssOrder.Spacing),
            ["pl"] = ("padding-left", CssOrder.Spacing),
            ["px"] = ("padding-left|padding-right", CssOrder.Spacing),
            ["py"] = ("padding-top|padding-bottom", CssOrder.Spacing),
            ["m"] = ("margin", CssOrder.Spacing),
            ["mt"] = ("margin-top", CssOrder.Spacing),
            ["mr"] = ("margin-right", CssOrder.Spacing),
            ["mb"] = ("margin-bottom", CssOrder.Spacing),
            ["ml"] = ("margin-left", CssOrder.Spacing),
            ["mx"] = ("margin-left|margin-right", CssOrder.Spacing),
            ["my"] = ("margin-top|margin-bottom", CssOrder.Spacing),
            ["gap"] = ("gap", CssOrder.Flexbox),
            ["gap-x"] = ("column-gap", CssOrder.Flexbox),
            ["gap-y"] = ("row-gap", CssOrder.Flexbox),
            ["text"] = ("font-size", CssOrder.Typography),
            ["bg"] = ("background-color", CssOrder.Background),
            ["border"] = ("border-width", CssOrder.Border),
            ["rounded"] = ("border-radius", CssOrder.Border),
            ["top"] = ("top", CssOrder.Layout),
            ["right"] = ("right", CssOrder.Layout),
            ["bottom"] = ("bottom", CssOrder.Layout),
            ["left"] = ("left", CssOrder.Layout),
            ["inset"] = ("inset", CssOrder.Layout),
            ["z"] = ("z-index", CssOrder.Layout),
            ["opacity"] = ("opacity", CssOrder.Effects),
            ["leading"] = ("line-height", CssOrder.Typography),
            ["tracking"] = ("letter-spacing", CssOrder.Typography),
            ["grid-cols"] = ("grid-template-columns", CssOrder.Grid),
            ["grid-rows"] = ("grid-template-rows", CssOrder.Grid),
            ["col-span"] = ("grid-column", CssOrder.Grid),
            ["row-span"] = ("grid-row", CssOrder.Grid)
        };

        if (propertyMap.TryGetValue(property, out var mapping))
        {
            var declarations = mapping.Css.Contains('|')
                ? string.Join(" ", mapping.Css.Split('|').Select(p => $"{p}: {value};"))
                : $"{mapping.Css}: {value};";

            return new CssRule
            {
                Selector = selector,
                Declarations = declarations,
                Order = mapping.Order
            };
        }

        return null;
    }

    [GeneratedRegex(@"^(.+)-\[(.+)\]$")]
    private static partial Regex ArbitraryValueRegex();

    #endregion

    #region Helpers

    private static string EscapeSelector(string selector)
    {
        // Escape special CSS characters in selectors
        return selector
            .Replace(":", "\\:")
            .Replace("/", "\\/")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace(".", "\\.")
            .Replace("#", "\\#")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace(",", "\\,")
            .Replace("%", "\\%");
    }

    #endregion
}
