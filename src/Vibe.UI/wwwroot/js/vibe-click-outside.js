/**
 * Vibe.UI Click Outside Detection Module
 * Handles click-outside events for dropdowns, menus, dialogs, etc.
 */

const clickOutsideHandlers = new Map();

/**
 * Registers a click-outside handler for an element
 * @param {string} elementId - The element ID to watch
 * @param {object} dotNetRef - .NET object reference for callback
 * @param {string} methodName - Method to invoke when clicked outside
 */
export function register(elementId, dotNetRef, methodName = 'OnClickOutside') {
    // Remove existing handler if any
    unregister(elementId);

    const handler = (event) => {
        const element = document.getElementById(elementId);
        if (element && !element.contains(event.target)) {
            dotNetRef.invokeMethodAsync(methodName);
        }
    };

    // Delay adding listener to avoid immediate trigger
    setTimeout(() => {
        document.addEventListener('mousedown', handler);
        document.addEventListener('touchstart', handler);
        clickOutsideHandlers.set(elementId, handler);
    }, 0);
}

/**
 * Unregisters a click-outside handler
 * @param {string} elementId - The element ID
 */
export function unregister(elementId) {
    const handler = clickOutsideHandlers.get(elementId);
    if (handler) {
        document.removeEventListener('mousedown', handler);
        document.removeEventListener('touchstart', handler);
        clickOutsideHandlers.delete(elementId);
    }
}

/**
 * Unregisters all click-outside handlers
 */
export function unregisterAll() {
    clickOutsideHandlers.forEach((handler, elementId) => {
        document.removeEventListener('mousedown', handler);
        document.removeEventListener('touchstart', handler);
    });
    clickOutsideHandlers.clear();
}

// Global access fallback
window.VibeClickOutside = { register, unregister, unregisterAll };
