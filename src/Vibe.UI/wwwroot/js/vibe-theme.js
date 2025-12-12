/**
 * Vibe.UI Theme Management Module
 * Handles theme toggling, persistence, and system preference detection
 * CSP-friendly - no eval() usage
 */

export const VibeTheme = {
    /**
     * Gets the current theme from storage or system preference
     * @param {string} storageKey - The localStorage key for theme preference
     * @returns {boolean} True if dark mode, false if light mode
     */
    getTheme: function(storageKey) {
        const stored = localStorage.getItem(storageKey);
        if (stored) {
            return stored === 'dark';
        }
        return window.matchMedia('(prefers-color-scheme: dark)').matches;
    },

    /**
     * Sets the theme and persists to localStorage
     * @param {string} storageKey - The localStorage key for theme preference
     * @param {boolean} isDark - True for dark mode, false for light mode
     */
    setTheme: function(storageKey, isDark) {
        const theme = isDark ? 'dark' : 'light';

        if (isDark) {
            document.documentElement.classList.add('dark');
        } else {
            document.documentElement.classList.remove('dark');
        }

        localStorage.setItem(storageKey, theme);
    },

    /**
     * Toggles the current theme
     * @param {string} storageKey - The localStorage key for theme preference
     * @returns {boolean} The new theme state (true = dark)
     */
    toggleTheme: function(storageKey) {
        const isDark = document.documentElement.classList.contains('dark');
        const newIsDark = !isDark;
        this.setTheme(storageKey, newIsDark);
        return newIsDark;
    },

    /**
     * Listens for system theme changes
     * @param {function} callback - Called with true/false when system preference changes
     * @returns {function} Cleanup function to remove listener
     */
    onSystemThemeChange: function(callback) {
        const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
        const handler = (e) => callback(e.matches);
        mediaQuery.addEventListener('change', handler);
        return () => mediaQuery.removeEventListener('change', handler);
    }
};

// Export for global access (fallback for non-module scenarios)
window.VibeTheme = VibeTheme;
