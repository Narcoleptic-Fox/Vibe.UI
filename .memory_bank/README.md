# Vibe.UI Memory Bank

This directory contains persistent documentation for the Vibe.UI project to help AI assistants maintain context across sessions.

## Contents

| File | Description |
|------|-------------|
| `accessibility.md` | Component accessibility patterns (focus, error, disabled states) |
| `cli-architecture.md` | CLI structure, commands, and template packaging |
| `vibe-css.md` | CSS utility framework architecture and usage |
| `alpha-release-summary.md` | Alpha 1.0.0 release status and changes |

## Quick Reference

### Version: 1.0.0-alpha

### Key Patterns

**Focus Ring:**
```css
.component:focus-visible {
    outline: 2px solid var(--vibe-ring);
    outline-offset: 2px;
}
```

**Error State:**
```css
.component[aria-invalid="true"] {
    border-color: var(--vibe-destructive);
}
```

**Disabled State:**
```css
.component:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}
```

### CLI Commands
```bash
vibe init          # Initialize project
vibe add button    # Add component
vibe list          # List components
vibe css --watch   # Generate CSS
```

### CSS Prefix
All utilities use `vibe-` prefix: `vibe-flex`, `vibe-p-4`, `vibe-bg-primary`

---

Last updated: December 16, 2025
