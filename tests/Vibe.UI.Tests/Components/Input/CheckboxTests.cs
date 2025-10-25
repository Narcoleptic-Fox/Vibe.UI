namespace Vibe.UI.Tests.Components.Input;

public class CheckboxTests : TestContext
{
    public CheckboxTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void Checkbox_RendersWithDefaultProps()
    {
        // Arrange & Act
        var cut = RenderComponent<Checkbox>();

        // Assert
        cut.Find("input[type='checkbox']").Should().NotBeNull();
        cut.Find("label").ClassList.Should().Contain("vibe-checkbox");
    }

    [Fact]
    public void Checkbox_IsChecked_WhenCheckedPropIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Checkbox>(parameters => parameters
            .Add(p => p.Checked, true));

        // Assert
        cut.Find("input[type='checkbox']").HasAttribute("checked").Should().BeTrue();
    }

    [Fact]
    public void Checkbox_TriggersCheckedChangedEvent()
    {
        // Arrange
        var checkedValue = false;
        var cut = RenderComponent<Checkbox>(parameters => parameters
            .Add(p => p.Checked, false)
            .Add(p => p.CheckedChanged, EventCallback.Factory.Create<bool>(this, (value) => checkedValue = value)));

        // Act
        cut.Find("input[type='checkbox']").Change(true);

        // Assert
        checkedValue.Should().BeTrue();
    }

    [Fact]
    public void Checkbox_IsDisabled_WhenDisabledPropIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Checkbox>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        cut.Find("input[type='checkbox']").HasAttribute("disabled").Should().BeTrue();
        cut.Find("label").ClassList.Should().Contain("vibe-checkbox-disabled");
    }

    [Fact]
    public void Checkbox_RendersLabel()
    {
        // Arrange & Act
        var cut = RenderComponent<Checkbox>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Accept terms")));

        // Assert
        cut.Find(".vibe-checkbox-label").TextContent.Should().Be("Accept terms");
    }

    [Fact]
    public void Checkbox_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Checkbox>(parameters => parameters
            .Add(p => p.CssClass, "custom-checkbox"));

        // Assert
        cut.Find("label").ClassList.Should().Contain("custom-checkbox");
    }
}
