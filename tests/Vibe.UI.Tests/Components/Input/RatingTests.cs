namespace Vibe.UI.Tests.Components.Input;

public class RatingTests : TestContext
{
    public RatingTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void Rating_RendersWithBasicStructure()
    {
        // Arrange & Act
        var cut = RenderComponent<Rating>();

        // Assert
        cut.Find(".vibe-rating").Should().NotBeNull();
        cut.Find("[role='radiogroup']").Should().NotBeNull();
    }

    [Fact]
    public void Rating_Renders5Stars_ByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<Rating>();

        // Assert
        cut.FindAll(".rating-star").Should().HaveCount(5);
    }

    [Fact]
    public void Rating_RendersCustomMaxRating()
    {
        // Arrange & Act
        var cut = RenderComponent<Rating>(parameters => parameters
            .Add(p => p.MaxRating, 10));

        // Assert
        cut.FindAll(".rating-star").Should().HaveCount(10);
    }

    [Fact]
    public void Rating_DisplaysEmptyStars_WhenValueIsZero()
    {
        // Arrange & Act
        var cut = RenderComponent<Rating>(parameters => parameters
            .Add(p => p.Value, 0));

        // Assert
        var stars = cut.FindAll(".star-filled");
        stars.Should().BeEmpty();
    }

    [Fact]
    public void Rating_DisplaysFilledStars_WhenValueIsSet()
    {
        // Arrange & Act
        var cut = RenderComponent<Rating>(parameters => parameters
            .Add(p => p.Value, 3));

        // Assert
        var stars = cut.FindAll(".star-filled");
        stars.Should().HaveCount(3);
    }

    [Fact]
    public void Rating_UpdatesValue_OnStarClick()
    {
        // Arrange
        double changedValue = 0;
        var cut = RenderComponent<Rating>(parameters => parameters
            .Add(p => p.Value, 0)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<double>(this, value => changedValue = value)));

        // Act
        cut.FindAll(".rating-star")[2].Click();

        // Assert
        changedValue.Should().Be(3);
    }

    [Fact]
    public void Rating_DoesNotUpdateValue_WhenReadOnly()
    {
        // Arrange
        double changedValue = 0;
        var cut = RenderComponent<Rating>(parameters => parameters
            .Add(p => p.Value, 0)
            .Add(p => p.ReadOnly, true)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<double>(this, value => changedValue = value)));

        // Act
        cut.FindAll(".rating-star")[2].Click();

        // Assert
        changedValue.Should().Be(0);
        cut.Instance.Value.Should().Be(0);
    }

    [Fact]
    public void Rating_DoesNotUpdateValue_WhenDisabled()
    {
        // Arrange
        double changedValue = 0;
        var cut = RenderComponent<Rating>(parameters => parameters
            .Add(p => p.Value, 0)
            .Add(p => p.Disabled, true)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<double>(this, value => changedValue = value)));

        // Act
        cut.FindAll(".rating-star")[2].Click();

        // Assert
        changedValue.Should().Be(0);
    }

    [Fact]
    public void Rating_AppliesReadOnlyClass_WhenReadOnly()
    {
        // Arrange & Act
        var cut = RenderComponent<Rating>(parameters => parameters
            .Add(p => p.ReadOnly, true));

        // Assert
        cut.Find(".vibe-rating").ClassList.Should().Contain("rating-readonly");
    }

    [Fact]
    public void Rating_AppliesDisabledClass_WhenDisabled()
    {
        // Arrange & Act
        var cut = RenderComponent<Rating>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        cut.Find(".vibe-rating").ClassList.Should().Contain("rating-disabled");
    }

    [Fact]
    public void Rating_DisablesStarButtons_WhenDisabled()
    {
        // Arrange & Act
        var cut = RenderComponent<Rating>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        var buttons = cut.FindAll(".rating-star");
        buttons.Should().AllSatisfy(button => button.HasAttribute("disabled").Should().BeTrue());
    }

    [Fact]
    public void Rating_DisablesStarButtons_WhenReadOnly()
    {
        // Arrange & Act
        var cut = RenderComponent<Rating>(parameters => parameters
            .Add(p => p.ReadOnly, true));

        // Assert
        var buttons = cut.FindAll(".rating-star");
        buttons.Should().AllSatisfy(button => button.HasAttribute("disabled").Should().BeTrue());
    }

    [Fact]
    public void Rating_ShowsValue_WhenShowValueIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Rating>(parameters => parameters
            .Add(p => p.Value, 3.5)
            .Add(p => p.ShowValue, true));

        // Assert
        var valueDisplay = cut.Find(".rating-value");
        valueDisplay.Should().NotBeNull();
        valueDisplay.TextContent.Should().Contain("3.5");
    }

    [Fact]
    public void Rating_HidesValue_WhenShowValueIsFalse()
    {
        // Arrange & Act
        var cut = RenderComponent<Rating>(parameters => parameters
            .Add(p => p.ShowValue, false));

        // Assert
        cut.FindAll(".rating-value").Should().BeEmpty();
    }

    [Theory]
    [InlineData(RatingSize.Small, "rating-small")]
    [InlineData(RatingSize.Default, "rating-default")]
    [InlineData(RatingSize.Large, "rating-large")]
    public void Rating_AppliesCorrectSizeClass(RatingSize size, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<Rating>(parameters => parameters
            .Add(p => p.Size, size));

        // Assert
        cut.Find(".vibe-rating").ClassList.Should().Contain(expectedClass);
    }

    [Fact]
    public void Rating_AppliesAriaLabel()
    {
        // Arrange & Act
        var cut = RenderComponent<Rating>(parameters => parameters
            .Add(p => p.AriaLabel, "Product rating"));

        // Assert
        cut.Find("[role='radiogroup']").GetAttribute("aria-label").Should().Be("Product rating");
    }

    [Fact]
    public void Rating_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Rating>(parameters => parameters
            .Add(p => p.CssClass, "custom-rating"));

        // Assert
        cut.Find(".vibe-rating").ClassList.Should().Contain("custom-rating");
    }

    [Fact]
    public void Rating_SupportsHalfRatings_WhenAllowHalfIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Rating>(parameters => parameters
            .Add(p => p.Value, 2.5)
            .Add(p => p.AllowHalf, true));

        // Assert
        cut.FindAll(".star-half").Should().NotBeEmpty();
    }

    [Fact]
    public void Rating_AppliesAriaLabels_ToStars()
    {
        // Arrange & Act
        var cut = RenderComponent<Rating>();

        // Assert
        var stars = cut.FindAll(".rating-star");
        stars[0].GetAttribute("aria-label").Should().Be("1 star");
        stars[1].GetAttribute("aria-label").Should().Be("2 stars");
        stars[4].GetAttribute("aria-label").Should().Be("5 stars");
    }
}
