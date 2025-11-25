/**
 * Shiki Syntax Highlighting Interop
 * Provides VSCode-quality syntax highlighting for code blocks
 */

// Shiki highlighter instance (lazy loaded)
let highlighter = null;
let highlighterPromise = null;

// Supported languages for the docs (web bundle built-in languages)
// Note: razor is not in the web bundle, so we map it to html
const SUPPORTED_LANGUAGES = [
    'csharp',
    'html',
    'css',
    'javascript',
    'typescript',
    'json',
    'xml',
    'bash',
    'powershell',
    'markdown'
];

// Theme mapping
const THEMES = {
    light: 'github-light',
    dark: 'github-dark'
};

/**
 * Initialize the Shiki highlighter
 */
async function initHighlighter() {
    if (highlighter) return highlighter;
    if (highlighterPromise) return highlighterPromise;

    highlighterPromise = (async () => {
        try {
            // Dynamic import of Shiki from CDN - use default export for bundle
            const shiki = await import('https://esm.sh/shiki@1.22.0/bundle/web');

            highlighter = await shiki.createHighlighter({
                themes: [THEMES.light, THEMES.dark],
                langs: SUPPORTED_LANGUAGES
            });

            console.log('[Shiki] Highlighter initialized');
            return highlighter;
        } catch (error) {
            console.error('[Shiki] Failed to initialize:', error);
            throw error;
        }
    })();

    return highlighterPromise;
}

/**
 * Highlight code with Shiki
 * @param {string} code - The code to highlight
 * @param {string} language - The programming language
 * @param {string} theme - 'light' or 'dark'
 * @returns {string} HTML string with highlighted code
 */
window.highlightCode = async function(code, language, theme) {
    try {
        const hl = await initHighlighter();

        // Normalize language name
        let lang = (language || 'text').toLowerCase();

        // Handle common aliases
        if (lang === 'cs') lang = 'csharp';
        if (lang === 'js') lang = 'javascript';
        if (lang === 'ts') lang = 'typescript';
        if (lang === 'sh' || lang === 'shell') lang = 'bash';
        if (lang === 'ps' || lang === 'ps1') lang = 'powershell';
        if (lang === 'md') lang = 'markdown';
        if (lang === 'razor' || lang === 'cshtml' || lang === 'blazor') lang = 'html'; // Razor -> HTML fallback

        // Check if language is supported
        if (!SUPPORTED_LANGUAGES.includes(lang)) {
            lang = 'text';
        }

        const themeName = theme === 'dark' ? THEMES.dark : THEMES.light;

        const html = hl.codeToHtml(code, {
            lang: lang,
            theme: themeName
        });

        return html;
    } catch (error) {
        console.error('[Shiki] Highlight error:', error);
        // Fallback to escaped HTML
        return `<pre><code>${escapeHtml(code)}</code></pre>`;
    }
};

/**
 * Check if highlighter is ready
 */
window.isHighlighterReady = function() {
    return highlighter !== null;
};

/**
 * Pre-initialize the highlighter (call early for faster first highlight)
 */
window.preloadHighlighter = async function() {
    try {
        await initHighlighter();
        return true;
    } catch {
        return false;
    }
};

/**
 * Escape HTML characters for fallback display
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Auto-preload highlighter when script loads (non-blocking)
setTimeout(() => {
    window.preloadHighlighter().catch(() => {
        // Ignore preload errors - will retry on first use
    });
}, 1000);
