using System.Text.RegularExpressions;

namespace Vibe.UI.Tests.Components.DataGrid;

/// <summary>
/// B3.1: the "vibe" theme for QuickGrid. It was a kill criterion for wrapping QuickGrid at all
/// that its markup can be styled without forcing anything, so the stylesheet is read here.
/// </summary>
public partial class VibeGridThemeTests
{
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Vibe.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull();
        return directory.FullName;
    }

    private static string Theme() =>
        File.ReadAllText(
            Path.Combine(
                RepositoryRoot(),
                "src",
                "Vibe.UI",
                "Components",
                "DataDisplay",
                "VibeGrid.razor.css"
            )
        );

    // Comments describe the rule in words, so they are not what is being checked.
    private static string WithoutComments(string css) => Comments().Replace(css, string.Empty);

    [Fact]
    public void The_theme_forces_nothing()
    {
        string css = WithoutComments(Theme());

        css.ShouldNotBeEmpty();
        Important().Matches(css).Count.ShouldBe(0);
    }

    [Fact]
    public void Every_rule_for_QuickGrids_markup_starts_at_the_grids_own_root()
    {
        string css = WithoutComments(Theme());
        string[] selectors = Rules()
            .Matches(css)
            .SelectMany(rule => rule.Groups["selectors"].Value.Split(','))
            // However the stylesheet is wrapped, a selector is one line here.
            .Select(selector => Whitespace().Replace(selector, " ").Trim())
            .Where(selector => selector.Length > 0)
            .ToArray();

        // The table belongs to a child component: a rule that does not reach in from the root
        // is scoped to markup this component never renders, and silently styles nothing.
        string[] forTheTable = selectors
            .Where(selector => selector.Contains(".quickgrid"))
            .ToArray();
        forTheTable.Length.ShouldBeGreaterThan(10);
        forTheTable.ShouldAllBe(selector =>
            selector.StartsWith(
                ".vibe-grid ::deep .quickgrid[theme=\"vibe\"]",
                StringComparison.Ordinal
            )
        );
    }

    [Fact]
    public void Every_token_the_theme_reads_is_one_the_base_stylesheet_defines()
    {
        string baseCss = File.ReadAllText(
            Path.Combine(RepositoryRoot(), "src", "Vibe.UI", "wwwroot", "css", "vibe-base.css")
        );
        string[] used = Tokens()
            .Matches(WithoutComments(Theme()))
            .Select(match => match.Groups["name"].Value)
            .Distinct()
            .Order()
            .ToArray();

        // A misspelt token is not an error in CSS. It is a colour that is not there.
        used.Length.ShouldBeGreaterThan(5);
        string[] undefined = used.Where(name =>
                !baseCss.Contains(name + ":", StringComparison.Ordinal)
            )
            .ToArray();
        undefined.ShouldBeEmpty();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex Comments();

    [GeneratedRegex(@"!\s*important", RegexOptions.IgnoreCase)]
    private static partial Regex Important();

    [GeneratedRegex(@"(?<selectors>[^{}]+)\{[^{}]*\}")]
    private static partial Regex Rules();

    [GeneratedRegex(@"var\((?<name>--vibe-[a-z0-9-]+)")]
    private static partial Regex Tokens();
}
