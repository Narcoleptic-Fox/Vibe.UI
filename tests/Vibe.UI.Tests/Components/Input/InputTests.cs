namespace Vibe.UI.Tests.Components.Input;

public class InputTests : TestContext
{
    public InputTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void Input_RendersWithDefaultType()
    {
        // Arrange & Act
        var cut = RenderComponent<Input>();

        // Assert
        var input = cut.Find("input");
        input.GetAttribute("type").Should().Be("text");
    }

    [Theory]
    [InlineData("email")]
    [InlineData("password")]
    [InlineData("number")]
    [InlineData("tel")]
    public void Input_AppliesCorrectType(string inputType)
    {
        // Arrange & Act
        var cut = RenderComponent<Input>(parameters => parameters
            .Add(p => p.Type, inputType));

        // Assert
        cut.Find("input").GetAttribute("type").Should().Be(inputType);
    }

    [Fact]
    public void Input_DisplaysPlaceholder()
    {
        // Arrange & Act
        var cut = RenderComponent<Input>(parameters => parameters
            .Add(p => p.Placeholder, "Enter text..."));

        // Assert
        cut.Find("input").GetAttribute("placeholder").Should().Be("Enter text...");
    }

    [Fact]
    public void Input_IsDisabled_WhenDisabledPropIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Input>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        cut.Find("input").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Input_IsReadOnly_WhenReadOnlyPropIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Input>(parameters => parameters
            .Add(p => p.ReadOnly, true));

        // Assert
        cut.Find("input").HasAttribute("readonly").Should().BeTrue();
    }

    [Fact]
    public void Input_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Input>(parameters => parameters
            .Add(p => p.CssClass, "custom-input"));

        // Assert
        cut.Find("input").ClassList.Should().Contain("custom-input");
    }
}
