namespace Vibe.UI.Tests.Services;

public class FormValidatorsTests
{
    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("user@domain.co.uk", true)]
    [InlineData("invalid-email", false)]
    [InlineData("@example.com", false)]
    [InlineData("test@", false)]
    [InlineData("", true)] // Email validator allows empty, use Required for that
    public void Email_ValidatesCorrectly(string? email, bool shouldBeValid)
    {
        // Arrange
        var validator = FormValidators.Email();

        // Act
        var result = validator(email);

        // Assert
        if (shouldBeValid)
            result.Should().BeNull();
        else
            result.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("+1 (555) 123-4567", true)]
    [InlineData("555-123-4567", true)]
    [InlineData("5551234567", true)]
    [InlineData("+44 20 1234 5678", true)]
    [InlineData("123", false)]
    [InlineData("abc", false)]
    public void Phone_ValidatesCorrectly(string? phone, bool shouldBeValid)
    {
        // Arrange
        var validator = FormValidators.Phone();

        // Act
        var result = validator(phone);

        // Assert
        if (shouldBeValid)
            result.Should().BeNull();
        else
            result.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("http://example.com", true)]
    [InlineData("https://sub.example.com/path", true)]
    [InlineData("example.com", false)]
    [InlineData("ftp://example.com", false)]
    [InlineData("not-a-url", false)]
    public void Url_ValidatesCorrectly(string? url, bool shouldBeValid)
    {
        // Arrange
        var validator = FormValidators.Url();

        // Act
        var result = validator(url);

        // Assert
        if (shouldBeValid)
            result.Should().BeNull();
        else
            result.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("test", 3, true)]
    [InlineData("test", 5, false)]
    [InlineData("hello world", 5, true)]
    [InlineData("", 1, false)]
    public void MinLength_ValidatesCorrectly(string? value, int minLength, bool shouldBeValid)
    {
        // Arrange
        var validator = FormValidators.MinLength(minLength);

        // Act
        var result = validator(value);

        // Assert
        if (shouldBeValid)
            result.Should().BeNull();
        else
            result.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("test", 10, true)]
    [InlineData("test", 3, false)]
    [InlineData("hello", 5, true)]
    [InlineData("hello", 4, false)]
    public void MaxLength_ValidatesCorrectly(string? value, int maxLength, bool shouldBeValid)
    {
        // Arrange
        var validator = FormValidators.MaxLength(maxLength);

        // Act
        var result = validator(value);

        // Assert
        if (shouldBeValid)
            result.Should().BeNull();
        else
            result.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(5, 1, 10, true)]
    [InlineData(0, 1, 10, false)]
    [InlineData(11, 1, 10, false)]
    [InlineData(1, 1, 10, true)]
    [InlineData(10, 1, 10, true)]
    public void Range_ValidatesCorrectly(int value, int min, int max, bool shouldBeValid)
    {
        // Arrange
        var validator = FormValidators.Range(min, max);

        // Act
        var result = validator(value);

        // Assert
        if (shouldBeValid)
            result.Should().BeNull();
        else
            result.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("Password123!", true)]
    [InlineData("Password123", false)] // No special char
    [InlineData("password123!", false)] // No uppercase
    [InlineData("PASSWORD123!", false)] // No lowercase
    [InlineData("Password!", false)] // No digit
    [InlineData("Pass1!", false)] // Too short
    public void StrongPassword_ValidatesCorrectly(string? password, bool shouldBeValid)
    {
        // Arrange
        var validator = FormValidators.StrongPassword(
            minLength: 8,
            requireUppercase: true,
            requireLowercase: true,
            requireDigit: true,
            requireSpecialChar: true);

        // Act
        var result = validator(password);

        // Assert
        if (shouldBeValid)
            result.Should().BeNull();
        else
            result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void StrongPassword_AllowsCustomRequirements()
    {
        // Arrange
        var validator = FormValidators.StrongPassword(
            minLength: 6,
            requireUppercase: false,
            requireLowercase: false,
            requireDigit: false,
            requireSpecialChar: false);

        // Act
        var result = validator("simple");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("4532015112830366", true)] // Valid Visa
    [InlineData("5425233430109903", true)] // Valid MasterCard
    [InlineData("374245455400126", true)] // Valid Amex
    [InlineData("1234567890123456", false)] // Invalid Luhn
    [InlineData("123", false)] // Too short
    public void CreditCard_ValidatesCorrectly(string? cardNumber, bool shouldBeValid)
    {
        // Arrange
        var validator = FormValidators.CreditCard();

        // Act
        var result = validator(cardNumber);

        // Assert
        if (shouldBeValid)
            result.Should().BeNull();
        else
            result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FutureDate_ValidatesFutureDate()
    {
        // Arrange
        var validator = FormValidators.FutureDate();
        var futureDate = DateTime.Now.AddDays(1);
        var pastDate = DateTime.Now.AddDays(-1);

        // Act
        var futureResult = validator(futureDate);
        var pastResult = validator(pastDate);

        // Assert
        futureResult.Should().BeNull();
        pastResult.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void PastDate_ValidatesPastDate()
    {
        // Arrange
        var validator = FormValidators.PastDate();
        var futureDate = DateTime.Now.AddDays(1);
        var pastDate = DateTime.Now.AddDays(-1);

        // Act
        var futureResult = validator(futureDate);
        var pastResult = validator(pastDate);

        // Assert
        futureResult.Should().NotBeNullOrEmpty();
        pastResult.Should().BeNull();
    }

    [Fact]
    public void Matches_ValidatesMatchingValues()
    {
        // Arrange
        var password = "MyPassword123";
        var validator = FormValidators.Matches<string>(() => password);

        // Act
        var matchResult = validator("MyPassword123");
        var noMatchResult = validator("DifferentPassword");

        // Assert
        matchResult.Should().BeNull();
        noMatchResult.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Pattern_ValidatesRegexPattern()
    {
        // Arrange
        var validator = FormValidators.Pattern(@"^\d{3}-\d{2}-\d{4}$", "Must be in format XXX-XX-XXXX");

        // Act
        var validResult = validator("123-45-6789");
        var invalidResult = validator("12-34-567");

        // Assert
        validResult.Should().BeNull();
        invalidResult.Should().Be("Must be in format XXX-XX-XXXX");
    }

    [Fact]
    public void Required_ValidatesRequiredValues()
    {
        // Arrange
        var validator = FormValidators.Required<string>("Email");

        // Act
        var validResult = validator("test@example.com");
        var nullResult = validator(null);
        var emptyResult = validator("");
        var whitespaceResult = validator("   ");

        // Assert
        validResult.Should().BeNull();
        nullResult.Should().Contain("Email is required");
        emptyResult.Should().Contain("Email is required");
        whitespaceResult.Should().Contain("Email is required");
    }

    [Fact]
    public void Combine_CombinesMultipleValidators()
    {
        // Arrange
        var validator = FormValidators.Combine<string>(
            FormValidators.Required<string>("Email"),
            FormValidators.Email(),
            FormValidators.MinLength(5)
        );

        // Act
        var validResult = validator("test@example.com");
        var emptyResult = validator("");
        var invalidEmailResult = validator("test");
        var tooShortResult = validator("a@b");

        // Assert
        validResult.Should().BeNull();
        emptyResult.Should().NotBeNullOrEmpty();
        invalidEmailResult.Should().NotBeNullOrEmpty();
        tooShortResult.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Combine_ReturnsFirstError()
    {
        // Arrange
        var validator = FormValidators.Combine<string>(
            FormValidators.MinLength(5, "Field"),
            FormValidators.Email()
        );

        // Act
        var result = validator("abc");

        // Assert
        result.Should().Contain("at least 5 characters");
    }
}
