namespace Vibe.UI.Tests.Components.Input;

public class ColorPickerTests : TestContext
{
    public ColorPickerTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void ColorPicker_RendersWithBasicStructure()
    {
        // Arrange & Act
        var cut = RenderComponent<ColorPicker>();

        // Assert
        cut.Find(".vibe-color-picker").Should().NotBeNull();
        cut.Find(".vibe-color-picker-preview").Should().NotBeNull();
        cut.Find(".vibe-color-picker-swatch").Should().NotBeNull();
    }

    [Fact]
    public void ColorPicker_DisplaysDefaultValue()
    {
        // Arrange & Act
        var cut = RenderComponent<ColorPicker>();

        // Assert
        var valueDisplay = cut.Find(".vibe-color-picker-value");
        valueDisplay.TextContent.Should().Be("#000000");
    }

    [Fact]
    public void ColorPicker_DisplaysProvidedValue()
    {
        // Arrange & Act
        var cut = RenderComponent<ColorPicker>(parameters => parameters
            .Add(p => p.Value, "#FF5733"));

        // Assert
        var valueDisplay = cut.Find(".vibe-color-picker-value");
        valueDisplay.TextContent.Should().Be("#FF5733");
    }

    [Fact]
    public void ColorPicker_AppliesColorToSwatch()
    {
        // Arrange & Act
        var cut = RenderComponent<ColorPicker>(parameters => parameters
            .Add(p => p.Value, "#FF5733"));

        // Assert
        var swatch = cut.Find(".vibe-color-picker-swatch");
        swatch.GetAttribute("style").Should().Contain("background-color: #FF5733");
    }

    [Fact]
    public void ColorPicker_OpensPopover_WhenPreviewClicked()
    {
        // Arrange
        var cut = RenderComponent<ColorPicker>();

        // Act
        cut.Find(".vibe-color-picker-preview").Click();

        // Assert
        cut.FindAll(".vibe-color-picker-popover").Should().NotBeEmpty();
        cut.Find(".vibe-color-picker").ClassList.Should().Contain("vibe-color-picker-open");
    }

    [Fact]
    public void ColorPicker_ClosesPopover_WhenPreviewClickedAgain()
    {
        // Arrange
        var cut = RenderComponent<ColorPicker>();
        cut.Find(".vibe-color-picker-preview").Click();

        // Act
        cut.Find(".vibe-color-picker-preview").Click();

        // Assert
        cut.FindAll(".vibe-color-picker-popover").Should().BeEmpty();
    }

    [Fact]
    public void ColorPicker_ShowsHueSlider_InPopover()
    {
        // Arrange
        var cut = RenderComponent<ColorPicker>();

        // Act
        cut.Find(".vibe-color-picker-preview").Click();

        // Assert
        cut.Find(".vibe-color-picker-hue-slider").Should().NotBeNull();
        cut.Find(".vibe-color-picker-hue-slider input[type='range']").Should().NotBeNull();
    }

    [Fact]
    public void ColorPicker_ShowsAlphaSlider_WhenShowAlphaIsTrue()
    {
        // Arrange
        var cut = RenderComponent<ColorPicker>(parameters => parameters
            .Add(p => p.ShowAlpha, true));

        // Act
        cut.Find(".vibe-color-picker-preview").Click();

        // Assert
        cut.Find(".vibe-color-picker-alpha-slider").Should().NotBeNull();
    }

    [Fact]
    public void ColorPicker_HidesAlphaSlider_WhenShowAlphaIsFalse()
    {
        // Arrange
        var cut = RenderComponent<ColorPicker>(parameters => parameters
            .Add(p => p.ShowAlpha, false));

        // Act
        cut.Find(".vibe-color-picker-preview").Click();

        // Assert
        cut.FindAll(".vibe-color-picker-alpha-slider").Should().BeEmpty();
    }

    [Fact]
    public void ColorPicker_ShowsHexInput()
    {
        // Arrange
        var cut = RenderComponent<ColorPicker>();

        // Act
        cut.Find(".vibe-color-picker-preview").Click();

        // Assert
        var hexInput = cut.FindAll("input[type='text']").FirstOrDefault(i => i.ParentElement?.TextContent.Contains("HEX") ?? false);
        hexInput.Should().NotBeNull();
    }

    [Fact]
    public void ColorPicker_ShowsRgbInputs_WhenShowRgbInputsIsTrue()
    {
        // Arrange
        var cut = RenderComponent<ColorPicker>(parameters => parameters
            .Add(p => p.ShowRgbInputs, true));

        // Act
        cut.Find(".vibe-color-picker-preview").Click();

        // Assert
        var rgbInputs = cut.FindAll("input[type='number']");
        rgbInputs.Should().HaveCountGreaterOrEqualTo(3);
    }

    [Fact]
    public void ColorPicker_HidesRgbInputs_WhenShowRgbInputsIsFalse()
    {
        // Arrange
        var cut = RenderComponent<ColorPicker>(parameters => parameters
            .Add(p => p.ShowRgbInputs, false));

        // Act
        cut.Find(".vibe-color-picker-preview").Click();

        // Assert
        var rgbInputs = cut.FindAll("input[type='number']");
        rgbInputs.Should().BeEmpty();
    }

    [Fact]
    public void ColorPicker_ShowsPresets_WhenShowPresetsIsTrue()
    {
        // Arrange
        var cut = RenderComponent<ColorPicker>(parameters => parameters
            .Add(p => p.ShowPresets, true));

        // Act
        cut.Find(".vibe-color-picker-preview").Click();

        // Assert
        var presets = cut.Find(".vibe-color-picker-presets");
        presets.Should().NotBeNull();
        cut.FindAll(".vibe-color-picker-preset").Should().NotBeEmpty();
    }

    [Fact]
    public void ColorPicker_HidesPresets_WhenShowPresetsIsFalse()
    {
        // Arrange
        var cut = RenderComponent<ColorPicker>(parameters => parameters
            .Add(p => p.ShowPresets, false));

        // Act
        cut.Find(".vibe-color-picker-preview").Click();

        // Assert
        cut.FindAll(".vibe-color-picker-presets").Should().BeEmpty();
    }

    [Fact]
    public void ColorPicker_DisplaysCustomPresets()
    {
        // Arrange
        var customPresets = new List<string> { "#FF0000", "#00FF00", "#0000FF" };
        var cut = RenderComponent<ColorPicker>(parameters => parameters
            .Add(p => p.Presets, customPresets)
            .Add(p => p.ShowPresets, true));

        // Act
        cut.Find(".vibe-color-picker-preview").Click();

        // Assert
        var presets = cut.FindAll(".vibe-color-picker-preset");
        presets.Should().HaveCount(3);
    }

    [Fact]
    public void ColorPicker_IsDisabled_WhenDisabledIsTrue()
    {
        // Arrange
        var cut = RenderComponent<ColorPicker>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Act
        cut.Find(".vibe-color-picker-preview").Click();

        // Assert
        cut.FindAll(".vibe-color-picker-popover").Should().BeEmpty();
        cut.Find(".vibe-color-picker").ClassList.Should().Contain("vibe-color-picker-disabled");
    }

    [Fact]
    public void ColorPicker_TriggersValueChanged_WhenPresetSelected()
    {
        // Arrange
        string changedValue = null;
        var cut = RenderComponent<ColorPicker>(parameters => parameters
            .Add(p => p.Value, "#000000")
            .Add(p => p.ShowPresets, true)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string>(this, value => changedValue = value)));

        // Act
        cut.Find(".vibe-color-picker-preview").Click();
        cut.FindAll(".vibe-color-picker-preset")[0].Click();

        // Assert
        changedValue.Should().NotBeNull();
        changedValue.Should().Be("#FF0000");
    }

    [Fact]
    public void ColorPicker_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<ColorPicker>(parameters => parameters
            .Add(p => p.CssClass, "custom-color-picker"));

        // Assert
        cut.Find(".vibe-color-picker").ClassList.Should().Contain("custom-color-picker");
    }

    [Fact]
    public void ColorPicker_ParsesHexColor_Correctly()
    {
        // Arrange & Act
        var cut = RenderComponent<ColorPicker>(parameters => parameters
            .Add(p => p.Value, "#FF5733"));

        // Open popover to trigger color parsing
        cut.Find(".vibe-color-picker-preview").Click();

        // Assert
        var hexInput = cut.FindAll("input[type='text']").First();
        hexInput.GetAttribute("value").Should().Be("#FF5733");
    }
}
