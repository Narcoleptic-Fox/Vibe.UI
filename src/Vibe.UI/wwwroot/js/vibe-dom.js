/**
 * Vibe.UI DOM Utilities Module
 * CSP-safe DOM operations for Blazor components
 */

/**
 * Gets the bounding client rect for an element by ID
 * @param {string} elementId - The element ID
 * @returns {DOMRect|null} The bounding rect or null if not found
 */
export function getBoundingRect(elementId) {
    const element = document.getElementById(elementId);
    if (!element) return null;
    return element.getBoundingClientRect();
}

/**
 * Gets the bounding client rect for an element's parent
 * @param {string} elementId - The element ID
 * @returns {DOMRect|null} The parent's bounding rect or null if not found
 */
export function getParentBoundingRect(elementId) {
    const element = document.getElementById(elementId);
    if (!element || !element.parentElement) return null;
    return element.parentElement.getBoundingClientRect();
}

/**
 * Focuses an element by ID
 * @param {string} elementId - The element ID
 */
export function focusElement(elementId) {
    const element = document.getElementById(elementId);
    if (element) element.focus();
}

/**
 * Scrolls an element into view
 * @param {string} elementId - The element ID
 * @param {object} options - ScrollIntoView options
 */
export function scrollIntoView(elementId, options = { behavior: 'smooth', block: 'nearest' }) {
    const element = document.getElementById(elementId);
    if (element) element.scrollIntoView(options);
}

/**
 * Gets the scroll position of an element
 * @param {string} elementId - The element ID
 * @returns {object|null} {scrollTop, scrollLeft} or null
 */
export function getScrollPosition(elementId) {
    const element = document.getElementById(elementId);
    if (!element) return null;
    return { scrollTop: element.scrollTop, scrollLeft: element.scrollLeft };
}

/**
 * Sets the scroll position of an element
 * @param {string} elementId - The element ID
 * @param {number} top - Scroll top position
 * @param {number} left - Scroll left position
 */
export function setScrollPosition(elementId, top, left) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = top;
        element.scrollLeft = left;
    }
}

// Export for global access (fallback)
window.VibeDom = {
    getBoundingRect,
    getParentBoundingRect,
    focusElement,
    scrollIntoView,
    getScrollPosition,
    setScrollPosition
};
