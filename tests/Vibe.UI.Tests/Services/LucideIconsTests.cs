namespace Vibe.UI.Tests.Services;

public class LucideIconsTests
{
    [Theory]
    [InlineData("menu")]
    [InlineData("heart")]
    [InlineData("star")]
    [InlineData("user")]
    [InlineData("search")]
    [InlineData("check")]
    [InlineData("x")]
    [InlineData("plus")]
    [InlineData("edit")]
    [InlineData("trash")]
    public void GetIcon_ReturnsIcon_ForCommonIcons(string iconName)
    {
        // Act
        var icon = LucideIcons.GetIcon(iconName);

        // Assert
        icon.Should().NotBeNull();
        icon.Should().NotBeEmpty();
    }

    [Fact]
    public void GetIcon_IsCaseInsensitive()
    {
        // Act
        var lowerCase = LucideIcons.GetIcon("heart");
        var upperCase = LucideIcons.GetIcon("HEART");
        var mixedCase = LucideIcons.GetIcon("HeArT");

        // Assert
        lowerCase.Should().NotBeNull();
        upperCase.Should().NotBeNull();
        mixedCase.Should().NotBeNull();
    }

    [Fact]
    public void GetIcon_ReturnsNull_ForNonExistentIcon()
    {
        // Act
        var icon = LucideIcons.GetIcon("non-existent-icon");

        // Assert
        icon.Should().BeNull();
    }

    [Fact]
    public void IconExists_ReturnsTrueForExistingIcon()
    {
        // Act
        var exists = LucideIcons.IconExists("heart");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public void IconExists_ReturnsFalseForNonExistentIcon()
    {
        // Act
        var exists = LucideIcons.IconExists("non-existent-icon");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public void GetAllIconNames_ReturnsAllIcons()
    {
        // Act
        var iconNames = LucideIcons.GetAllIconNames().ToList();

        // Assert
        iconNames.Should().NotBeEmpty();
        iconNames.Should().Contain("menu");
        iconNames.Should().Contain("heart");
        iconNames.Should().Contain("star");
    }

    [Fact]
    public void GetAllIconNames_ReturnsSortedList()
    {
        // Act
        var iconNames = LucideIcons.GetAllIconNames().ToList();

        // Assert
        var sortedNames = iconNames.OrderBy(n => n).ToList();
        iconNames.Should().Equal(sortedNames);
    }

    [Fact]
    public void GetIcon_ReturnsPathElements()
    {
        // Act
        var icon = LucideIcons.GetIcon("heart");

        // Assert
        icon.Should().NotBeNull();
        icon.Should().HaveCountGreaterThan(0);
        icon![0].Element.Should().NotBeNullOrEmpty();
        icon[0].Attributes.Should().NotBeEmpty();
    }

    [Fact]
    public void GetIcon_HasDAttribute_ForPathElements()
    {
        // Act
        var icon = LucideIcons.GetIcon("check");

        // Assert
        icon.Should().NotBeNull();
        var pathElement = icon!.FirstOrDefault(e => e.Element == "path");
        pathElement.Should().NotBeNull();
        pathElement!.Attributes.Should().ContainKey("d");
    }
}
