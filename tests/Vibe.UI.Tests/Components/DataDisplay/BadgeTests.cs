namespace Vibe.UI.Tests.Components.DataDisplay;

public class BadgeTests : TestContext
{
    public BadgeTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void Badge_RendersWithDefaultProps()
    {
        // Arrange & Act
        var cut = RenderComponent<Badge>(parameters => parameters
            .Add(p => p.ChildContent, "New"));

        // Assert
        cut.Find(".vibe-badge").Should().NotBeNull();
        cut.Find(".vibe-badge").TextContent.Should().Be("New");
    }

    [Fact]
    public void Badge_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Badge>(parameters => parameters
            .Add(p => p.CssClass, "custom-badge")
            .Add(p => p.ChildContent, "Badge"));

        // Assert
        cut.Find(".vibe-badge").ClassList.Should().Contain("custom-badge");
    }

    [Fact]
    public void Badge_RendersHtmlContent()
    {
        // Arrange & Act
        var cut = RenderComponent<Badge>(parameters => parameters
            .Add(p => p.ChildContent, builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddContent(1, "Custom ");
                builder.OpenElement(2, "strong");
                builder.AddContent(3, "Badge");
                builder.CloseElement();
                builder.CloseElement();
            }));

        // Assert
        cut.Find(".vibe-badge strong").TextContent.Should().Be("Badge");
    }
}
