namespace Vibe.UI.Tests.Components.Input;

public class RadioTests : TestContext
{
    public RadioTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void Radio_RendersWithBasicStructure()
    {
        // Arrange & Act
        var cut = RenderComponent<Radio>();

        // Assert
        cut.Find("label.vibe-radio").Should().NotBeNull();
        cut.Find("input[type='radio']").Should().NotBeNull();
        cut.Find(".vibe-radio-control").Should().NotBeNull();
    }

    [Fact]
    public void Radio_RendersWithLabel()
    {
        // Arrange & Act
        var cut = RenderComponent<Radio>(parameters => parameters
            .AddChildContent("Option 1"));

        // Assert
        var label = cut.Find(".vibe-radio-label");
        label.TextContent.Should().Be("Option 1");
    }

    [Fact]
    public void Radio_AppliesNameAttribute()
    {
        // Arrange & Act
        var cut = RenderComponent<Radio>(parameters => parameters
            .Add(p => p.Name, "radio-group")
            .Add(p => p.Value, "option1"));

        // Assert
        var input = cut.Find("input[type='radio']");
        input.GetAttribute("name").Should().Be("radio-group");
    }

    [Fact]
    public void Radio_AppliesValueAttribute()
    {
        // Arrange & Act
        var cut = RenderComponent<Radio>(parameters => parameters
            .Add(p => p.Name, "group")
            .Add(p => p.Value, "option1"));

        // Assert
        var input = cut.Find("input[type='radio']");
        input.GetAttribute("value").Should().Be("option1");
    }

    [Fact]
    public void Radio_IsChecked_WhenCheckedIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Radio>(parameters => parameters
            .Add(p => p.Checked, true)
            .Add(p => p.Value, "option1"));

        // Assert
        var input = cut.Find("input[type='radio']");
        input.HasAttribute("checked").Should().BeTrue();
    }

    [Fact]
    public void Radio_IsNotChecked_WhenCheckedIsFalse()
    {
        // Arrange & Act
        var cut = RenderComponent<Radio>(parameters => parameters
            .Add(p => p.Checked, false)
            .Add(p => p.Value, "option1"));

        // Assert
        var input = cut.Find("input[type='radio']");
        input.HasAttribute("checked").Should().BeFalse();
    }

    [Fact]
    public void Radio_IsDisabled_WhenDisabledIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Radio>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        cut.Find("input[type='radio']").HasAttribute("disabled").Should().BeTrue();
        cut.Find("label").ClassList.Should().Contain("vibe-radio-disabled");
    }

    [Fact]
    public void Radio_TriggersCheckedChanged_OnChange()
    {
        // Arrange
        var checkedValue = false;
        var cut = RenderComponent<Radio>(parameters => parameters
            .Add(p => p.Checked, false)
            .Add(p => p.Value, "option1")
            .Add(p => p.CheckedChanged, EventCallback.Factory.Create<bool>(this, value => checkedValue = value)));

        // Act
        cut.Find("input[type='radio']").Change("option1");

        // Assert
        checkedValue.Should().BeTrue();
    }

    [Fact]
    public void Radio_UpdatesCheckedState_OnChange()
    {
        // Arrange
        var cut = RenderComponent<Radio>(parameters => parameters
            .Add(p => p.Checked, false)
            .Add(p => p.Value, "option1"));

        // Act
        cut.Find("input[type='radio']").Change("option1");

        // Assert
        cut.Instance.Checked.Should().BeTrue();
    }

    [Fact]
    public void Radio_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Radio>(parameters => parameters
            .Add(p => p.CssClass, "custom-radio"));

        // Assert
        cut.Find("label").ClassList.Should().Contain("custom-radio");
    }

    [Fact]
    public void Radio_RendersWithoutLabel()
    {
        // Arrange & Act
        var cut = RenderComponent<Radio>();

        // Assert
        cut.FindAll(".vibe-radio-label").Should().BeEmpty();
    }

    [Fact]
    public void Radio_SupportsAdditionalAttributes()
    {
        // Arrange & Act
        var cut = RenderComponent<Radio>(parameters => parameters
            .AddUnmatched("data-test-id", "my-radio"));

        // Assert
        cut.Find("label").GetAttribute("data-test-id").Should().Be("my-radio");
    }
}
