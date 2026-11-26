using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace Vibe.UI.Docs.Services;

/// <summary>
/// The one list of documentation pages. The component index, the sidebar and the home page
/// counts all read from here, so a page that gains a route shows up everywhere at once.
/// </summary>
public static partial class DocsCatalog
{
    private static readonly IReadOnlyList<CategoryDefinition> CategoryDefinitions =
    [
        new("Layout", "Components for structuring and organizing page content."),
        new("Forms", "Input components for collecting user data."),
        new("Data Display", "Components for presenting information."),
        new("Feedback", "Components for user notifications and feedback."),
        new("Navigation", "Components for navigating between pages and sections."),
        new("Overlay", "Components that appear above other content."),
        new("Utility", "Helper components for common patterns."),
        new("Advanced", "Complex components for specific use cases."),
        new("Other", "Additional component examples and API references."),
    ];

    // Categories are presentation metadata only. Route discovery controls catalog coverage,
    // and newly routed pages automatically fall back to Other until they are classified.
    private static readonly IReadOnlyDictionary<string, string> CategoryByPage = new Dictionary<
        string,
        string
    >(StringComparer.Ordinal)
    {
        ["Accordion"] = "Navigation",
        ["Alert"] = "Feedback",
        ["AlertDialog"] = "Feedback",
        ["AspectRatio"] = "Layout",
        ["Avatar"] = "Data Display",
        ["Badge"] = "Data Display",
        ["Breadcrumb"] = "Navigation",
        ["Button"] = "Forms",
        ["Calendar"] = "Data Display",
        ["Card"] = "Layout",
        ["Carousel"] = "Data Display",
        ["Chart"] = "Data Display",
        ["Checkbox"] = "Forms",
        ["Collapsible"] = "Layout",
        ["ColorPicker"] = "Forms",
        ["Combobox"] = "Forms",
        ["Confetti"] = "Feedback",
        ["Container"] = "Layout",
        ["DataGrid"] = "Data Display",
        ["DatePicker"] = "Forms",
        ["DateRangePicker"] = "Forms",
        ["Dialog"] = "Overlay",
        ["Divider"] = "Layout",
        ["DragDrop"] = "Advanced",
        ["Drawer"] = "Overlay",
        ["Dropdown"] = "Navigation",
        ["FileUpload"] = "Forms",
        ["Form"] = "Forms",
        ["FormField"] = "Forms",
        ["Grid"] = "Layout",
        ["ImageCropper"] = "Advanced",
        ["Input"] = "Forms",
        ["InputOTP"] = "Forms",
        ["KanbanBoard"] = "Advanced",
        ["Link"] = "Navigation",
        ["Mentions"] = "Forms",
        ["Menu"] = "Navigation",
        ["Notification"] = "Feedback",
        ["Pagination"] = "Navigation",
        ["Popover"] = "Overlay",
        ["Progress"] = "Feedback",
        ["RadioGroup"] = "Forms",
        ["Rating"] = "Forms",
        ["Resizable"] = "Layout",
        ["RichTextEditor"] = "Forms",
        ["Select"] = "Forms",
        ["Separator"] = "Layout",
        ["Sheet"] = "Overlay",
        ["Sidebar"] = "Navigation",
        ["Skeleton"] = "Data Display",
        ["Slider"] = "Forms",
        ["Spinner"] = "Feedback",
        ["Splitter"] = "Layout",
        ["Stack"] = "Layout",
        ["Stepper"] = "Navigation",
        ["Switch"] = "Forms",
        ["Table"] = "Data Display",
        ["Tabs"] = "Navigation",
        ["Tag"] = "Data Display",
        ["Textarea"] = "Forms",
        ["ThemeToggle"] = "Utility",
        ["Timeline"] = "Data Display",
        ["Toast"] = "Feedback",
        ["ToggleGroup"] = "Forms",
        ["Tooltip"] = "Overlay",
        ["TransferList"] = "Forms",
        ["TreeView"] = "Data Display",
        ["ValidatedInput"] = "Forms",
        ["VirtualScroll"] = "Data Display",
    };

    private static readonly IReadOnlyDictionary<string, string> DescriptionByPage = new Dictionary<
        string,
        string
    >(StringComparer.Ordinal)
    {
        ["Alert"] = "Important message display",
        ["AspectRatio"] = "Maintain aspect ratio for media",
        ["Avatar"] = "User profile image",
        ["Badge"] = "Small status indicator",
        ["Breadcrumb"] = "Navigation breadcrumb",
        ["Button"] = "Clickable button element",
        ["Calendar"] = "Date display grid",
        ["Card"] = "Container for grouping related content",
        ["Checkbox"] = "Boolean toggle input",
        ["Collapsible"] = "Expandable content",
        ["Combobox"] = "Text box with a filtered list of options",
        ["Container"] = "Responsive layout wrapper",
        ["DataGrid"] = "Tabular data display",
        ["DatePicker"] = "Date selection input",
        ["DateRangePicker"] = "Date range selection",
        ["Dialog"] = "Modal dialog window",
        ["Divider"] = "Visual separator between content",
        ["Drawer"] = "Side navigation drawer",
        ["Dropdown"] = "Dropdown menu",
        ["Form"] = "Form container with validation",
        ["FormField"] = "Field wrapper with label and error",
        ["Grid"] = "Responsive grid system",
        ["Input"] = "Text input field",
        ["KanbanBoard"] = "Drag-and-drop kanban board",
        ["Link"] = "Navigation link",
        ["Menu"] = "Navigation menu",
        ["Pagination"] = "Page navigation",
        ["Popover"] = "Rich content popup",
        ["Progress"] = "Progress indicator",
        ["RadioGroup"] = "Group of radio buttons",
        ["Select"] = "Dropdown selection",
        ["Separator"] = "Visual separator",
        ["Skeleton"] = "Loading placeholder",
        ["Slider"] = "Range value selector",
        ["Spinner"] = "Loading spinner",
        ["Stack"] = "Vertical and horizontal layouts",
        ["Stepper"] = "Multi-step navigation",
        ["Switch"] = "Toggle switch control",
        ["Table"] = "Simple table layout",
        ["Tag"] = "Removable label",
        ["ThemeToggle"] = "Dark and light mode toggle",
        ["Toast"] = "Temporary notifications",
        ["Tooltip"] = "Contextual information popup",
    };

    private static readonly IReadOnlySet<string> NewPages = new HashSet<string>(
        StringComparer.Ordinal
    )
    {
        "KanbanBoard",
    };

    /// <summary>
    /// The prose guides, in reading order. Routes are relative to the deployed base path.
    /// </summary>
    public static IReadOnlyList<GuidePage> Guides { get; } =
    [
        new("Introduction", "getting-started"),
        new("Installation", "installation"),
        new("Theming", "theming"),
        new("CLI", "cli"),
    ];

    /// <summary>
    /// Every routed component page, discovered from the <c>@page "/components/..."</c> routes.
    /// </summary>
    public static IReadOnlyList<ComponentPage> Components { get; } = DiscoverComponents();

    /// <summary>
    /// The component pages grouped for display. Empty categories are left out.
    /// </summary>
    public static IReadOnlyList<ComponentCategory> Categories { get; } = BuildCategories();

    /// <summary>
    /// True when the path (relative to the base, no leading slash) is a guide or a component page,
    /// which are the pages that get the sidebar and the table of contents.
    /// </summary>
    public static bool IsDocsPath(string relativePath)
    {
        var path = relativePath.Trim('/');

        return path.Equals("components", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("components/", StringComparison.OrdinalIgnoreCase)
            || Guides.Any(guide => guide.Route.Equals(path, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<ComponentCategory> BuildCategories()
    {
        return CategoryDefinitions
            .Select(definition => new ComponentCategory(
                definition.Name,
                definition.Description,
                Components
                    .Where(component => component.Category == definition.Name)
                    .OrderBy(component => component.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray()
            ))
            .Where(category => category.Components.Count > 0)
            .ToArray();
    }

    private static IReadOnlyList<ComponentPage> DiscoverComponents()
    {
        return typeof(DocsCatalog)
            .Assembly.ExportedTypes.Where(type =>
                !type.IsAbstract && typeof(IComponent).IsAssignableFrom(type)
            )
            .Select(type => new
            {
                PageType = type,
                Routes = type.GetCustomAttributes<RouteAttribute>(inherit: false)
                    .Select(attribute => NormalizeRoute(attribute.Template))
                    .Where(route =>
                        route.StartsWith("/components/", StringComparison.OrdinalIgnoreCase)
                    )
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
            })
            .Where(page => page.Routes.Length > 0)
            .Select(page =>
            {
                var pageKey = GetPageKey(page.PageType.Name);
                var displayName = SplitPascalCase(pageKey);

                return new ComponentPage(
                    displayName,
                    SelectCanonicalRoute(page.Routes, pageKey),
                    page.Routes,
                    DescriptionByPage.GetValueOrDefault(
                        pageKey,
                        $"{displayName} examples, usage, and API reference."
                    ),
                    CategoryByPage.GetValueOrDefault(pageKey, "Other"),
                    NewPages.Contains(pageKey)
                );
            })
            .OrderBy(component => component.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string GetPageKey(string typeName)
    {
        if (typeName.EndsWith("View", StringComparison.Ordinal))
        {
            return typeName[..^"View".Length];
        }

        return typeName switch
        {
            "TreeViewer" => "TreeView",
            _ => typeName,
        };
    }

    private static string SelectCanonicalRoute(IEnumerable<string> routes, string pageKey)
    {
        var routeList = routes.ToArray();
        var matchingRoute = routeList.FirstOrDefault(route =>
            NormalizeIdentifier(route[(route.LastIndexOf('/') + 1)..])
            == NormalizeIdentifier(pageKey)
        );

        return matchingRoute
            ?? routeList
                .OrderBy(route => route.Length)
                .ThenBy(route => route, StringComparer.OrdinalIgnoreCase)
                .First();
    }

    private static string NormalizeRoute(string route) =>
        route.StartsWith('/') ? route : $"/{route}";

    private static string NormalizeIdentifier(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static string SplitPascalCase(string value)
    {
        var acronymBoundary = AcronymBoundary().Replace(value, "$1 $2");
        return WordBoundary().Replace(acronymBoundary, "$1 $2");
    }

    [GeneratedRegex("([A-Z]+)([A-Z][a-z])")]
    private static partial Regex AcronymBoundary();

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex WordBoundary();

    private sealed record CategoryDefinition(string Name, string Description);
}

/// <summary>A prose guide page. <see cref="Route"/> has no leading slash.</summary>
public sealed record GuidePage(string Title, string Route);

/// <summary>A group of component pages shown under one heading.</summary>
public sealed record ComponentCategory(
    string Name,
    string Description,
    IReadOnlyList<ComponentPage> Components
);

/// <summary>
/// A routed component page. <see cref="Route"/> is the canonical route with a leading slash;
/// <see cref="Routes"/> also holds any aliases.
/// </summary>
public sealed record ComponentPage(
    string Name,
    string Route,
    IReadOnlyList<string> Routes,
    string Description,
    string Category,
    bool IsNew
)
{
    /// <summary>The canonical route relative to the deployed base path.</summary>
    public string RelativeRoute => Route.TrimStart('/');
}
