using Microsoft.Playwright;

namespace Vibe.UI.Docs.E2E.PageObjects;

/// <summary>
/// Page object for component documentation pages
/// </summary>
public class ComponentPage : BasePage
{
    public ComponentPage(IPage page) : base(page)
    {
    }

    // Component Page Elements
    public ILocator ComponentTitle => Page.GetByRole(AriaRole.Heading, new() { Level = 1 });
    public ILocator ComponentDescription => Page.Locator(".component-description, .text-zinc-600").First;

    // Live Preview Section
    public ILocator LivePreview => Page.Locator(".live-preview, .preview-container");
    public ILocator PreviewContent => LivePreview.Locator("> *").First;

    // Code Block Section
    public ILocator CodeBlocks => Page.Locator("pre code");
    public ILocator FirstCodeBlock => CodeBlocks.First;

    // Prop Tweaker (interactive property controls)
    public ILocator PropTweaker => Page.Locator(".prop-tweaker, [class*='prop-control']");

    // API Reference Section
    public ILocator ApiReference => Page.Locator(".api-reference, h2:has-text('API Reference')");
    public ILocator PropsTable => Page.Locator("table").Filter(new()
    {
        Has = Page.Locator("th:has-text('Prop'), th:has-text('Property')")
    });

    /// <summary>
    /// Navigate to a specific component page
    /// </summary>
    public async Task NavigateToAsync(string componentName)
    {
        var url = $"/components/{componentName.ToLowerInvariant()}";
        await Page.GotoAsync(url);
    }

    /// <summary>
    /// Get the component title text
    /// </summary>
    public async Task<string> GetComponentTitleAsync()
    {
        return await ComponentTitle.TextContentAsync() ?? string.Empty;
    }

    /// <summary>
    /// Check if code blocks have syntax highlighting applied
    /// Shiki adds class attributes and span elements for syntax highlighting
    /// </summary>
    public async Task<bool> HasSyntaxHighlightingAsync()
    {
        var firstBlock = FirstCodeBlock;
        if (!await firstBlock.IsVisibleAsync())
        {
            return false;
        }

        // Check if code block has child spans (syntax highlighting adds spans)
        var spanCount = await firstBlock.Locator("span").CountAsync();
        return spanCount > 0;
    }

    /// <summary>
    /// Get the number of code blocks on the page
    /// </summary>
    public async Task<int> GetCodeBlockCountAsync()
    {
        return await CodeBlocks.CountAsync();
    }

    /// <summary>
    /// Check if live preview is visible
    /// </summary>
    public async Task<bool> HasLivePreviewAsync()
    {
        return await LivePreview.IsVisibleAsync();
    }

    /// <summary>
    /// Check if API reference section exists
    /// </summary>
    public async Task<bool> HasApiReferenceAsync()
    {
        return await ApiReference.IsVisibleAsync();
    }

    /// <summary>
    /// Interact with a prop tweaker control
    /// </summary>
    public async Task AdjustPropAsync(string propName, string value)
    {
        var control = Page.Locator($"[data-prop='{propName}'], label:has-text('{propName}')");
        await control.FillAsync(value);
    }
}
