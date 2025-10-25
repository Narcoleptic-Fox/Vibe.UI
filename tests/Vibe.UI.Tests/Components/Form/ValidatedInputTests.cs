namespace Vibe.UI.Tests.Components.Form;

public class ValidatedInputTests : TestContext
{
    public ValidatedInputTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void ValidatedInput_RendersWithLabel()
    {
        // Arrange & Act
        var cut = RenderComponent<ValidatedInput<string>>(parameters => parameters
            .Add(p => p.Label, "Email Address"));

        // Assert
        var label = cut.Find("label");
        label.Should().NotBeNull();
        label.TextContent.Should().Contain("Email Address");
    }

    [Fact]
    public void ValidatedInput_ShowsRequiredIndicator_WhenRequired()
    {
        // Arrange & Act
        var cut = RenderComponent<ValidatedInput<string>>(parameters => parameters
            .Add(p => p.Label, "Username")
            .Add(p => p.Required, true));

        // Assert
        cut.Find(".required-indicator").Should().NotBeNull();
    }

    [Fact]
    public void ValidatedInput_ShowsHelperText()
    {
        // Arrange & Act
        var cut = RenderComponent<ValidatedInput<string>>(parameters => parameters
            .Add(p => p.HelperText, "Must be a valid email"));

        // Assert
        var helper = cut.Find(".validated-input-helper");
        helper.TextContent.Should().Be("Must be a valid email");
    }

    [Fact]
    public void ValidatedInput_ShowsErrorMessage_WhenInvalid()
    {
        // Arrange & Act
        var cut = RenderComponent<ValidatedInput<string>>(parameters => parameters
            .Add(p => p.ErrorMessage, "Invalid email format"));

        // Assert
        var error = cut.Find(".validated-input-error");
        error.TextContent.Should().Be("Invalid email format");
    }

    [Fact]
    public void ValidatedInput_AppliesValidClass_WhenValid()
    {
        // Arrange
        var cut = RenderComponent<ValidatedInput<string>>(parameters => parameters
            .Add(p => p.Value, "test@example.com")
            .Add(p => p.Validator, FormValidators.Email()));

        // Act
        cut.Instance.ForceValidation();
        cut.Render();

        // Assert
        cut.Find("input").ClassList.Should().Contain("is-valid");
    }

    [Fact]
    public void ValidatedInput_AppliesInvalidClass_WhenInvalid()
    {
        // Arrange
        var cut = RenderComponent<ValidatedInput<string>>(parameters => parameters
            .Add(p => p.Value, "invalid-email")
            .Add(p => p.Validator, FormValidators.Email()));

        // Act
        cut.Instance.ForceValidation();
        cut.Render();

        // Assert
        cut.Find("input").ClassList.Should().Contain("is-invalid");
    }

    [Fact]
    public void ValidatedInput_IsDisabled_WhenDisabledPropIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<ValidatedInput<string>>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        cut.Find("input").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void ValidatedInput_IsReadOnly_WhenReadOnlyPropIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<ValidatedInput<string>>(parameters => parameters
            .Add(p => p.ReadOnly, true));

        // Assert
        cut.Find("input").HasAttribute("readonly").Should().BeTrue();
    }

    [Fact]
    public void ValidatedInput_ShowsValidationIcon_WhenValid()
    {
        // Arrange
        var cut = RenderComponent<ValidatedInput<string>>(parameters => parameters
            .Add(p => p.Value, "test@example.com")
            .Add(p => p.Validator, FormValidators.Email())
            .Add(p => p.ShowValidationIcon, true));

        // Act
        cut.Instance.ForceValidation();
        cut.Render();

        // Assert
        cut.FindAll(".validation-icon").Should().NotBeEmpty();
    }

    [Fact]
    public void ValidatedInput_TriggersValueChanged_OnInput()
    {
        // Arrange
        string? changedValue = null;
        var cut = RenderComponent<ValidatedInput<string>>(parameters => parameters
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(
                this, value => changedValue = value)));

        // Act
        var input = cut.Find("input");
        input.Input("new value");

        // Assert
        changedValue.Should().Be("new value");
    }

    [Fact]
    public void ValidatedInput_ValidatesOnBlur_WhenConfigured()
    {
        // Arrange
        var cut = RenderComponent<ValidatedInput<string>>(parameters => parameters
            .Add(p => p.ValidateOnBlur, true)
            .Add(p => p.Required, true));

        // Act
        var input = cut.Find("input");
        input.Blur();

        // Assert
        cut.FindAll(".validated-input-error").Should().NotBeEmpty();
    }

    [Fact]
    public void ValidatedInput_AppliesCorrectInputType()
    {
        // Arrange & Act
        var cut = RenderComponent<ValidatedInput<string>>(parameters => parameters
            .Add(p => p.InputType, "password"));

        // Assert
        cut.Find("input").GetAttribute("type").Should().Be("password");
    }

    [Fact]
    public void ValidatedInput_IsValidField_ReturnsTrueWhenValid()
    {
        // Arrange
        var cut = RenderComponent<ValidatedInput<string>>(parameters => parameters
            .Add(p => p.Value, "test@example.com")
            .Add(p => p.Validator, FormValidators.Email()));

        // Act & Assert
        cut.Instance.IsValidField().Should().BeTrue();
    }

    [Fact]
    public void ValidatedInput_IsValidField_ReturnsFalseWhenInvalid()
    {
        // Arrange
        var cut = RenderComponent<ValidatedInput<string>>(parameters => parameters
            .Add(p => p.Value, "invalid")
            .Add(p => p.Validator, FormValidators.Email()));

        // Act & Assert
        cut.Instance.IsValidField().Should().BeFalse();
    }
}
