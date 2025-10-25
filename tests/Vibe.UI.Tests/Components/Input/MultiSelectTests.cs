namespace Vibe.UI.Tests.Components.Input;

public class MultiSelectTests : TestContext
{
    public MultiSelectTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void MultiSelect_RendersWithBasicStructure()
    {
        // Arrange & Act
        var cut = RenderComponent<MultiSelect<string>>();

        // Assert
        cut.Find(".vibe-multiselect").Should().NotBeNull();
        cut.Find(".multiselect-container").Should().NotBeNull();
        cut.Find(".multiselect-input").Should().NotBeNull();
    }

    [Fact]
    public void MultiSelect_DisplaysLabel_WhenProvided()
    {
        // Arrange & Act
        var cut = RenderComponent<MultiSelect<string>>(parameters => parameters
            .Add(p => p.Label, "Select Tags"));

        // Assert
        var label = cut.Find(".multiselect-label");
        label.Should().NotBeNull();
        label.TextContent.Should().Be("Select Tags");
    }

    [Fact]
    public void MultiSelect_DisplaysPlaceholder_WhenNoItemsSelected()
    {
        // Arrange & Act
        var cut = RenderComponent<MultiSelect<string>>(parameters => parameters
            .Add(p => p.Placeholder, "Choose options..."));

        // Assert
        cut.Markup.Should().Contain("Choose options...");
    }

    [Fact]
    public void MultiSelect_DisplaysSelectedItems()
    {
        // Arrange
        var selectedItems = new List<string> { "Option 1", "Option 2" };

        // Act
        var cut = RenderComponent<MultiSelect<string>>(parameters => parameters
            .Add(p => p.Items, new[] { "Option 1", "Option 2", "Option 3" })
            .Add(p => p.SelectedItems, selectedItems));

        // Assert
        var tags = cut.FindAll(".multiselect-tag");
        tags.Should().HaveCount(2);
        tags[0].TextContent.Should().Contain("Option 1");
        tags[1].TextContent.Should().Contain("Option 2");
    }

    [Fact]
    public void MultiSelect_OpensDropdown_WhenContainerClicked()
    {
        // Arrange
        var cut = RenderComponent<MultiSelect<string>>(parameters => parameters
            .Add(p => p.Items, new[] { "A", "B", "C" }));

        // Act
        cut.Find(".multiselect-container").Click();

        // Assert
        cut.FindAll(".multiselect-dropdown").Should().NotBeEmpty();
    }

    [Fact]
    public void MultiSelect_DisplaysAllItems_InDropdown()
    {
        // Arrange
        var items = new[] { "Apple", "Banana", "Cherry" };
        var cut = RenderComponent<MultiSelect<string>>(parameters => parameters
            .Add(p => p.Items, items));

        // Act
        cut.Find(".multiselect-container").Click();

        // Assert
        var options = cut.FindAll(".multiselect-option");
        options.Should().HaveCount(3);
        options[0].TextContent.Should().Contain("Apple");
        options[1].TextContent.Should().Contain("Banana");
        options[2].TextContent.Should().Contain("Cherry");
    }

    [Fact]
    public void MultiSelect_SelectsItem_WhenClicked()
    {
        // Arrange
        var selectedItems = new List<string>();
        var cut = RenderComponent<MultiSelect<string>>(parameters => parameters
            .Add(p => p.Items, new[] { "A", "B", "C" })
            .Add(p => p.SelectedItems, selectedItems)
            .Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<List<string>>(this, items => selectedItems = items)));

        // Act
        cut.Find(".multiselect-container").Click();
        cut.FindAll(".multiselect-option")[0].Click();

        // Assert
        selectedItems.Should().Contain("A");
    }

    [Fact]
    public void MultiSelect_DeselectsItem_WhenClickedAgain()
    {
        // Arrange
        var selectedItems = new List<string> { "A" };
        var cut = RenderComponent<MultiSelect<string>>(parameters => parameters
            .Add(p => p.Items, new[] { "A", "B", "C" })
            .Add(p => p.SelectedItems, selectedItems)
            .Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<List<string>>(this, items => selectedItems = items)));

        // Act
        cut.Find(".multiselect-container").Click();
        cut.FindAll(".multiselect-option")[0].Click();

        // Assert
        selectedItems.Should().NotContain("A");
    }

    [Fact]
    public void MultiSelect_RemovesItem_WhenRemoveButtonClicked()
    {
        // Arrange
        var selectedItems = new List<string> { "Option 1" };
        var cut = RenderComponent<MultiSelect<string>>(parameters => parameters
            .Add(p => p.Items, new[] { "Option 1", "Option 2" })
            .Add(p => p.SelectedItems, selectedItems)
            .Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<List<string>>(this, items => selectedItems = items)));

        // Act
        cut.Find(".multiselect-tag-remove").Click();

        // Assert
        selectedItems.Should().BeEmpty();
    }

    [Fact]
    public void MultiSelect_IsDisabled_WhenDisabledIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<MultiSelect<string>>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        cut.Find(".multiselect-input").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void MultiSelect_FiltersItems_WhenSearchTextEntered()
    {
        // Arrange
        var cut = RenderComponent<MultiSelect<string>>(parameters => parameters
            .Add(p => p.Items, new[] { "Apple", "Banana", "Cherry", "Date" })
            .Add(p => p.EnableFiltering, true));

        // Act
        cut.Find(".multiselect-input").Input("an");
        cut.Find(".multiselect-container").Click();

        // Assert
        var options = cut.FindAll(".multiselect-option");
        options.Should().HaveCount(1);
        options[0].TextContent.Should().Contain("Banana");
    }

    [Fact]
    public void MultiSelect_ShowsNoOptionsMessage_WhenNoMatchingItems()
    {
        // Arrange
        var cut = RenderComponent<MultiSelect<string>>(parameters => parameters
            .Add(p => p.Items, new[] { "Apple", "Banana" })
            .Add(p => p.EnableFiltering, true));

        // Act
        cut.Find(".multiselect-input").Input("xyz");
        cut.Find(".multiselect-container").Click();

        // Assert
        cut.Markup.Should().Contain("No options available");
    }

    [Fact]
    public void MultiSelect_DisplaysHelperText_WhenProvided()
    {
        // Arrange & Act
        var cut = RenderComponent<MultiSelect<string>>(parameters => parameters
            .Add(p => p.HelperText, "Select one or more options"));

        // Assert
        var helperText = cut.Find(".multiselect-helper-text");
        helperText.TextContent.Should().Be("Select one or more options");
    }

    [Fact]
    public void MultiSelect_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<MultiSelect<string>>(parameters => parameters
            .Add(p => p.CssClass, "custom-multiselect"));

        // Assert
        cut.Find(".vibe-multiselect").ClassList.Should().Contain("custom-multiselect");
    }

    [Fact]
    public void MultiSelect_ShowsCheckmark_ForSelectedItems()
    {
        // Arrange
        var selectedItems = new List<string> { "A" };
        var cut = RenderComponent<MultiSelect<string>>(parameters => parameters
            .Add(p => p.Items, new[] { "A", "B", "C" })
            .Add(p => p.SelectedItems, selectedItems));

        // Act
        cut.Find(".multiselect-container").Click();

        // Assert
        var selectedOption = cut.FindAll(".multiselect-option")[0];
        selectedOption.ClassList.Should().Contain("selected");
        selectedOption.FindAll("svg").Should().NotBeEmpty();
    }

    [Fact]
    public void MultiSelect_RespectsMaxSelectedItems()
    {
        // Arrange
        var selectedItems = new List<string> { "A", "B" };
        var cut = RenderComponent<MultiSelect<string>>(parameters => parameters
            .Add(p => p.Items, new[] { "A", "B", "C" })
            .Add(p => p.SelectedItems, selectedItems)
            .Add(p => p.MaxSelectedItems, 2)
            .Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<List<string>>(this, items => selectedItems = items)));

        // Act
        cut.Find(".multiselect-container").Click();
        cut.FindAll(".multiselect-option")[2].Click();

        // Assert
        selectedItems.Should().HaveCount(2);
        selectedItems.Should().NotContain("C");
    }
}
