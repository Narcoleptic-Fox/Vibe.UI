using FluentAssertions;
using Vibe.UI.CLI.Services;
using Xunit;

namespace Vibe.UI.CLI.Tests.Services;

public class ComponentReferenceScannerTests
{
    private static readonly string[] Known =
    [
        "Accordion",
        "Icon",
        "Input",
        "Label",
        "Pagination",
        "Tabs",
        "Toast",
        "TreeView",
    ];

    private static IReadOnlyList<string> Scan(string source) =>
        ComponentReferenceScanner.FindReferencedComponents(source, Known);

    [Theory]
    [InlineData("<Icon Name=\"check\" />")]
    [InlineData("<Icon/>")]
    [InlineData("<Icon>")]
    [InlineData("<div>\n    <Icon\n        Name=\"check\" />\n</div>")]
    public void MarkupTag_IsAReference(string source)
    {
        Scan(source).Should().Equal("Icon");
    }

    [Theory]
    [InlineData("[CascadingParameter]\n    private Accordion? Parent { get; set; }")]
    [InlineData("private Accordion? _registeredParent;")]
    [InlineData("private Accordion _parent = default!;")]
    [InlineData("void Register(Accordion parent) { }")]
    public void Declaration_IsAReference(string source)
    {
        Scan(source).Should().Equal("Accordion");
    }

    [Theory]
    [InlineData("public TreeView.TreeNode? Item { get; set; }")]
    [InlineData("public EventCallback<TreeView.TreeNode> OnNodeClick { get; set; }")]
    [InlineData("private IReadOnlyList<TreeView.TreeNode> Items => Empty;")]
    [InlineData("var empty = Array.Empty<TreeView.TreeNode>();")]
    [InlineData("private Dictionary<string, TreeView?> _byKey = new();")]
    public void NestedTypeAndGenericArgument_AreReferences(string source)
    {
        Scan(source).Should().Equal("TreeView");
    }

    [Theory]
    [InlineData("var toast = new Toast();")]
    [InlineData("var type = typeof(Toast);")]
    [InlineData("if (component is Toast) { }")]
    [InlineData("var toast = component as Toast;")]
    public void TypeExpression_IsAReference(string source)
    {
        Scan(source).Should().Equal("Toast");
    }

    [Theory]
    // A parameter that shares a component's name is the commonest trap: these are not references.
    [InlineData("[Parameter]\n    public string? Icon { get; set; }")]
    [InlineData("public string Label { get; set; } = string.Empty;")]
    [InlineData(
        "private string? Normalized => string.IsNullOrWhiteSpace(Label) ? null : Label.Trim();"
    )]
    [InlineData("<span class=\"command-item-label\">@item.Label</span>")]
    [InlineData("Items.Where(i => i.Label.Contains(value))")]
    [InlineData("<input type=\"text\" />")]
    [InlineData("<IconButton />")]
    public void MemberOrLookalike_IsNotAReference(string source)
    {
        Scan(source).Should().BeEmpty();
    }

    [Theory]
    [InlineData("/// The content of the group (<Icon /> components).")]
    [InlineData("// private Accordion? Parent { get; set; }")]
    [InlineData("/* <Icon /> */")]
    [InlineData("@* <Icon /> *@")]
    [InlineData("<!-- <Icon /> -->")]
    public void Comment_IsNotAReference(string source)
    {
        Scan(source).Should().BeEmpty();
    }

    [Fact]
    public void UnknownPascalCaseTag_IsIgnored()
    {
        Scan("<EditForm><CascadingValue Value=\"this\" /></EditForm>").Should().BeEmpty();
    }

    [Fact]
    public void Result_IsDistinctAndOrdered()
    {
        Scan("<Pagination /><Icon /><Icon /><Input />")
            .Should()
            .Equal("Icon", "Input", "Pagination");
    }
}
