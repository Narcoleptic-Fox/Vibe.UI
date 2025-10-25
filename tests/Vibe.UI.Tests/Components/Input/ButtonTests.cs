namespace Vibe.UI.Tests.Components.Input;

public class ButtonTests : TestContext
{
    public ButtonTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void Button_RendersWithDefaultProps()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.ChildContent, "Click me"));

        // Assert
        cut.Find("button").Should().NotBeNull();
        cut.Find("button").TextContent.Should().Be("Click me");
        cut.Find("button").ClassList.Should().Contain("vibe-button");
    }

    [Fact]
    public void Button_TriggersOnClickEvent()
    {
        // Arrange
        var clicked = false;
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.ChildContent, "Click me")
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => clicked = true)));

        // Act
        cut.Find("button").Click();

        // Assert
        clicked.Should().BeTrue();
    }

    [Fact]
    public void Button_IsDisabled_WhenDisabledPropIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.ChildContent, "Disabled"));

        // Assert
        cut.Find("button").HasAttribute("disabled").Should().BeTrue();
        cut.Find("button").ClassList.Should().Contain("vibe-button-disabled");
    }

    [Theory]
    [InlineData("Primary", "vibe-button-primary")]
    [InlineData("Secondary", "vibe-button-secondary")]
    [InlineData("Destructive", "vibe-button-destructive")]
    [InlineData("Outline", "vibe-button-outline")]
    [InlineData("Ghost", "vibe-button-ghost")]
    public void Button_AppliesCorrectVariantClass(string variant, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Variant, Enum.Parse<ButtonVariant>(variant))
            .Add(p => p.ChildContent, "Test"));

        // Assert
        cut.Find("button").ClassList.Should().Contain(expectedClass);
    }

    [Theory]
    [InlineData("Small", "vibe-button-sm")]
    [InlineData("Default", "vibe-button-default")]
    [InlineData("Large", "vibe-button-lg")]
    public void Button_AppliesCorrectSizeClass(string size, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Size, Enum.Parse<ButtonSize>(size))
            .Add(p => p.ChildContent, "Test"));

        // Assert
        cut.Find("button").ClassList.Should().Contain(expectedClass);
    }

    [Fact]
    public void Button_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.CssClass, "custom-class")
            .Add(p => p.ChildContent, "Test"));

        // Assert
        cut.Find("button").ClassList.Should().Contain("custom-class");
    }

    [Fact]
    public void Button_ShowsLoadingState()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Loading, true)
            .Add(p => p.ChildContent, "Loading"));

        // Assert
        cut.Find("button").HasAttribute("disabled").Should().BeTrue();
        cut.Find("button").ClassList.Should().Contain("vibe-button-loading");
    }
}
