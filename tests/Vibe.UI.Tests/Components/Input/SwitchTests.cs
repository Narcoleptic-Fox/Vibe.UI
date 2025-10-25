namespace Vibe.UI.Tests.Components.Input;

public class SwitchTests : TestContext
{
    public SwitchTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void Switch_RendersWithDefaultState()
    {
        // Arrange & Act
        var cut = RenderComponent<Switch>();

        // Assert
        var input = cut.Find("input[type='checkbox']");
        input.Should().NotBeNull();
        input.GetAttribute("checked").Should().BeNull();
    }

    [Fact]
    public void Switch_IsChecked_WhenCheckedPropIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Switch>(parameters => parameters
            .Add(p => p.Checked, true));

        // Assert
        cut.Find("input").HasAttribute("checked").Should().BeTrue();
    }

    [Fact]
    public void Switch_IsDisabled_WhenDisabledPropIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Switch>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        cut.Find("input").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Switch_TriggersCheckedChanged_OnClick()
    {
        // Arrange
        var checkedValue = false;
        var cut = RenderComponent<Switch>(parameters => parameters
            .Add(p => p.CheckedChanged, EventCallback.Factory.Create<bool>(
                this, value => checkedValue = value)));

        // Act
        cut.Find("input").Change(true);

        // Assert
        checkedValue.Should().BeTrue();
    }

    [Fact]
    public void Switch_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Switch>(parameters => parameters
            .Add(p => p.CssClass, "custom-switch"));

        // Assert
        cut.Markup.Should().Contain("custom-switch");
    }
}
