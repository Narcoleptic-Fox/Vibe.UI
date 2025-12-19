using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;

namespace Vibe.UI.Base
{
    /// <summary>
    /// Base class for all Vibe.UI components.
    /// Provides common functionality for CSS class management and attribute handling.
    /// Theming is handled via pure CSS using :root and .dark selectors following shadcn/ui patterns.
    /// </summary>
    public abstract class VibeComponent : ComponentBase
    {
        /// <summary>
        /// Gets or sets the CSS class for the component.
        /// </summary>
        [Parameter]
        public string? Class { get; set; }

        /// <summary>
        /// Gets or sets additional attributes for the component.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object>? AdditionalAttributes { get; set; }

        /// <summary>
        /// Gets the combined CSS class including component-specific and custom classes.
        /// </summary>
        protected string CombinedClass => GetCombinedClass();

        /// <summary>
        /// Gets the component-specific CSS class.
        /// Override this in derived components to provide the base CSS class.
        /// </summary>
        protected abstract string ComponentClass { get; }

        /// <summary>
        /// Gets the combined CSS class.
        /// Combines the component's base class with any custom classes provided via the Class parameter.
        /// </summary>
        protected virtual string GetCombinedClass()
        {
            var classList = new List<string> { ComponentClass };

            if (!string.IsNullOrWhiteSpace(Class))
            {
                classList.Add(Class);
            }

            return string.Join(" ", classList.Where(c => !string.IsNullOrWhiteSpace(c)));
        }

        /// <summary>
        /// Combines multiple CSS classes into a single string, filtering out null or whitespace values.
        /// </summary>
        /// <param name="classes">The CSS classes to combine.</param>
        /// <returns>A space-separated string of non-null, non-whitespace CSS classes.</returns>
        protected static string CombineClasses(params string?[] classes)
        {
            return string.Join(" ", classes.Where(c => !string.IsNullOrWhiteSpace(c)));
        }
    }
}