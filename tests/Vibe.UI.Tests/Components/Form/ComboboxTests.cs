namespace Vibe.UI.Tests.Components.Form;

using Vibe.UI.Components;
using static Vibe.UI.Components.Combobox;

public class ComboboxTests : TestContext
{
    public ComboboxTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void Combobox_RendersWithBasicStructure()
    {
        // Arrange & Act
        var cut = RenderComponent<Combobox>();

        // Assert
        cut.Find(".vibe-combobox").Should().NotBeNull();
        cut.Find(".combobox-trigger").Should().NotBeNull();
        cut.Find("input.combobox-input").Should().NotBeNull();
    }

    [Fact]
    public void Combobox_DisplaysPlaceholder()
    {
        // Arrange & Act
        var cut = RenderComponent<Combobox>(parameters => parameters
            .Add(p => p.Placeholder, "Select an option..."));

        // Assert
        cut.Find("input.combobox-input").GetAttribute("placeholder").Should().Be("Select an option...");
    }

    [Fact]
    public void Combobox_ShowsChevronIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<Combobox>();

        // Assert
        cut.Find(".combobox-chevron").Should().NotBeNull();
        cut.Find(".combobox-chevron svg").Should().NotBeNull();
    }

    [Fact]
    public void Combobox_OpensDropdown_WhenTriggerClicked()
    {
        // Arrange
        var cut = RenderComponent<Combobox>(parameters => parameters
            .Add(p => p.Options, new List<ComboboxOption>
            {
                new() { Label = "Option 1", Value = "1" }
            }));

        // Act
        cut.Find(".combobox-trigger").Click();

        // Assert
        cut.FindAll(".combobox-content").Should().NotBeEmpty();
    }

    [Fact]
    public void Combobox_DisplaysOptions_WhenOpen()
    {
        // Arrange
        var options = new List<ComboboxOption>
        {
            new() { Label = "Apple", Value = "apple" },
            new() { Label = "Banana", Value = "banana" },
            new() { Label = "Cherry", Value = "cherry" }
        };

        var cut = RenderComponent<Combobox>(parameters => parameters
            .Add(p => p.Options, options));

        // Act
        cut.Find(".combobox-trigger").Click();

        // Assert
        var optionElements = cut.FindAll(".combobox-option");
        optionElements.Should().HaveCount(3);
        optionElements[0].TextContent.Should().Contain("Apple");
        optionElements[1].TextContent.Should().Contain("Banana");
        optionElements[2].TextContent.Should().Contain("Cherry");
    }

    [Fact]
    public void Combobox_FiltersOptions_BasedOnInput()
    {
        // Arrange
        var options = new List<ComboboxOption>
        {
            new() { Label = "Apple", Value = "apple" },
            new() { Label = "Apricot", Value = "apricot" },
            new() { Label = "Banana", Value = "banana" }
        };

        var cut = RenderComponent<Combobox>(parameters => parameters
            .Add(p => p.Options, options));

        // Act
        cut.Find("input.combobox-input").Input("Ap");
        cut.Find(".combobox-trigger").Click();

        // Assert
        var filteredOptions = cut.FindAll(".combobox-option");
        filteredOptions.Should().HaveCount(2);
        filteredOptions[0].TextContent.Should().Contain("Apple");
        filteredOptions[1].TextContent.Should().Contain("Apricot");
    }

    [Fact]
    public void Combobox_ShowsEmptyMessage_WhenNoMatchingOptions()
    {
        // Arrange
        var options = new List<ComboboxOption>
        {
            new() { Label = "Apple", Value = "apple" }
        };

        var cut = RenderComponent<Combobox>(parameters => parameters
            .Add(p => p.Options, options));

        // Act
        cut.Find("input.combobox-input").Input("xyz");
        cut.Find(".combobox-trigger").Click();

        // Assert
        cut.Find(".combobox-empty").TextContent.Should().Contain("No results found");
    }

    [Fact]
    public void Combobox_SelectsOption_OnClick()
    {
        // Arrange
        string selectedValue = null;
        var options = new List<ComboboxOption>
        {
            new() { Label = "Apple", Value = "apple" },
            new() { Label = "Banana", Value = "banana" }
        };

        var cut = RenderComponent<Combobox>(parameters => parameters
            .Add(p => p.Options, options)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string>(this, value => selectedValue = value)));

        // Act
        cut.Find(".combobox-trigger").Click();
        cut.FindAll(".combobox-option")[0].Click();

        // Assert
        selectedValue.Should().Be("apple");
    }

    [Fact]
    public void Combobox_ClosesDropdown_AfterSelection()
    {
        // Arrange
        var options = new List<ComboboxOption>
        {
            new() { Label = "Apple", Value = "apple" }
        };

        var cut = RenderComponent<Combobox>(parameters => parameters
            .Add(p => p.Options, options));

        // Act
        cut.Find(".combobox-trigger").Click();
        cut.Find(".combobox-option").Click();

        // Assert
        cut.FindAll(".combobox-content").Should().BeEmpty();
    }

    [Fact]
    public void Combobox_UpdatesInputValue_AfterSelection()
    {
        // Arrange
        var options = new List<ComboboxOption>
        {
            new() { Label = "Apple", Value = "apple" }
        };

        var cut = RenderComponent<Combobox>(parameters => parameters
            .Add(p => p.Options, options));

        // Act
        cut.Find(".combobox-trigger").Click();
        cut.Find(".combobox-option").Click();

        // Assert
        // Input value is bound to InputValue property which gets set to the label
        cut.Markup.Should().Contain("Apple");
    }

    [Fact]
    public void Combobox_ShowsBackdrop_WhenOpen()
    {
        // Arrange
        var cut = RenderComponent<Combobox>(parameters => parameters
            .Add(p => p.Options, new List<ComboboxOption>
            {
                new() { Label = "Option 1", Value = "1" }
            }));

        // Act
        cut.Find(".combobox-trigger").Click();

        // Assert
        cut.Find(".combobox-backdrop").Should().NotBeNull();
    }

    [Fact]
    public void Combobox_ClosesDropdown_WhenBackdropClicked()
    {
        // Arrange
        var cut = RenderComponent<Combobox>(parameters => parameters
            .Add(p => p.Options, new List<ComboboxOption>
            {
                new() { Label = "Option 1", Value = "1" }
            }));

        cut.Find(".combobox-trigger").Click();

        // Act
        cut.Find(".combobox-backdrop").Click();

        // Assert
        cut.FindAll(".combobox-content").Should().BeEmpty();
    }

    [Fact]
    public void Combobox_SkipsDisabledOptions()
    {
        // Arrange
        var options = new List<ComboboxOption>
        {
            new() { Label = "Apple", Value = "apple", Disabled = false },
            new() { Label = "Banana", Value = "banana", Disabled = true }
        };

        var cut = RenderComponent<Combobox>(parameters => parameters
            .Add(p => p.Options, options));

        // Act
        cut.Find(".combobox-trigger").Click();

        // Assert - Should only show enabled option
        var displayedOptions = cut.FindAll(".combobox-option");
        displayedOptions.Should().HaveCount(1);
        displayedOptions[0].TextContent.Should().Contain("Apple");
    }

    [Fact]
    public void Combobox_HighlightsSelectedOption()
    {
        // Arrange
        var options = new List<ComboboxOption>
        {
            new() { Label = "Apple", Value = "apple" },
            new() { Label = "Banana", Value = "banana" }
        };

        var cut = RenderComponent<Combobox>(parameters => parameters
            .Add(p => p.Options, options));

        // Act
        cut.Find(".combobox-trigger").Click();
        cut.FindAll(".combobox-option")[0].MouseEnter();

        // Assert
        cut.FindAll(".combobox-option")[0].ClassList.Should().Contain("selected");
    }
}
