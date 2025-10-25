namespace Vibe.UI.Tests.Components.Utility;

public class IconTests : TestContext
{
    public IconTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void Icon_RendersWithName()
    {
        // Arrange & Act
        var cut = RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, "heart"));

        // Assert
        cut.Find("svg").Should().NotBeNull();
        cut.Find("svg").ClassList.Should().Contain("vibe-icon");
    }

    [Fact]
    public void Icon_AppliesCustomSize()
    {
        // Arrange & Act
        var cut = RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, "star")
            .Add(p => p.Size, 32));

        // Assert
        var svg = cut.Find("svg");
        svg.GetAttribute("width").Should().Be("32");
        svg.GetAttribute("height").Should().Be("32");
    }

    [Fact]
    public void Icon_AppliesCustomColor()
    {
        // Arrange & Act
        var cut = RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, "check")
            .Add(p => p.Color, "#FF0000"));

        // Assert
        var svg = cut.Find("svg");
        svg.GetAttribute("style").Should().Contain("color: #FF0000");
    }

    [Fact]
    public void Icon_AppliesCustomStrokeWidth()
    {
        // Arrange & Act
        var cut = RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, "menu")
            .Add(p => p.StrokeWidth, 3));

        // Assert
        var svg = cut.Find("svg");
        svg.GetAttribute("stroke-width").Should().Be("3");
    }

    [Fact]
    public void Icon_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, "user")
            .Add(p => p.CssClass, "custom-icon"));

        // Assert
        cut.Find("svg").ClassList.Should().Contain("custom-icon");
    }

    [Fact]
    public void Icon_SupportsCustomSvgContent()
    {
        // Arrange
        RenderFragment customContent = builder =>
        {
            builder.OpenElement(0, "circle");
            builder.AddAttribute(1, "cx", "12");
            builder.AddAttribute(2, "cy", "12");
            builder.AddAttribute(3, "r", "10");
            builder.CloseElement();
        };

        // Act
        var cut = RenderComponent<Icon>(parameters => parameters
            .Add(p => p.IconContent, customContent));

        // Assert
        cut.Find("circle").Should().NotBeNull();
    }

    [Theory]
    [InlineData("menu")]
    [InlineData("heart")]
    [InlineData("star")]
    [InlineData("user")]
    [InlineData("search")]
    public void Icon_RendersCommonIcons(string iconName)
    {
        // Arrange & Act
        var cut = RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, iconName));

        // Assert
        cut.Find("svg").Should().NotBeNull();
    }

    [Fact]
    public void Icon_HasCorrectDefaultViewBox()
    {
        // Arrange & Act
        var cut = RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, "check"));

        // Assert
        var svg = cut.Find("svg");
        svg.GetAttribute("viewBox").Should().Be("0 0 24 24");
    }

    [Fact]
    public void Icon_HasCorrectDefaultStroke()
    {
        // Arrange & Act
        var cut = RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, "x"));

        // Assert
        var svg = cut.Find("svg");
        svg.GetAttribute("stroke").Should().Be("currentColor");
        svg.GetAttribute("fill").Should().Be("none");
    }
}
