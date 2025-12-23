using Vibe.UI.CSS.Generator;

namespace Vibe.UI.CSS.Tests.Generator;

public class VibeColorsTests
{
    #region Palette Tests

    [Theory]
    [InlineData("red", 500, "#ef4444")]
    [InlineData("blue", 600, "#2563eb")]
    [InlineData("green", 500, "#22c55e")]
    [InlineData("slate", 100, "#f1f5f9")]
    [InlineData("gray", 900, "#111827")]
    [InlineData("zinc", 800, "#27272a")]
    [InlineData("emerald", 400, "#34d399")]
    [InlineData("purple", 700, "#7e22ce")]
    public void TryGetColor_ValidColorAndShade_ReturnsCorrectHex(string color, int shade, string expectedHex)
    {
        var result = VibeColors.TryGetColor(color, shade, out var hex);

        Assert.True(result);
        Assert.Equal(expectedHex, hex);
    }

    [Fact]
    public void TryGetColor_InvalidColor_ReturnsFalse()
    {
        var result = VibeColors.TryGetColor("notacolor", 500, out var hex);

        Assert.False(result);
        Assert.Empty(hex);
    }

    [Fact]
    public void TryGetColor_InvalidShade_ReturnsFalse()
    {
        var result = VibeColors.TryGetColor("red", 999, out var hex);

        Assert.False(result);
        Assert.Empty(hex);
    }

    [Theory]
    [InlineData("RED", 500)]
    [InlineData("Red", 500)]
    [InlineData("rEd", 500)]
    public void TryGetColor_CaseInsensitive_ReturnsTrue(string color, int shade)
    {
        var result = VibeColors.TryGetColor(color, shade, out _);

        Assert.True(result);
    }

    #endregion

    #region Special Colors Tests

    [Theory]
    [InlineData("black", "#000000")]
    [InlineData("white", "#ffffff")]
    [InlineData("transparent", "transparent")]
    [InlineData("current", "currentColor")]
    [InlineData("inherit", "inherit")]
    public void TryGetSpecial_ValidSpecialColor_ReturnsCorrectValue(string color, string expectedValue)
    {
        var result = VibeColors.TryGetSpecial(color, out var value);

        Assert.True(result);
        Assert.Equal(expectedValue, value);
    }

    [Fact]
    public void TryGetSpecial_InvalidSpecialColor_ReturnsFalse()
    {
        var result = VibeColors.TryGetSpecial("notspecial", out var value);

        Assert.False(result);
    }

    #endregion

    #region IsValidColor Tests

    [Theory]
    [InlineData("red")]
    [InlineData("blue")]
    [InlineData("green")]
    [InlineData("slate")]
    [InlineData("zinc")]
    [InlineData("black")]
    [InlineData("white")]
    [InlineData("transparent")]
    public void IsValidColor_ValidColors_ReturnsTrue(string color)
    {
        Assert.True(VibeColors.IsValidColor(color));
    }

    [Theory]
    [InlineData("notacolor")]
    [InlineData("")]
    [InlineData("primary")] // semantic, not in palette
    public void IsValidColor_InvalidColors_ReturnsFalse(string color)
    {
        Assert.False(VibeColors.IsValidColor(color));
    }

    #endregion

    #region Palette Completeness Tests

    [Theory]
    [InlineData("slate")]
    [InlineData("gray")]
    [InlineData("zinc")]
    [InlineData("neutral")]
    [InlineData("stone")]
    [InlineData("red")]
    [InlineData("orange")]
    [InlineData("amber")]
    [InlineData("yellow")]
    [InlineData("lime")]
    [InlineData("green")]
    [InlineData("emerald")]
    [InlineData("teal")]
    [InlineData("cyan")]
    [InlineData("sky")]
    [InlineData("blue")]
    [InlineData("indigo")]
    [InlineData("violet")]
    [InlineData("purple")]
    [InlineData("fuchsia")]
    [InlineData("pink")]
    [InlineData("rose")]
    public void Palette_ContainsAllTailwindColors(string color)
    {
        Assert.True(VibeColors.Palette.ContainsKey(color));
    }

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(300)]
    [InlineData(400)]
    [InlineData(500)]
    [InlineData(600)]
    [InlineData(700)]
    [InlineData(800)]
    [InlineData(900)]
    [InlineData(950)]
    public void Palette_ContainsAllShades(int shade)
    {
        // Check that red (as a representative) has all shades
        Assert.True(VibeColors.Palette["red"].ContainsKey(shade));
    }

    [Fact]
    public void ValidShades_ContainsExpectedValues()
    {
        var expected = new[] { 50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950 };

        Assert.Equal(expected, VibeColors.ValidShades);
    }

    #endregion
}

