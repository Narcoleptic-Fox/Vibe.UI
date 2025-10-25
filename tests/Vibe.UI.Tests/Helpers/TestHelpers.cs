using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Vibe.UI.Tests.Helpers;

/// <summary>
/// Builder class for creating test components with fluent API.
/// </summary>
public class ComponentTestBuilder<TComponent> where TComponent : IComponent
{
    private readonly TestContext _context;
    private readonly ComponentParameterCollectionBuilder<TComponent> _parameters;

    public ComponentTestBuilder(TestContext context)
    {
        _context = context;
        _parameters = new ComponentParameterCollectionBuilder<TComponent>();
    }

    public ComponentTestBuilder<TComponent> WithParameter(string name, object? value)
    {
        _parameters.Add(name, value);
        return this;
    }

    public ComponentTestBuilder<TComponent> WithChildContent(string content)
    {
        _parameters.AddChildContent(content);
        return this;
    }

    public ComponentTestBuilder<TComponent> WithChildContent(RenderFragment fragment)
    {
        _parameters.AddChildContent(fragment);
        return this;
    }

    public IRenderedComponent<TComponent> Render()
    {
        return _context.RenderComponent<TComponent>(_parameters.Build());
    }
}

/// <summary>
/// Helper methods for testing components.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a ComponentTestBuilder for the specified component.
    /// </summary>
    public static ComponentTestBuilder<TComponent> CreateBuilder<TComponent>(this TestContext context)
        where TComponent : IComponent
    {
        return new ComponentTestBuilder<TComponent>(context);
    }

    /// <summary>
    /// Asserts that an element has the specified CSS class.
    /// </summary>
    public static void ShouldHaveClass(this IElement element, string className)
    {
        element.ClassList.Should().Contain(className,
            $"element should have class '{className}' but has: {string.Join(", ", element.ClassList)}");
    }

    /// <summary>
    /// Asserts that an element does not have the specified CSS class.
    /// </summary>
    public static void ShouldNotHaveClass(this IElement element, string className)
    {
        element.ClassList.Should().NotContain(className,
            $"element should not have class '{className}'");
    }

    /// <summary>
    /// Asserts that an element has the specified attribute.
    /// </summary>
    public static void ShouldHaveAttribute(this IElement element, string attributeName)
    {
        element.HasAttribute(attributeName).Should().BeTrue(
            $"element should have attribute '{attributeName}'");
    }

    /// <summary>
    /// Asserts that an element has the specified attribute with the specified value.
    /// </summary>
    public static void ShouldHaveAttribute(this IElement element, string attributeName, string expectedValue)
    {
        element.GetAttribute(attributeName).Should().Be(expectedValue,
            $"element attribute '{attributeName}' should have value '{expectedValue}'");
    }

    /// <summary>
    /// Asserts that an element is disabled.
    /// </summary>
    public static void ShouldBeDisabled(this IElement element)
    {
        element.HasAttribute("disabled").Should().BeTrue("element should be disabled");
    }

    /// <summary>
    /// Asserts that an element is enabled.
    /// </summary>
    public static void ShouldBeEnabled(this IElement element)
    {
        element.HasAttribute("disabled").Should().BeFalse("element should be enabled");
    }

    /// <summary>
    /// Asserts that an element is visible.
    /// </summary>
    public static void ShouldBeVisible(this IElement element)
    {
        var style = element.GetAttribute("style") ?? "";
        style.Should().NotContain("display: none", "element should be visible");
        style.Should().NotContain("visibility: hidden", "element should be visible");
    }

    /// <summary>
    /// Gets the text content of an element, trimmed.
    /// </summary>
    public static string GetTrimmedText(this IElement element)
    {
        return element.TextContent.Trim();
    }

    /// <summary>
    /// Simulates typing into an input element.
    /// </summary>
    public static void TypeText(this IElement input, string text)
    {
        input.Change(text);
    }

    /// <summary>
    /// Simulates a click with modifier keys.
    /// </summary>
    public static void ClickWith(this IElement element, bool ctrl = false, bool shift = false, bool alt = false)
    {
        var args = new MouseEventArgs
        {
            CtrlKey = ctrl,
            ShiftKey = shift,
            AltKey = alt
        };
        element.Click(args);
    }

    /// <summary>
    /// Waits for an element to appear in the DOM.
    /// </summary>
    public static IElement WaitForElement(this IRenderedFragment fragment, string selector, TimeSpan? timeout = null)
    {
        return fragment.WaitForElement(selector, timeout ?? TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Asserts that a component rendered without errors.
    /// </summary>
    public static void ShouldRenderWithoutErrors<TComponent>(this IRenderedComponent<TComponent> component)
        where TComponent : IComponent
    {
        component.Should().NotBeNull();
        component.Markup.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Creates a simple RenderFragment with text content.
    /// </summary>
    public static RenderFragment CreateFragment(string text)
    {
        return builder => builder.AddContent(0, text);
    }

    /// <summary>
    /// Creates a RenderFragment from markup.
    /// </summary>
    public static RenderFragment CreateFragmentFromMarkup(string markup)
    {
        return builder => builder.AddMarkupContent(0, markup);
    }
}

/// <summary>
/// Test data builders for common scenarios.
/// </summary>
public static class TestDataBuilders
{
    /// <summary>
    /// Generates a list of test strings.
    /// </summary>
    public static List<string> GenerateStrings(int count, string prefix = "Item")
    {
        return Enumerable.Range(1, count).Select(i => $"{prefix} {i}").ToList();
    }

    /// <summary>
    /// Generates test data with properties.
    /// </summary>
    public static List<T> GenerateTestData<T>(int count, Func<int, T> factory)
    {
        return Enumerable.Range(1, count).Select(factory).ToList();
    }

    /// <summary>
    /// Creates a test EventCallback.
    /// </summary>
    public static EventCallback CreateCallback(Action action)
    {
        return EventCallback.Factory.Create(new object(), action);
    }

    /// <summary>
    /// Creates a test EventCallback with parameter.
    /// </summary>
    public static EventCallback<T> CreateCallback<T>(Action<T> action)
    {
        return EventCallback.Factory.Create(new object(), action);
    }
}
