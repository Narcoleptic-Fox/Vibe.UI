namespace Vibe.UI.Tests.Components.Layout;

public class CardTests : TestContext
{
    public CardTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void Card_RendersBasicStructure()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>();

        // Assert
        cut.Find(".vibe-card").Should().NotBeNull();
    }

    [Fact]
    public void Card_RendersWithChildContent()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .AddChildContent("Card content"));

        // Assert
        cut.Markup.Should().Contain("Card content");
    }

    [Fact]
    public void Card_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.CssClass, "custom-card"));

        // Assert
        cut.Find(".vibe-card").ClassList.Should().Contain("custom-card");
    }
}

public class CardHeaderTests : TestContext
{
    public CardHeaderTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void CardHeader_RendersWithContent()
    {
        // Arrange & Act
        var cut = RenderComponent<CardHeader>(parameters => parameters
            .AddChildContent("Header content"));

        // Assert
        cut.Markup.Should().Contain("Header content");
        cut.Find(".vibe-card-header").Should().NotBeNull();
    }
}

public class CardTitleTests : TestContext
{
    public CardTitleTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void CardTitle_RendersWithContent()
    {
        // Arrange & Act
        var cut = RenderComponent<CardTitle>(parameters => parameters
            .AddChildContent("Title"));

        // Assert
        cut.Markup.Should().Contain("Title");
    }
}

public class CardContentTests : TestContext
{
    public CardContentTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void CardContent_RendersWithContent()
    {
        // Arrange & Act
        var cut = RenderComponent<CardContent>(parameters => parameters
            .AddChildContent("Content"));

        // Assert
        cut.Markup.Should().Contain("Content");
    }
}

public class CardFooterTests : TestContext
{
    public CardFooterTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void CardFooter_RendersWithContent()
    {
        // Arrange & Act
        var cut = RenderComponent<CardFooter>(parameters => parameters
            .AddChildContent("Footer"));

        // Assert
        cut.Markup.Should().Contain("Footer");
    }
}
