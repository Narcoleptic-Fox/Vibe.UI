namespace Vibe.UI.Tests.Components.Input;

public class SliderTests : TestContext
{
    public SliderTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void Slider_RendersWithBasicStructure()
    {
        // Arrange & Act
        var cut = RenderComponent<Slider>();

        // Assert
        cut.Find(".vibe-slider").Should().NotBeNull();
        cut.Find("input[type='range']").Should().NotBeNull();
        cut.Find(".vibe-slider-track").Should().NotBeNull();
        cut.Find(".vibe-slider-range").Should().NotBeNull();
    }

    [Fact]
    public void Slider_AppliesMinMaxValues()
    {
        // Arrange & Act
        var cut = RenderComponent<Slider>(parameters => parameters
            .Add(p => p.Min, 0)
            .Add(p => p.Max, 100));

        // Assert
        var input = cut.Find("input[type='range']");
        input.GetAttribute("min").Should().Be("0");
        input.GetAttribute("max").Should().Be("100");
    }

    [Fact]
    public void Slider_AppliesStepValue()
    {
        // Arrange & Act
        var cut = RenderComponent<Slider>(parameters => parameters
            .Add(p => p.Step, 5));

        // Assert
        var input = cut.Find("input[type='range']");
        input.GetAttribute("step").Should().Be("5");
    }

    [Fact]
    public void Slider_AppliesCurrentValue()
    {
        // Arrange & Act
        var cut = RenderComponent<Slider>(parameters => parameters
            .Add(p => p.Value, 50));

        // Assert
        var input = cut.Find("input[type='range']");
        input.GetAttribute("value").Should().Be("50");
    }

    [Fact]
    public void Slider_ShowsValue_ByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<Slider>(parameters => parameters
            .Add(p => p.Value, 75));

        // Assert
        var valueDisplay = cut.Find(".vibe-slider-value");
        valueDisplay.Should().NotBeNull();
        valueDisplay.TextContent.Should().Be("75");
    }

    [Fact]
    public void Slider_HidesValue_WhenShowValueIsFalse()
    {
        // Arrange & Act
        var cut = RenderComponent<Slider>(parameters => parameters
            .Add(p => p.ShowValue, false));

        // Assert
        cut.FindAll(".vibe-slider-value").Should().BeEmpty();
    }

    [Fact]
    public void Slider_IsDisabled_WhenDisabledIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Slider>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        cut.Find("input[type='range']").HasAttribute("disabled").Should().BeTrue();
        cut.Find(".vibe-slider").ClassList.Should().Contain("vibe-slider-disabled");
    }

    [Fact]
    public void Slider_UpdatesRangeWidth_BasedOnValue()
    {
        // Arrange & Act
        var cut = RenderComponent<Slider>(parameters => parameters
            .Add(p => p.Min, 0)
            .Add(p => p.Max, 100)
            .Add(p => p.Value, 50));

        // Assert
        var range = cut.Find(".vibe-slider-range");
        var style = range.GetAttribute("style");
        style.Should().Contain("width: 50%");
    }

    [Fact]
    public void Slider_TriggersValueChanged_OnChange()
    {
        // Arrange
        double changedValue = 0;
        var cut = RenderComponent<Slider>(parameters => parameters
            .Add(p => p.Value, 25)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<double>(this, value => changedValue = value)));

        // Act
        cut.Find("input[type='range']").Change("75");

        // Assert
        changedValue.Should().Be(75);
    }

    [Fact]
    public void Slider_UpdatesValue_OnChange()
    {
        // Arrange
        var cut = RenderComponent<Slider>(parameters => parameters
            .Add(p => p.Value, 25));

        // Act
        cut.Find("input[type='range']").Change("75");

        // Assert
        cut.Instance.Value.Should().Be(75);
    }

    [Fact]
    public void Slider_ClampsValue_BetweenMinAndMax()
    {
        // Arrange
        var cut = RenderComponent<Slider>(parameters => parameters
            .Add(p => p.Min, 0)
            .Add(p => p.Max, 100)
            .Add(p => p.Value, 25));

        // Act
        cut.Find("input[type='range']").Change("150");

        // Assert
        cut.Instance.Value.Should().Be(100);
    }

    [Fact]
    public void Slider_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Slider>(parameters => parameters
            .Add(p => p.CssClass, "custom-slider"));

        // Assert
        cut.Find(".vibe-slider").ClassList.Should().Contain("custom-slider");
    }

    [Fact]
    public void Slider_HandlesDecimalValues()
    {
        // Arrange & Act
        var cut = RenderComponent<Slider>(parameters => parameters
            .Add(p => p.Min, 0.0)
            .Add(p => p.Max, 1.0)
            .Add(p => p.Step, 0.1)
            .Add(p => p.Value, 0.5));

        // Assert
        var input = cut.Find("input[type='range']");
        input.GetAttribute("value").Should().Be("0.5");
        input.GetAttribute("step").Should().Be("0.1");
    }

    [Fact]
    public void Slider_CalculatesCorrectPercentage_WithCustomRange()
    {
        // Arrange & Act
        var cut = RenderComponent<Slider>(parameters => parameters
            .Add(p => p.Min, 10)
            .Add(p => p.Max, 110)
            .Add(p => p.Value, 60));

        // Assert
        var range = cut.Find(".vibe-slider-range");
        var style = range.GetAttribute("style");
        style.Should().Contain("width: 50%");
    }

    [Fact]
    public void Slider_HandlesEdgeCase_MinEqualsMax()
    {
        // Arrange & Act
        var cut = RenderComponent<Slider>(parameters => parameters
            .Add(p => p.Min, 50)
            .Add(p => p.Max, 50)
            .Add(p => p.Value, 50));

        // Assert
        var range = cut.Find(".vibe-slider-range");
        var style = range.GetAttribute("style");
        style.Should().Contain("width: 0%");
    }
}
