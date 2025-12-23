namespace Vibe.UI.CSS.Tests;

/// <summary>
/// Integration tests for VibeCss public API.
/// </summary>
public class VibeCssTests
{
    #region GenerateFromContent Tests

    [Fact]
    public void GenerateFromContent_SimpleContent_GeneratesCorrectCss()
    {
        var content = @"<div class=""vibe-flex vibe-gap-4"">Content</div>";

        var css = VibeCss.GenerateFromContent(content);

        Assert.Contains(".vibe-flex", css);
        Assert.Contains("display: flex", css);
        Assert.Contains(".vibe-gap-4", css);
        Assert.Contains("gap:", css);
    }

    [Fact]
    public void GenerateFromContent_WithOptions_AppliesPrefix()
    {
        var content = @"<div class=""tw-flex tw-p-4"">Content</div>";
        var options = new GenerationOptions { Prefix = "tw", IncludeBase = false };

        var css = VibeCss.GenerateFromContent(content, options);

        Assert.Contains(".tw-flex", css);
        Assert.Contains(".tw-p-4", css);
    }

    [Fact]
    public void GenerateFromContent_IncludeBaseFalse_NoBaseStyles()
    {
        var content = @"<div class=""vibe-flex"">Content</div>";
        var options = new GenerationOptions { IncludeBase = false };

        var css = VibeCss.GenerateFromContent(content, options);

        Assert.Contains(".vibe-flex", css);
        // Base CSS typically contains CSS variables, but without base it shouldn't have root rules
        Assert.DoesNotContain(":root {", css);
    }

    [Fact]
    public void GenerateFromContent_IncludeBaseTrue_HasBaseStyles()
    {
        var content = @"<div class=""vibe-flex"">Content</div>";
        var options = new GenerationOptions { IncludeBase = true };

        var css = VibeCss.GenerateFromContent(content, options);

        Assert.Contains(".vibe-flex", css);
        // Base CSS should include CSS variables
        Assert.Contains("--vibe-", css);
    }

    #endregion

    #region Complex Content Tests

    [Fact]
    public void GenerateFromContent_MultipleElements_ExtractsAllClasses()
    {
        var content = @"
            <div class=""vibe-flex vibe-flex-col"">
                <header class=""vibe-p-4 vibe-bg-primary"">Header</header>
                <main class=""vibe-flex-1 vibe-p-6"">Main</main>
                <footer class=""vibe-p-4 vibe-bg-muted"">Footer</footer>
            </div>";

        var css = VibeCss.GenerateFromContent(content, new GenerationOptions { IncludeBase = false });

        Assert.Contains(".vibe-flex", css);
        Assert.Contains(".vibe-flex-col", css);
        Assert.Contains(".vibe-p-4", css);
        Assert.Contains(".vibe-p-6", css);
        Assert.Contains(".vibe-bg-primary", css);
        Assert.Contains(".vibe-bg-muted", css);
        Assert.Contains(".vibe-flex-1", css);
    }

    [Fact]
    public void GenerateFromContent_BlazorComponent_ExtractsAllSyntaxes()
    {
        var content = @"
            <div class=""vibe-flex"">
                <MyComponent Class=""vibe-p-4"" />
                <Button @class=""vibe-bg-primary"">Click</Button>
                <span class=@""vibe-text-sm"">Text</span>
            </div>";

        var css = VibeCss.GenerateFromContent(content, new GenerationOptions { IncludeBase = false });

        Assert.Contains(".vibe-flex", css);
        Assert.Contains(".vibe-p-4", css);
        Assert.Contains(".vibe-bg-primary", css);
        Assert.Contains(".vibe-text-sm", css);
    }

    [Fact]
    public void GenerateFromContent_Variants_GeneratesCorrectSelectors()
    {
        var content = @"<button class=""vibe-bg-primary hover:vibe-bg-secondary focus:vibe-ring-2"">Button</button>";

        var css = VibeCss.GenerateFromContent(content, new GenerationOptions { IncludeBase = false });

        Assert.Contains(".vibe-bg-primary", css);
        Assert.Contains(@"hover\:vibe-bg-secondary:hover", css);
        Assert.Contains(@"focus\:vibe-ring-2:focus", css);
    }

    [Fact]
    public void GenerateFromContent_ResponsiveVariants_GeneratesMediaQueries()
    {
        var content = @"<div class=""vibe-flex sm:vibe-flex-row md:vibe-flex-col lg:vibe-hidden"">Content</div>";

        var css = VibeCss.GenerateFromContent(content, new GenerationOptions { IncludeBase = false });

        Assert.Contains(".vibe-flex", css);
        Assert.Contains("@media (min-width: 640px)", css);  // sm
        Assert.Contains("@media (min-width: 768px)", css);  // md
        Assert.Contains("@media (min-width: 1024px)", css); // lg
    }

    [Fact]
    public void GenerateFromContent_ColorPalette_GeneratesCorrectColors()
    {
        var content = @"
            <div class=""vibe-bg-red-500 vibe-text-blue-600"">
                <span class=""vibe-bg-green-300/50"">Semi-transparent</span>
            </div>";

        var css = VibeCss.GenerateFromContent(content, new GenerationOptions { IncludeBase = false });

        Assert.Contains(".vibe-bg-red-500", css);
        Assert.Contains("#ef4444", css); // red-500
        Assert.Contains(".vibe-text-blue-600", css);
        Assert.Contains("#2563eb", css); // blue-600
        Assert.Contains(@"vibe-bg-green-300\/50", css);
        Assert.Contains("rgb(134 239 172 / 0.5)", css); // green-300 with 50% opacity
    }

    [Fact]
    public void GenerateFromContent_ArbitraryValues_GeneratesCorrectly()
    {
        var content = @"<div class=""vibe-w-[500px] vibe-p-[1.5rem] vibe-mt-[20px]"">Content</div>";

        var css = VibeCss.GenerateFromContent(content, new GenerationOptions { IncludeBase = false });

        Assert.Contains(@"vibe-w-\[500px\]", css);
        Assert.Contains("width: 500px", css);
        Assert.Contains(@"vibe-p-\[1\.5rem\]", css);
        Assert.Contains("padding: 1.5rem", css);
        Assert.Contains(@"vibe-mt-\[20px\]", css);
        Assert.Contains("margin-top: 20px", css);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GenerateFromContent_EmptyContent_ReturnsHeaderOnly()
    {
        var content = "";

        var css = VibeCss.GenerateFromContent(content, new GenerationOptions { IncludeBase = false });

        Assert.Contains("Generated by Vibe.UI.CSS", css);
    }

    [Fact]
    public void GenerateFromContent_NoClasses_ReturnsHeaderOnly()
    {
        var content = @"<div>No CSS classes here</div>";

        var css = VibeCss.GenerateFromContent(content, new GenerationOptions { IncludeBase = false });

        Assert.Contains("Generated by Vibe.UI.CSS", css);
    }

    [Fact]
    public void GenerateFromContent_NonPrefixedClasses_Ignored()
    {
        var content = @"<div class=""flex p-4 non-vibe-class vibe-flex"">Content</div>";

        var css = VibeCss.GenerateFromContent(content, new GenerationOptions { IncludeBase = false });

        // Only vibe-prefixed classes should generate rules
        Assert.Contains(".vibe-flex", css);
        Assert.DoesNotContain(".flex {", css);
        Assert.DoesNotContain(".p-4 {", css);
    }

    #endregion

    #region Full Utility Coverage Tests

    [Theory]
    [InlineData("vibe-flex", "display: flex")]
    [InlineData("vibe-block", "display: block")]
    [InlineData("vibe-hidden", "display: none")]
    [InlineData("vibe-grid", "display: grid")]
    [InlineData("vibe-inline-flex", "display: inline-flex")]
    public void GenerateFromContent_DisplayUtilities_GeneratesCorrectCss(string className, string expectedDeclaration)
    {
        var content = $@"<div class=""{className}"">Content</div>";

        var css = VibeCss.GenerateFromContent(content, new GenerationOptions { IncludeBase = false });

        Assert.Contains($".{className}", css);
        Assert.Contains(expectedDeclaration, css);
    }

    [Theory]
    [InlineData("vibe-m-4", "margin: 1rem")]
    [InlineData("vibe-mt-2", "margin-top: 0.5rem")]
    [InlineData("vibe-mx-auto", "margin-left: auto")]
    [InlineData("vibe-p-8", "padding: 2rem")]
    [InlineData("vibe-py-4", "padding-top: 1rem")]
    public void GenerateFromContent_SpacingUtilities_GeneratesCorrectCss(string className, string expectedDeclaration)
    {
        var content = $@"<div class=""{className}"">Content</div>";

        var css = VibeCss.GenerateFromContent(content, new GenerationOptions { IncludeBase = false });

        Assert.Contains($".{className}", css);
        Assert.Contains(expectedDeclaration, css);
    }

    [Theory]
    [InlineData("vibe-w-full", "width: 100%")]
    [InlineData("vibe-w-1/2", "width: 50%")]
    [InlineData("vibe-h-screen", "height: 100vh")]
    [InlineData("vibe-min-w-0", "min-width: 0")]
    [InlineData("vibe-max-w-xl", "max-width: 36rem")]
    public void GenerateFromContent_SizingUtilities_GeneratesCorrectCss(string className, string expectedDeclaration)
    {
        var content = $@"<div class=""{className}"">Content</div>";

        var css = VibeCss.GenerateFromContent(content, new GenerationOptions { IncludeBase = false });

        Assert.Contains(expectedDeclaration, css);
    }

    [Theory]
    [InlineData("vibe-rounded", "border-radius: 0.25rem")]
    [InlineData("vibe-rounded-lg", "border-radius: 0.5rem")]
    [InlineData("vibe-rounded-full", "border-radius: 9999px")]
    [InlineData("vibe-border", "border-width: 1px")]
    [InlineData("vibe-border-2", "border-width: 2px")]
    public void GenerateFromContent_BorderUtilities_GeneratesCorrectCss(string className, string expectedDeclaration)
    {
        var content = $@"<div class=""{className}"">Content</div>";

        var css = VibeCss.GenerateFromContent(content, new GenerationOptions { IncludeBase = false });

        Assert.Contains(expectedDeclaration, css);
    }

    #endregion

    #region GenerationOptions Tests

    [Fact]
    public void GenerationOptions_DefaultValues_AreCorrect()
    {
        var options = new GenerationOptions();

        Assert.Equal("vibe", options.Prefix);
        Assert.True(options.IncludeBase);
        Assert.Contains("*.razor", options.ScanPatterns);
        Assert.Contains("*.cshtml", options.ScanPatterns);
        Assert.Contains("*.html", options.ScanPatterns);
    }

    #endregion
}

