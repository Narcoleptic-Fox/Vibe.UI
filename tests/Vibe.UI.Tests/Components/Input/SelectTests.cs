namespace Vibe.UI.Tests.Components.Input;

public class SelectTests : TestContext
{
    public SelectTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void Select_RendersWithOptions()
    {
        // Arrange
        var options = new List<SelectOption>
        {
            new() { Value = "1", Label = "Option 1" },
            new() { Value = "2", Label = "Option 2" }
        };

        // Act
        var cut = RenderComponent<Select>(parameters => parameters
            .Add(p => p.Options, options));

        // Assert
        cut.Find("select").Should().NotBeNull();
        var optionElements = cut.FindAll("option");
        optionElements.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void Select_DisplaysPlaceholder()
    {
        // Arrange & Act
        var cut = RenderComponent<Select>(parameters => parameters
            .Add(p => p.Placeholder, "Select an option"));

        // Assert
        cut.Markup.Should().Contain("Select an option");
    }

    [Fact]
    public void Select_IsDisabled_WhenDisabledPropIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Select>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        cut.Find("select").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Select_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Select>(parameters => parameters
            .Add(p => p.CssClass, "custom-select"));

        // Assert
        cut.Markup.Should().Contain("custom-select");
    }
}
