namespace Vibe.UI.Tests.Components.Form;

public class LabelTests : TestContext
{
    public LabelTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void Label_RendersWithDefaultProps()
    {
        // Arrange & Act
        var cut = RenderComponent<Label>(parameters => parameters
            .Add(p => p.ChildContent, "Username"));

        // Assert
        cut.Find("label").Should().NotBeNull();
        cut.Find("label").TextContent.Should().Be("Username");
        cut.Find("label").ClassList.Should().Contain("vibe-label");
    }

    [Fact]
    public void Label_AppliesForAttribute()
    {
        // Arrange & Act
        var cut = RenderComponent<Label>(parameters => parameters
            .Add(p => p.For, "username-input")
            .Add(p => p.ChildContent, "Username"));

        // Assert
        cut.Find("label").GetAttribute("for").Should().Be("username-input");
    }

    [Fact]
    public void Label_ShowsRequiredIndicator_WhenRequiredIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Label>(parameters => parameters
            .Add(p => p.Required, true)
            .Add(p => p.ChildContent, "Email"));

        // Assert
        cut.Find("label").ClassList.Should().Contain("vibe-label-required");
    }

    [Fact]
    public void Label_IsDisabled_WhenDisabledPropIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Label>(parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.ChildContent, "Disabled Label"));

        // Assert
        cut.Find("label").ClassList.Should().Contain("vibe-label-disabled");
    }

    [Fact]
    public void Label_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Label>(parameters => parameters
            .Add(p => p.CssClass, "custom-label")
            .Add(p => p.ChildContent, "Test"));

        // Assert
        cut.Find("label").ClassList.Should().Contain("custom-label");
    }
}
