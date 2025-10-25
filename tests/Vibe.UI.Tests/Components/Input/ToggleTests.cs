namespace Vibe.UI.Tests.Components.Input;

public class ToggleTests : TestContext
{
    public ToggleTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void Toggle_RendersWithBasicStructure()
    {
        // Arrange & Act
        var cut = RenderComponent<Toggle>();

        // Assert
        cut.Find("button.vibe-toggle").Should().NotBeNull();
        cut.Find("button").GetAttribute("type").Should().Be("button");
    }

    [Fact]
    public void Toggle_RendersWithContent()
    {
        // Arrange & Act
        var cut = RenderComponent<Toggle>(parameters => parameters
            .AddChildContent("Bold"));

        // Assert
        cut.Find("button").TextContent.Should().Be("Bold");
    }

    [Fact]
    public void Toggle_IsNotPressed_ByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<Toggle>();

        // Assert
        cut.Instance.Pressed.Should().BeFalse();
        cut.Find("button").GetAttribute("aria-pressed").Should().Be("False");
    }

    [Fact]
    public void Toggle_IsPressed_WhenPressedIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Toggle>(parameters => parameters
            .Add(p => p.Pressed, true));

        // Assert
        cut.Instance.Pressed.Should().BeTrue();
        cut.Find("button").GetAttribute("aria-pressed").Should().Be("True");
        cut.Find("button").ClassList.Should().Contain("pressed");
    }

    [Fact]
    public void Toggle_IsDisabled_WhenDisabledIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Toggle>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        cut.Find("button").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Toggle_TogglesState_OnClick()
    {
        // Arrange
        var cut = RenderComponent<Toggle>(parameters => parameters
            .Add(p => p.Pressed, false));

        // Act
        cut.Find("button").Click();

        // Assert
        cut.Instance.Pressed.Should().BeTrue();
    }

    [Fact]
    public void Toggle_TriggersPressedChanged_OnClick()
    {
        // Arrange
        bool changedValue = false;
        var cut = RenderComponent<Toggle>(parameters => parameters
            .Add(p => p.Pressed, false)
            .Add(p => p.PressedChanged, EventCallback.Factory.Create<bool>(this, value => changedValue = value)));

        // Act
        cut.Find("button").Click();

        // Assert
        changedValue.Should().BeTrue();
    }

    [Fact]
    public void Toggle_DoesNotToggle_WhenDisabled()
    {
        // Arrange
        var cut = RenderComponent<Toggle>(parameters => parameters
            .Add(p => p.Pressed, false)
            .Add(p => p.Disabled, true));

        // Act
        cut.Find("button").Click();

        // Assert
        cut.Instance.Pressed.Should().BeFalse();
    }

    [Fact]
    public void Toggle_AppliesVariantClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Toggle>(parameters => parameters
            .Add(p => p.Variant, "outline"));

        // Assert
        cut.Find("button").ClassList.Should().Contain("toggle-outline");
    }

    [Fact]
    public void Toggle_AppliesSizeClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Toggle>(parameters => parameters
            .Add(p => p.Size, "sm"));

        // Assert
        cut.Find("button").ClassList.Should().Contain("toggle-sm");
    }

    [Fact]
    public void Toggle_HasDefaultVariant()
    {
        // Arrange & Act
        var cut = RenderComponent<Toggle>();

        // Assert
        cut.Find("button").ClassList.Should().Contain("toggle-default");
    }

    [Fact]
    public void Toggle_HasDefaultSize()
    {
        // Arrange & Act
        var cut = RenderComponent<Toggle>();

        // Assert
        cut.Find("button").ClassList.Should().Contain("toggle-default");
    }

    [Fact]
    public void Toggle_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Toggle>(parameters => parameters
            .Add(p => p.CssClass, "custom-toggle"));

        // Assert
        cut.Find("button").ClassList.Should().Contain("custom-toggle");
    }

    [Fact]
    public void Toggle_SupportsAdditionalAttributes()
    {
        // Arrange & Act
        var cut = RenderComponent<Toggle>(parameters => parameters
            .AddUnmatched("data-testid", "my-toggle"));

        // Assert
        cut.Find("button").GetAttribute("data-testid").Should().Be("my-toggle");
    }
}
