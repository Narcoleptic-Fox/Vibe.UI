namespace Vibe.UI.Tests.Components.Input;

public class ToggleGroupTests : TestContext
{
    public ToggleGroupTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void ToggleGroup_RendersWithDefaultProps()
    {
        // Arrange & Act
        var cut = RenderComponent<ToggleGroup>(parameters => parameters
            .Add(p => p.ChildContent, builder =>
            {
                builder.OpenComponent<ToggleGroupItem>(0);
                builder.AddAttribute(1, "Value", "option1");
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Option 1")));
                builder.CloseComponent();
            }));

        // Assert
        cut.Find(".vibe-toggle-group").Should().NotBeNull();
        cut.Find(".vibe-toggle-group-item").Should().NotBeNull();
    }

    [Fact]
    public void ToggleGroup_SingleSelection_SelectsOnlyOneItem()
    {
        // Arrange
        string? selectedValue = null;
        var cut = RenderComponent<ToggleGroup>(parameters => parameters
            .Add(p => p.Type, ToggleGroupType.Single)
            .Add(p => p.Value, null)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, (value) => selectedValue = value))
            .Add(p => p.ChildContent, builder =>
            {
                // Option 1
                builder.OpenComponent<ToggleGroupItem>(0);
                builder.AddAttribute(1, "Value", "option1");
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Option 1")));
                builder.CloseComponent();

                // Option 2
                builder.OpenComponent<ToggleGroupItem>(3);
                builder.AddAttribute(4, "Value", "option2");
                builder.AddAttribute(5, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Option 2")));
                builder.CloseComponent();
            }));

        // Act
        var buttons = cut.FindAll("button");
        buttons[0].Click();

        // Assert
        selectedValue.Should().Be("option1");
        buttons[0].ClassList.Should().Contain("vibe-toggle-group-item-pressed");
        buttons[1].ClassList.Should().NotContain("vibe-toggle-group-item-pressed");
    }

    [Fact]
    public void ToggleGroup_HorizontalOrientation_AppliesCorrectClass()
    {
        // Arrange & Act
        var cut = RenderComponent<ToggleGroup>(parameters => parameters
            .Add(p => p.Orientation, ToggleGroupOrientation.Horizontal));

        // Assert
        cut.Find(".vibe-toggle-group").ClassList.Should().Contain("vibe-toggle-group-horizontal");
    }

    [Fact]
    public void ToggleGroup_VerticalOrientation_AppliesCorrectClass()
    {
        // Arrange & Act
        var cut = RenderComponent<ToggleGroup>(parameters => parameters
            .Add(p => p.Orientation, ToggleGroupOrientation.Vertical));

        // Assert
        cut.Find(".vibe-toggle-group").ClassList.Should().Contain("vibe-toggle-group-vertical");
    }

    [Fact]
    public void ToggleGroup_Disabled_DisablesAllItems()
    {
        // Arrange & Act
        var cut = RenderComponent<ToggleGroup>(parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.ChildContent, builder =>
            {
                builder.OpenComponent<ToggleGroupItem>(0);
                builder.AddAttribute(1, "Value", "option1");
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Option 1")));
                builder.CloseComponent();
            }));

        // Assert
        cut.Find(".vibe-toggle-group").ClassList.Should().Contain("vibe-toggle-group-disabled");
        cut.Find("button").HasAttribute("disabled").Should().BeTrue();
    }
}
