/**
 * Shiki Syntax Highlighting Interop
 * Provides VSCode-quality syntax highlighting for code blocks
 */

// Shiki highlighter instance (lazy loaded)
let highlighter = null;
let highlighterPromise = null;
let initializationAttempts = 0;
const MAX_INIT_ATTEMPTS = 3;

// Languages included in the Shiki web bundle
// See: https://shiki.style/languages for full list
// The web bundle includes: html, css, javascript, typescript, json, markdown, yaml, xml
// Languages NOT in web bundle that we need to handle: csharp, razor, powershell, bash
const WEB_BUNDLE_LANGUAGES = [
    'html',
    'css',
    'javascript',
    'typescript',
    'json',
    'markdown',
    'yaml',
    'xml'
];

// Theme mapping
const THEMES = {
    light: 'github-light',
    dark: 'github-dark'
};

/**
 * Initialize the Shiki highlighter with retry logic
 */
async function initHighlighter() {
    if (highlighter) return highlighter;
    if (highlighterPromise) return highlighterPromise;

    highlighterPromise = (async () => {
        while (initializationAttempts < MAX_INIT_ATTEMPTS) {
            try {
                initializationAttempts++;
                console.log(`[Shiki] Initializing highlighter (attempt ${initializationAttempts})...`);

                // Dynamic import of Shiki from CDN - use web bundle (limited languages)
                const shiki = await import('https://esm.sh/shiki@1.22.0/bundle/web');

                // Only load languages that are in the web bundle
                highlighter = await shiki.createHighlighter({
                    themes: [THEMES.light, THEMES.dark],
                    langs: WEB_BUNDLE_LANGUAGES
                });

                console.log('[Shiki] Highlighter initialized successfully with web bundle languages');
                return highlighter;
            } catch (error) {
                console.error(`[Shiki] Initialization attempt ${initializationAttempts} failed:`, error);
                if (initializationAttempts >= MAX_INIT_ATTEMPTS) {
                    throw error;
                }
                // Wait before retry
                await new Promise(resolve => setTimeout(resolve, 500));
            }
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

        // Handle common aliases and map to web bundle languages
        // The web bundle only has: html, css, javascript, typescript, json, markdown, yaml, xml
        if (lang === 'js') lang = 'javascript';
        if (lang === 'ts') lang = 'typescript';
        if (lang === 'md') lang = 'markdown';

        // Languages NOT in web bundle - map to closest alternative
        if (lang === 'cs' || lang === 'csharp' || lang === 'c#') lang = 'typescript'; // C# -> TypeScript (similar syntax)
        if (lang === 'razor' || lang === 'cshtml' || lang === 'blazor') lang = 'html'; // Razor -> HTML (best match for markup)
        if (lang === 'sh' || lang === 'shell' || lang === 'bash') lang = 'javascript'; // Shell -> JS (basic highlighting)
        if (lang === 'ps' || lang === 'ps1' || lang === 'powershell') lang = 'javascript'; // PowerShell -> JS

        // Check if language is in web bundle
        if (!WEB_BUNDLE_LANGUAGES.includes(lang)) {
            console.log(`[Shiki] Language '${lang}' not in web bundle, using 'javascript' as fallback`);
            lang = 'javascript'; // Default fallback with decent syntax highlighting
        }

        const themeName = theme === 'dark' ? THEMES.dark : THEMES.light;

        console.log(`[Shiki] Highlighting as '${lang}' with theme '${themeName}'`);

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
 * Wait for highlighter to be ready with timeout
 * @param {number} timeoutMs - Maximum time to wait in milliseconds
 * @returns {boolean} - True if ready, false if timeout
 */
window.waitForHighlighter = async function(timeoutMs = 5000) {
    const startTime = Date.now();

    while (Date.now() - startTime < timeoutMs) {
        try {
            await initHighlighter();
            return true;
        } catch {
            // If init fails, wait a bit and check again
            await new Promise(resolve => setTimeout(resolve, 100));
        }
    }

    return highlighter !== null;
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
