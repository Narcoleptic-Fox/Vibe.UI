/**
 * Type declarations for Shiki CDN import
 */

declare module 'https://esm.sh/shiki@1.22.0/bundle/web' {
  export interface ShikiHighlighter {
    codeToHtml(code: string, options: { lang: string; theme: string }): string;
  }

  export function createHighlighter(options: {
    themes: string[];
    langs: string[];
  }): Promise<ShikiHighlighter>;
}
