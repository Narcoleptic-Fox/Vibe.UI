namespace Vibe.UI.Tests.Components.Input;

public class InputOTPTests : TestContext
{
    public InputOTPTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void InputOTP_RendersCorrectNumberOfInputs()
    {
        // Arrange & Act
        var cut = RenderComponent<InputOTP>(parameters => parameters
            .Add(p => p.Length, 6));

        // Assert
        var inputs = cut.FindAll("input");
        inputs.Should().HaveCount(6);
    }

    [Theory]
    [InlineData(InputOTPPattern.Numeric, "0123456789")]
    [InlineData(InputOTPPattern.Alphanumeric, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789")]
    [InlineData(InputOTPPattern.Alpha, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz")]
    public void InputOTP_AppliesCorrectPattern(InputOTPPattern pattern, string allowedChars)
    {
        // Arrange & Act
        var cut = RenderComponent<InputOTP>(parameters => parameters
            .Add(p => p.Pattern, pattern)
            .Add(p => p.Length, 4));

        // Assert
        cut.Find("input").Should().NotBeNull();
    }

    [Fact]
    public void InputOTP_ShowsSeparator_WhenConfigured()
    {
        // Arrange & Act
        var cut = RenderComponent<InputOTP>(parameters => parameters
            .Add(p => p.Length, 6)
            .Add(p => p.SeparatorIndex, 3));

        // Assert
        cut.FindAll(".otp-separator").Should().NotBeEmpty();
    }

    [Fact]
    public void InputOTP_IsDisabled_WhenDisabledPropIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<InputOTP>(parameters => parameters
            .Add(p => p.Length, 4)
            .Add(p => p.Disabled, true));

        // Assert
        var inputs = cut.FindAll("input");
        inputs.Should().OnlyContain(input => input.HasAttribute("disabled"));
    }

    [Fact]
    public void InputOTP_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<InputOTP>(parameters => parameters
            .Add(p => p.Length, 4)
            .Add(p => p.CssClass, "custom-otp"));

        // Assert
        cut.Find(".vibe-inputotp").ClassList.Should().Contain("custom-otp");
    }
}
