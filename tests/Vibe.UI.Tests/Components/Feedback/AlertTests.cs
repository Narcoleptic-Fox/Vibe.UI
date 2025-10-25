namespace Vibe.UI.Tests.Components.Feedback;

public class AlertTests : TestContext
{
    public AlertTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void Alert_RendersWithTitle()
    {
        // Arrange & Act
        var cut = RenderComponent<Alert>(parameters => parameters
            .Add(p => p.Title, "Alert Title"));

        // Assert
        var title = cut.Find(".vibe-alert-title");
        title.TextContent.Should().Contain("Alert Title");
    }

    [Fact]
    public void Alert_RendersWithDescription()
    {
        // Arrange & Act
        var cut = RenderComponent<Alert>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Alert description")));

        // Assert
        cut.Markup.Should().Contain("Alert description");
    }

    [Theory]
    [InlineData(AlertType.Default, "vibe-alert-default")]
    [InlineData(AlertType.Info, "vibe-alert-info")]
    [InlineData(AlertType.Success, "vibe-alert-success")]
    [InlineData(AlertType.Warning, "vibe-alert-warning")]
    [InlineData(AlertType.Error, "vibe-alert-error")]
    public void Alert_AppliesCorrectTypeClass(AlertType type, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<Alert>(parameters => parameters
            .Add(p => p.Type, type));

        // Assert
        cut.Find(".vibe-alert").ClassList.Should().Contain(expectedClass);
    }

    [Fact]
    public void Alert_ShowsDismissButton_WhenDismissible()
    {
        // Arrange & Act
        var cut = RenderComponent<Alert>(parameters => parameters
            .Add(p => p.IsDismissible, true));

        // Assert
        cut.FindAll("button").Should().NotBeEmpty();
    }

    [Fact]
    public void Alert_HidesDismissButton_WhenNotDismissible()
    {
        // Arrange & Act
        var cut = RenderComponent<Alert>(parameters => parameters
            .Add(p => p.IsDismissible, false));

        // Assert
        cut.FindAll("button").Should().BeEmpty();
    }

    [Fact]
    public void Alert_TriggersDismiss_OnButtonClick()
    {
        // Arrange
        var dismissed = false;
        var cut = RenderComponent<Alert>(parameters => parameters
            .Add(p => p.IsDismissible, true)
            .Add(p => p.OnDismiss, EventCallback.Factory.Create(this, () => dismissed = true)));

        // Act
        cut.Find("button").Click();

        // Assert
        dismissed.Should().BeTrue();
    }

    [Fact]
    public void Alert_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Alert>(parameters => parameters
            .Add(p => p.CssClass, "custom-alert"));

        // Assert
        cut.Find(".vibe-alert").ClassList.Should().Contain("custom-alert");
    }
}
