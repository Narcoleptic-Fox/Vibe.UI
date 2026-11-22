using System.Text.RegularExpressions;

namespace Vibe.UI.CLI.Services;

/// <summary>
/// Finds which other components a component's Razor source refers to, so that
/// <c>vibe add</c> can install them without a hand-kept dependency list.
/// </summary>
/// <remarks>
/// This is a text scan, not a compiler. It looks for a component name only where a type can
/// stand: a markup tag, a declaration, a generic argument, or after <c>new</c>, <c>typeof</c>,
/// <c>is</c> and <c>as</c>. A parameter that merely shares a component's name
/// (<c>public string? Icon { get; set; }</c>) is not a reference. A wrong guess in either
/// direction is caught by scripts/Validate-VendoredComponents.ps1, which compiles every
/// component on its own after <c>vibe add</c>.
/// </remarks>
public static partial class ComponentReferenceScanner
{
    // A component name, optionally followed by a nested type: TreeView.TreeNode
    private const string TypeName = @"(?<name>[A-Z]\w*)(?:\.[A-Z]\w*)*";

    [GeneratedRegex(@"@\*.*?\*@|<!--.*?-->|/\*.*?\*/|//[^\n]*", RegexOptions.Singleline)]
    private static partial Regex Comments();

    // <Icon Name="x" />, <CarouselItem>
    [GeneratedRegex(@"<(?<name>[A-Z]\w*)(?=[\s/>])")]
    private static partial Regex MarkupTags();

    // private Accordion? Parent { get; set; }   public TreeView.TreeNode? Item { get; set; }
    [GeneratedRegex(@"(?<![\w.@""])" + TypeName + @"\??(?:\[\])?\s+[A-Za-z_]\w*\s*(?:[{;=,)])")]
    private static partial Regex Declarations();

    // IReadOnlyList<TreeView.TreeNode>, Dictionary<string, TabItem?>
    [GeneratedRegex(@"<(?:[^<>;(){}]*,)?\s*" + TypeName + @"\??\s*(?:,[^<>;(){}]*)?>")]
    private static partial Regex GenericArguments();

    // new Toast(), typeof(Dialog), x is Tabs, x as Tabs
    [GeneratedRegex(@"\b(?:new\s+|typeof\(\s*|is\s+|as\s+)" + TypeName + @"\b")]
    private static partial Regex Expressions();

    /// <summary>
    /// Returns the names from <paramref name="componentNames"/> that
    /// <paramref name="razorSource"/> refers to as a type, in ordinal order.
    /// </summary>
    public static IReadOnlyList<string> FindReferencedComponents(
        string razorSource,
        IReadOnlyCollection<string> componentNames
    )
    {
        var known =
            componentNames as IReadOnlySet<string>
            ?? new HashSet<string>(componentNames, StringComparer.Ordinal);
        var source = Comments().Replace(razorSource, " ");
        var found = new SortedSet<string>(StringComparer.Ordinal);

        foreach (
            var pattern in new[] { MarkupTags(), Declarations(), GenericArguments(), Expressions() }
        )
        {
            foreach (Match match in pattern.Matches(source))
            {
                var name = match.Groups["name"].Value;
                if (known.Contains(name))
                {
                    found.Add(name);
                }
            }
        }

        return found.ToList();
    }
}
