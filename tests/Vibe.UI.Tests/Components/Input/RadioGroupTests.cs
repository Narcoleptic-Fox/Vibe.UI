namespace Vibe.UI.Tests.Components.Input;

public class RadioGroupTests : TestContext
{
    public RadioGroupTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void RadioGroup_RendersWithBasicStructure()
    {
        // Arrange & Act
        var cut = RenderComponent<RadioGroup>();

        // Assert
        cut.Find(".vibe-radio-group").Should().NotBeNull();
        cut.Find("[role='radiogroup']").Should().NotBeNull();
    }

    [Fact]
    public void RadioGroup_AppliesVerticalOrientation_ByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<RadioGroup>();

        // Assert
        cut.Find(".vibe-radio-group").ClassList.Should().Contain("vibe-radio-group-vertical");
    }

    [Fact]
    public void RadioGroup_AppliesHorizontalOrientation_WhenSet()
    {
        // Arrange & Act
        var cut = RenderComponent<RadioGroup>(parameters => parameters
            .Add(p => p.Orientation, RadioGroupOrientation.Horizontal));

        // Assert
        cut.Find(".vibe-radio-group").ClassList.Should().Contain("vibe-radio-group-horizontal");
    }

    [Fact]
    public void RadioGroup_AppliesDisabledClass_WhenDisabled()
    {
        // Arrange & Act
        var cut = RenderComponent<RadioGroup>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        cut.Find(".vibe-radio-group").ClassList.Should().Contain("vibe-radio-group-disabled");
    }

    [Fact]
    public void RadioGroup_AppliesAriaLabel()
    {
        // Arrange & Act
        var cut = RenderComponent<RadioGroup>(parameters => parameters
            .Add(p => p.AriaLabel, "Choose an option"));

        // Assert
        cut.Find("[role='radiogroup']").GetAttribute("aria-label").Should().Be("Choose an option");
    }

    [Fact]
    public void RadioGroup_AppliesAriaRequired_WhenRequired()
    {
        // Arrange & Act
        var cut = RenderComponent<RadioGroup>(parameters => parameters
            .Add(p => p.Required, true));

        // Assert
        cut.Find("[role='radiogroup']").GetAttribute("aria-required").Should().Be("True");
    }

    [Fact]
    public void RadioGroup_GeneratesUniqueName_ByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<RadioGroup>();

        // Assert
        cut.Instance.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void RadioGroup_AppliesCustomName()
    {
        // Arrange & Act
        var cut = RenderComponent<RadioGroup>(parameters => parameters
            .Add(p => p.Name, "custom-radio-group"));

        // Assert
        cut.Instance.Name.Should().Be("custom-radio-group");
    }

    [Fact]
    public void RadioGroup_RendersChildContent()
    {
        // Arrange & Act
        var cut = RenderComponent<RadioGroup>(parameters => parameters
            .AddChildContent("<div class='test-child'>Radio items</div>"));

        // Assert
        cut.Markup.Should().Contain("test-child");
        cut.Markup.Should().Contain("Radio items");
    }

    [Fact]
    public void RadioGroup_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<RadioGroup>(parameters => parameters
            .Add(p => p.CssClass, "custom-radio-group"));

        // Assert
        cut.Find(".vibe-radio-group").ClassList.Should().Contain("custom-radio-group");
    }

    [Fact]
    public void RadioGroup_SupportsAdditionalAttributes()
    {
        // Arrange & Act
        var cut = RenderComponent<RadioGroup>(parameters => parameters
            .AddUnmatched("data-testid", "my-radio-group"));

        // Assert
        cut.Find(".vibe-radio-group").GetAttribute("data-testid").Should().Be("my-radio-group");
    }

    [Fact]
    public void RadioGroup_InitializesWithValue()
    {
        // Arrange & Act
        var cut = RenderComponent<RadioGroup>(parameters => parameters
            .Add(p => p.Value, "option1"));

        // Assert
        cut.Instance.Value.Should().Be("option1");
    }
}
