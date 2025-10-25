namespace Vibe.UI.Tests.Components.Input;

public class TagInputTests : TestContext
{
    public TagInputTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void TagInput_RendersWithBasicStructure()
    {
        // Arrange & Act
        var cut = RenderComponent<TagInput>();

        // Assert
        cut.Find(".vibe-tag-input").Should().NotBeNull();
        cut.Find(".tag-input-container").Should().NotBeNull();
        cut.Find("input.tag-input").Should().NotBeNull();
    }

    [Fact]
    public void TagInput_DisplaysPlaceholder_WhenNoTags()
    {
        // Arrange & Act
        var cut = RenderComponent<TagInput>(parameters => parameters
            .Add(p => p.Placeholder, "Add tags..."));

        // Assert
        cut.Find("input.tag-input").GetAttribute("placeholder").Should().Be("Add tags...");
    }

    [Fact]
    public void TagInput_DisplaysExistingTags()
    {
        // Arrange
        var tags = new List<string> { "React", "TypeScript", "Blazor" };

        // Act
        var cut = RenderComponent<TagInput>(parameters => parameters
            .Add(p => p.Tags, tags));

        // Assert
        var tagElements = cut.FindAll(".tag-item");
        tagElements.Should().HaveCount(3);
        tagElements[0].TextContent.Should().Contain("React");
        tagElements[1].TextContent.Should().Contain("TypeScript");
        tagElements[2].TextContent.Should().Contain("Blazor");
    }

    [Fact]
    public void TagInput_ShowsRemoveButton_ForEachTag()
    {
        // Arrange
        var tags = new List<string> { "Tag1", "Tag2" };

        // Act
        var cut = RenderComponent<TagInput>(parameters => parameters
            .Add(p => p.Tags, tags));

        // Assert
        cut.FindAll(".tag-remove").Should().HaveCount(2);
    }

    [Fact]
    public void TagInput_HidesRemoveButton_WhenDisabled()
    {
        // Arrange
        var tags = new List<string> { "Tag1" };

        // Act
        var cut = RenderComponent<TagInput>(parameters => parameters
            .Add(p => p.Tags, tags)
            .Add(p => p.Disabled, true));

        // Assert
        cut.FindAll(".tag-remove").Should().BeEmpty();
    }

    [Fact]
    public void TagInput_IsDisabled_WhenDisabledIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<TagInput>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        cut.Find("input.tag-input").HasAttribute("disabled").Should().BeTrue();
        cut.Find(".vibe-tag-input").ClassList.Should().Contain("tag-input-disabled");
    }

    [Fact]
    public void TagInput_ShowsSuggestions_WhenProvided()
    {
        // Arrange
        var suggestions = new List<string> { "React", "Vue", "Angular" };
        var tags = new List<string>();

        // Act
        var cut = RenderComponent<TagInput>(parameters => parameters
            .Add(p => p.Tags, tags)
            .Add(p => p.Suggestions, suggestions));

        // Set instance field to show suggestions (normally triggered by user input)
        cut.Instance.GetType().GetField("_showSuggestions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(cut.Instance, true);
        cut.Render();

        // Assert
        cut.FindAll(".tag-suggestion").Should().HaveCount(3);
    }

    [Fact]
    public void TagInput_FiltersOutExistingTagsFromSuggestions()
    {
        // Arrange
        var suggestions = new List<string> { "React", "Vue", "Angular" };
        var tags = new List<string> { "React" };

        // Act
        var cut = RenderComponent<TagInput>(parameters => parameters
            .Add(p => p.Tags, tags)
            .Add(p => p.Suggestions, suggestions));

        cut.Instance.GetType().GetField("_showSuggestions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(cut.Instance, true);
        cut.Render();

        // Assert - Should only show Vue and Angular
        var suggestionElements = cut.FindAll(".tag-suggestion");
        suggestionElements.Should().HaveCount(2);
    }

    [Fact]
    public void TagInput_AppliesAriaLabel()
    {
        // Arrange & Act
        var cut = RenderComponent<TagInput>(parameters => parameters
            .Add(p => p.AriaLabel, "Technology tags"));

        // Assert
        cut.Find("input.tag-input").GetAttribute("aria-label").Should().Be("Technology tags");
    }

    [Fact]
    public void TagInput_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<TagInput>(parameters => parameters
            .Add(p => p.CssClass, "custom-tag-input"));

        // Assert
        cut.Find(".vibe-tag-input").ClassList.Should().Contain("custom-tag-input");
    }

    [Fact]
    public void TagInput_HidesPlaceholder_WhenTagsExist()
    {
        // Arrange
        var tags = new List<string> { "Tag1" };

        // Act
        var cut = RenderComponent<TagInput>(parameters => parameters
            .Add(p => p.Tags, tags)
            .Add(p => p.Placeholder, "Add tags..."));

        // Assert
        cut.Find("input.tag-input").GetAttribute("placeholder").Should().BeEmpty();
    }
}
