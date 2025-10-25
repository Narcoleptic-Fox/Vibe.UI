namespace Vibe.UI.Tests.Components.Input;

public class TextAreaTests : TestContext
{
    public TextAreaTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void TextArea_RendersWithBasicStructure()
    {
        // Arrange & Act
        var cut = RenderComponent<TextArea>();

        // Assert
        cut.Find("textarea.vibe-textarea").Should().NotBeNull();
    }

    [Fact]
    public void TextArea_AppliesValue()
    {
        // Arrange & Act
        var cut = RenderComponent<TextArea>(parameters => parameters
            .Add(p => p.Value, "Initial text"));

        // Assert
        cut.Find("textarea").TextContent.Should().Be("Initial text");
    }

    [Fact]
    public void TextArea_AppliesPlaceholder()
    {
        // Arrange & Act
        var cut = RenderComponent<TextArea>(parameters => parameters
            .Add(p => p.Placeholder, "Enter your message..."));

        // Assert
        cut.Find("textarea").GetAttribute("placeholder").Should().Be("Enter your message...");
    }

    [Fact]
    public void TextArea_IsDisabled_WhenDisabledIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<TextArea>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        cut.Find("textarea").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void TextArea_IsReadOnly_WhenReadOnlyIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<TextArea>(parameters => parameters
            .Add(p => p.ReadOnly, true));

        // Assert
        cut.Find("textarea").HasAttribute("readonly").Should().BeTrue();
    }

    [Fact]
    public void TextArea_AppliesRowCount()
    {
        // Arrange & Act
        var cut = RenderComponent<TextArea>(parameters => parameters
            .Add(p => p.Rows, 10));

        // Assert
        cut.Find("textarea").GetAttribute("rows").Should().Be("10");
    }

    [Fact]
    public void TextArea_HasDefaultRowCount()
    {
        // Arrange & Act
        var cut = RenderComponent<TextArea>();

        // Assert
        cut.Find("textarea").GetAttribute("rows").Should().Be("4");
    }

    [Fact]
    public void TextArea_TriggersValueChanged_OnInput()
    {
        // Arrange
        string changedValue = null;
        var cut = RenderComponent<TextArea>(parameters => parameters
            .Add(p => p.Value, "")
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string>(this, value => changedValue = value)));

        // Act
        cut.Find("textarea").Input("New text");

        // Assert
        changedValue.Should().Be("New text");
    }

    [Fact]
    public void TextArea_TriggersOnInput_Callback()
    {
        // Arrange
        ChangeEventArgs capturedArgs = null;
        var cut = RenderComponent<TextArea>(parameters => parameters
            .Add(p => p.OnInput, EventCallback.Factory.Create<ChangeEventArgs>(this, args => capturedArgs = args)));

        // Act
        cut.Find("textarea").Input("Test");

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs.Value.Should().Be("Test");
    }

    [Fact]
    public void TextArea_TriggersOnChange_Callback()
    {
        // Arrange
        ChangeEventArgs capturedArgs = null;
        var cut = RenderComponent<TextArea>(parameters => parameters
            .Add(p => p.OnChange, EventCallback.Factory.Create<ChangeEventArgs>(this, args => capturedArgs = args)));

        // Act
        cut.Find("textarea").Change("Changed");

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs.Value.Should().Be("Changed");
    }

    [Fact]
    public void TextArea_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<TextArea>(parameters => parameters
            .Add(p => p.CssClass, "custom-textarea"));

        // Assert
        cut.Find("textarea").ClassList.Should().Contain("custom-textarea");
    }

    [Fact]
    public void TextArea_SupportsAdditionalAttributes()
    {
        // Arrange & Act
        var cut = RenderComponent<TextArea>(parameters => parameters
            .AddUnmatched("data-testid", "my-textarea")
            .AddUnmatched("maxlength", "500"));

        // Assert
        var textarea = cut.Find("textarea");
        textarea.GetAttribute("data-testid").Should().Be("my-textarea");
        textarea.GetAttribute("maxlength").Should().Be("500");
    }

    [Fact]
    public void TextArea_HandlesEmptyValue()
    {
        // Arrange & Act
        var cut = RenderComponent<TextArea>(parameters => parameters
            .Add(p => p.Value, ""));

        // Assert
        cut.Find("textarea").TextContent.Should().BeEmpty();
    }

    [Fact]
    public void TextArea_HandlesNullValue()
    {
        // Arrange & Act
        var cut = RenderComponent<TextArea>(parameters => parameters
            .Add(p => p.Value, null));

        // Assert
        cut.Find("textarea").Should().NotBeNull();
    }
}
