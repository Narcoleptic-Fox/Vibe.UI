using Vibe.UI.CLI.Models;

namespace Vibe.UI.CLI.Services;

public class ComponentService
{
    private readonly Dictionary<string, ComponentInfo> _components;

    public ComponentService()
    {
        _components = InitializeComponents();
    }

    public List<ComponentInfo> GetAvailableComponents()
    {
        return _components.Values.OrderBy(c => c.Category).ThenBy(c => c.Name).ToList();
    }

    public ComponentInfo? GetComponent(string name)
    {
        var key = name.ToLowerInvariant();
        return _components.TryGetValue(key, out var component) ? component : null;
    }

    public List<string> GetInstalledComponents(string projectPath, string componentsDir)
    {
        var installedPath = Path.Combine(projectPath, componentsDir);

        if (!Directory.Exists(installedPath))
            return new List<string>();

        var razorFiles = Directory.GetFiles(installedPath, "*.razor", SearchOption.AllDirectories);
        return razorFiles.Select(Path.GetFileNameWithoutExtension).ToList();
    }

    public async Task InstallComponentAsync(string projectPath, string componentsDir, string componentName, bool overwrite = false)
    {
        var component = GetComponent(componentName);
        if (component == null)
            throw new InvalidOperationException($"Component '{componentName}' not found.");

        var targetDir = Path.Combine(projectPath, componentsDir, component.Category);
        Directory.CreateDirectory(targetDir);

        // Copy .razor file
        var razorFile = Path.Combine(targetDir, $"{component.Name}.razor");
        if (!File.Exists(razorFile) || overwrite)
        {
            var razorContent = GetComponentTemplate(component.Name);
            await File.WriteAllTextAsync(razorFile, razorContent);
        }

        // Copy .razor.css file if component has CSS
        if (component.HasCss)
        {
            var cssFile = Path.Combine(targetDir, $"{component.Name}.razor.css");
            if (!File.Exists(cssFile) || overwrite)
            {
                var cssContent = GetComponentCssTemplate(component.Name);
                await File.WriteAllTextAsync(cssFile, cssContent);
            }
        }
    }

    private Dictionary<string, ComponentInfo> InitializeComponents()
    {
        var components = new Dictionary<string, ComponentInfo>
        {
            // Input Components
            ["button"] = new ComponentInfo { Name = "Button", Category = "Input", Description = "Displays a button or a component that looks like a button" },
            ["checkbox"] = new ComponentInfo { Name = "Checkbox", Category = "Input", Description = "A control that allows the user to toggle between checked and not checked" },
            ["input"] = new ComponentInfo { Name = "Input", Category = "Input", Description = "Displays a form input field or a component that looks like an input field" },
            ["radio"] = new ComponentInfo { Name = "Radio", Category = "Input", Description = "A radio button control" },
            ["radiogroup"] = new ComponentInfo { Name = "RadioGroup", Category = "Input", Description = "A set of checkable buttons where no more than one can be checked at a time", Dependencies = new List<string> { "RadioGroupItem" } },
            ["radiogroupitem"] = new ComponentInfo { Name = "RadioGroupItem", Category = "Input", Description = "An item within a radio group" },
            ["select"] = new ComponentInfo { Name = "Select", Category = "Input", Description = "Displays a list of options for the user to pick from" },
            ["slider"] = new ComponentInfo { Name = "Slider", Category = "Input", Description = "An input where the user selects a value from within a given range" },
            ["switch"] = new ComponentInfo { Name = "Switch", Category = "Input", Description = "A control that allows the user to toggle between checked and not checked" },
            ["textarea"] = new ComponentInfo { Name = "TextArea", Category = "Input", Description = "Displays a form textarea or a component that looks like a textarea" },
            ["toggle"] = new ComponentInfo { Name = "Toggle", Category = "Input", Description = "A two-state button that can be either on or off" },
            ["togglegroup"] = new ComponentInfo { Name = "ToggleGroup", Category = "Input", Description = "A set of two-state buttons that can be toggled on or off", Dependencies = new List<string> { "ToggleGroupItem" } },
            ["togglegroupitem"] = new ComponentInfo { Name = "ToggleGroupItem", Category = "Input", Description = "An item within a toggle group" },
            ["colorpicker"] = new ComponentInfo { Name = "ColorPicker", Category = "Input", Description = "A color picker component for selecting colors" },
            ["multiselect"] = new ComponentInfo { Name = "MultiSelect", Category = "Input", Description = "Select multiple items from a dropdown" },

            // Form Components
            ["form"] = new ComponentInfo { Name = "Form", Category = "Form", Description = "Building forms with validation" },
            ["formfield"] = new ComponentInfo { Name = "FormField", Category = "Form", Description = "A form field wrapper" },
            ["formlabel"] = new ComponentInfo { Name = "FormLabel", Category = "Form", Description = "Renders an accessible label associated with controls" },
            ["formmessage"] = new ComponentInfo { Name = "FormMessage", Category = "Form", Description = "Displays form validation messages" },
            ["label"] = new ComponentInfo { Name = "Label", Category = "Form", Description = "Renders an accessible label associated with controls" },
            ["combobox"] = new ComponentInfo { Name = "Combobox", Category = "Form", Description = "Autocomplete input and command palette with a list of suggestions" },

            // Data Display
            ["avatar"] = new ComponentInfo { Name = "Avatar", Category = "DataDisplay", Description = "An image element with a fallback for representing the user" },
            ["badge"] = new ComponentInfo { Name = "Badge", Category = "DataDisplay", Description = "Displays a badge or a component that looks like a badge" },
            ["datatable"] = new ComponentInfo { Name = "DataTable", Category = "DataDisplay", Description = "Powerful table with sorting, filtering, and pagination" },
            ["progress"] = new ComponentInfo { Name = "Progress", Category = "DataDisplay", Description = "Displays an indicator showing the completion progress of a task" },
            ["table"] = new ComponentInfo { Name = "Table", Category = "DataDisplay", Description = "A responsive table component" },
            ["chart"] = new ComponentInfo { Name = "Chart", Category = "DataDisplay", Description = "Data visualization charts" },

            // Layout
            ["aspectratio"] = new ComponentInfo { Name = "AspectRatio", Category = "Layout", Description = "Displays content within a desired aspect ratio" },
            ["card"] = new ComponentInfo { Name = "Card", Category = "Layout", Description = "Displays a card with header, content, and footer" },
            ["separator"] = new ComponentInfo { Name = "Separator", Category = "Layout", Description = "Visually or semantically separates content" },
            ["resizable"] = new ComponentInfo { Name = "Resizable", Category = "Layout", Description = "Accessible resizable panel groups and layouts" },
            ["sheet"] = new ComponentInfo { Name = "Sheet", Category = "Layout", Description = "Extends the Dialog component to display content that complements the main content" },

            // Navigation
            ["breadcrumb"] = new ComponentInfo { Name = "Breadcrumb", Category = "Navigation", Description = "Displays the path to the current resource using a hierarchy of links", Dependencies = new List<string> { "BreadcrumbItem" } },
            ["breadcrumbitem"] = new ComponentInfo { Name = "BreadcrumbItem", Category = "Navigation", Description = "An item within a breadcrumb" },
            ["menubar"] = new ComponentInfo { Name = "Menubar", Category = "Navigation", Description = "A visually persistent menu common in desktop applications" },
            ["navigationmenu"] = new ComponentInfo { Name = "NavigationMenu", Category = "Navigation", Description = "A collection of links for navigating websites", Dependencies = new List<string> { "NavigationMenuItem" } },
            ["navigationmenuitem"] = new ComponentInfo { Name = "NavigationMenuItem", Category = "Navigation", Description = "An item within a navigation menu" },
            ["pagination"] = new ComponentInfo { Name = "Pagination", Category = "Navigation", Description = "Pagination with page navigation, next and previous links" },
            ["tabs"] = new ComponentInfo { Name = "Tabs", Category = "Navigation", Description = "A set of layered sections of content", Dependencies = new List<string> { "TabItem" } },
            ["tabitem"] = new ComponentInfo { Name = "TabItem", Category = "Navigation", Description = "An item within tabs" },

            // Overlay
            ["alertdialog"] = new ComponentInfo { Name = "AlertDialog", Category = "Overlay", Description = "A modal dialog that interrupts the user with important content" },
            ["contextmenu"] = new ComponentInfo { Name = "ContextMenu", Category = "Overlay", Description = "Displays a menu to the user triggered by right-click", Dependencies = new List<string> { "ContextMenuItem" } },
            ["contextmenuitem"] = new ComponentInfo { Name = "ContextMenuItem", Category = "Overlay", Description = "An item within a context menu" },
            ["dialog"] = new ComponentInfo { Name = "Dialog", Category = "Overlay", Description = "A window overlaid on either the primary window or another dialog" },
            ["drawer"] = new ComponentInfo { Name = "Drawer", Category = "Overlay", Description = "A panel that slides out from the edge of the screen" },
            ["hovercard"] = new ComponentInfo { Name = "HoverCard", Category = "Overlay", Description = "For sighted users to preview content available behind a link" },
            ["popover"] = new ComponentInfo { Name = "Popover", Category = "Overlay", Description = "Displays rich content in a portal, triggered by a button" },
            ["tooltip"] = new ComponentInfo { Name = "Tooltip", Category = "Overlay", Description = "A popup that displays information related to an element" },

            // Feedback
            ["alert"] = new ComponentInfo { Name = "Alert", Category = "Feedback", Description = "Displays a callout for user attention" },
            ["skeleton"] = new ComponentInfo { Name = "Skeleton", Category = "Feedback", Description = "Use to show a placeholder while content is loading" },
            ["toast"] = new ComponentInfo { Name = "Toast", Category = "Feedback", Description = "A succinct message that is displayed temporarily", Dependencies = new List<string> { "ToastContainer" } },
            ["toastcontainer"] = new ComponentInfo { Name = "ToastContainer", Category = "Feedback", Description = "Container for toast notifications" },

            // Date & Time
            ["calendar"] = new ComponentInfo { Name = "Calendar", Category = "DateTime", Description = "A date field component that allows users to enter and edit date" },
            ["datepicker"] = new ComponentInfo { Name = "DatePicker", Category = "DateTime", Description = "A date picker component with calendar dropdown" },
            ["daterangepicker"] = new ComponentInfo { Name = "DateRangePicker", Category = "DateTime", Description = "A date range picker component" },

            // Utility
            ["command"] = new ComponentInfo { Name = "Command", Category = "Utility", Description = "Fast, composable, unstyled command menu" },
            ["scrollarea"] = new ComponentInfo { Name = "ScrollArea", Category = "Utility", Description = "Augments native scroll functionality for custom, cross-browser styling" },
            ["dropdownmenu"] = new ComponentInfo { Name = "DropdownMenu", Category = "Utility", Description = "Displays a menu to the user triggered by a button" },

            // Disclosure
            ["accordion"] = new ComponentInfo { Name = "Accordion", Category = "Disclosure", Description = "A vertically stacked set of interactive headings", Dependencies = new List<string> { "AccordionItem" } },
            ["accordionitem"] = new ComponentInfo { Name = "AccordionItem", Category = "Disclosure", Description = "An item within an accordion" },
            ["carousel"] = new ComponentInfo { Name = "Carousel", Category = "Disclosure", Description = "A carousel with motion and swipe support", Dependencies = new List<string> { "CarouselItem" } },
            ["carouselitem"] = new ComponentInfo { Name = "CarouselItem", Category = "Disclosure", Description = "An item within a carousel" },
            ["collapsible"] = new ComponentInfo { Name = "Collapsible", Category = "Disclosure", Description = "An interactive component which expands/collapses a panel" },
        };

        return components;
    }

    private string GetComponentTemplate(string componentName)
    {
        // This would ideally load from embedded resources or template files
        // For now, return a placeholder
        return $@"@namespace Vibe.UI.Components
@inherits ThemedComponentBase

<div class=""vibe-{componentName.ToLowerInvariant()}"">
    @ChildContent
</div>

@code {{
    [Parameter]
    public RenderFragment? ChildContent {{ get; set; }}

    [Parameter]
    public string? CssClass {{ get; set; }}

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes {{ get; set; }}

    private string CombinedCssClass => CombineClasses(
        ""vibe-{componentName.ToLowerInvariant()}"",
        CssClass
    );
}}";
    }

    private string GetComponentCssTemplate(string componentName)
    {
        return $@".vibe-{componentName.ToLowerInvariant()} {{
    /* Add component styles here */
}}";
    }
}
