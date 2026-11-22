using FluentAssertions;
using Vibe.UI.CLI.Services;
using Xunit;

namespace Vibe.UI.CLI.Tests.Services;

/// <summary>
/// What <c>vibe add</c> installs alongside a component. These run against the real component
/// sources. Whether the result actually compiles is proven by
/// scripts/Validate-VendoredComponents.ps1, not here.
/// </summary>
public class ComponentDependencyTests
{
    private readonly ComponentService _componentService = new();

    private string[] Required(string componentName) =>
        _componentService.GetRequiredComponents(componentName).Select(c => c.Name).ToArray();

    [Theory]
    // Rendered in markup.
    [InlineData("select", new[] { "Icon" })]
    [InlineData("datatable", new[] { "Icon", "Pagination" })]
    [InlineData("datepicker", new[] { "Input" })]
    // A child that takes its parent as a cascading parameter.
    [InlineData("accordionitem", new[] { "Accordion" })]
    [InlineData("tabitem", new[] { "Tabs" })]
    // A type nested in another component, plus that component's own needs.
    [InlineData("treeviewnode", new[] { "Icon", "TreeView" })]
    // Reached only through another dependency.
    [InlineData("treeview", new[] { "Icon", "TreeViewNode" })]
    // A companion the parent never names in its own source: declared in the registry.
    [InlineData("radiogroup", new[] { "RadioGroupItem" })]
    [InlineData("button", new string[0])]
    public void GetRequiredComponents_ReturnsTheTransitiveSet(
        string componentName,
        string[] expected
    )
    {
        Required(componentName).Should().Equal(expected);
    }

    [Fact]
    public void GetRequiredComponents_TerminatesAndExcludesTheComponentItself_ForEveryComponent()
    {
        // Parents and children refer to each other, so a missing cycle guard would hang here.
        foreach (var component in _componentService.GetAvailableComponents())
        {
            Required(component.Name)
                .Should()
                .NotContain(component.Name, $"{component.Name} must not depend on itself")
                .And.OnlyHaveUniqueItems();
        }
    }

    [Fact]
    public void GetRequiredComponents_IsCaseInsensitiveOnTheName()
    {
        Required("SELECT").Should().Equal(Required("select"));
    }

    [Fact]
    public void GetRequiredComponents_ThrowsForAnUnknownComponent()
    {
        var act = () => _componentService.GetRequiredComponents("not-a-component");

        act.Should().Throw<InvalidOperationException>().WithMessage("*not-a-component*");
    }
}
