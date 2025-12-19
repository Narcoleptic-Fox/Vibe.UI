# Vibe.UI Component Accessibility Documentation

## Overview

This document covers the accessibility patterns implemented across Vibe.UI components for the Alpha 1.0.0 release.

---

## 1. Focus-Visible Patterns

### Standard Focus Ring Pattern
```css
.component:focus-visible {
    outline: 2px solid var(--vibe-ring);
    outline-offset: 2px;
    box-shadow: 0 0 0 2px var(--vibe-background), 0 0 0 4px var(--vibe-ring);
}
```

### Components Supporting Focus-Visible
- **Input** (all variants: text, filled, outlined)
- **TextArea** (updated from :focus to :focus-visible)
- **Button** (all styles)
- **Checkbox** (via hidden input + control pattern)
- **Radio** (via hidden input + control pattern)
- **Switch** (via hidden input + thumb pattern)
- **Menu Items**
- **Drawer close button**
- **Alert Dialog close button**
- **Dialog** (managed via JavaScript focus trap)

---

## 2. Error/Invalid State Patterns

### CSS Implementation
All form inputs support error states through `aria-invalid="true"`:

```css
/* TextArea */
.vibe-textarea[aria-invalid="true"],
.vibe-textarea.vibe-textarea-error {
    border-color: var(--vibe-destructive);
}

/* Input (all variants) */
.vibe-input-wrapper.vibe-input-wrapper-outlined:has(::deep .vibe-input[aria-invalid="true"]) {
    border-color: var(--vibe-destructive);
}

/* Checkbox/Radio */
.vibe-checkbox-input[aria-invalid="true"] + .vibe-checkbox-control {
    border-color: var(--vibe-destructive);
}
```

### Components with Error State Support
| Component | Class-based | aria-invalid |
|-----------|-------------|--------------|
| Input | .vibe-input-has-error | ✓ |
| TextArea | .vibe-textarea-error | ✓ |
| Select | .vibe-select-error | ✓ |
| Checkbox | .vibe-checkbox-error | ✓ |
| Radio | .vibe-radio-error | ✓ |

---

## 3. Disabled State Patterns

### Standard Pattern
```css
.vibe-button:disabled,
.vibe-button[disabled],
.vibe-button[aria-disabled="true"],
.vibe-button.vibe-button-disabled {
    opacity: 0.5;
    cursor: not-allowed;
    pointer-events: none;
}
```

### Close Button Pattern (Drawer/Dialog)
```css
.drawer-close:disabled,
.drawer-close[disabled],
.drawer-close[aria-disabled="true"] {
    opacity: 0.5;
    cursor: not-allowed;
    pointer-events: none;
}
```

---

## 4. ARIA Attributes

### Dialog Components
```html
<div role="dialog"
    aria-modal="true"
    aria-labelledby="dialog-title-id"
    aria-describedby="dialog-body-id">
```

### AlertDialog Components
```html
<div role="alertdialog"
    aria-modal="true"
    aria-labelledby="alert-title-id"
    aria-describedby="alert-description-id">
```

### Input Components
```html
<input
    aria-invalid="true"
    aria-errormessage="error-id"
    aria-describedby="error-id helper-id" />
```

---

## 5. Focus Management (vibe-dialog.js)

### Features
- Focus trap: Keeps keyboard focus within dialog
- Tab/Shift+Tab cycling through focusable elements
- Focus restoration on dialog close
- Body scroll lock while dialog open
- Escape key support

### JavaScript API
```javascript
// Activate focus trap
window.VibeDialog.activate(key, dialogElement);

// Deactivate and restore focus
window.VibeDialog.deactivate(key);
```

---

## 6. CSS Custom Properties Used

```css
--vibe-ring          /* Focus ring color */
--vibe-destructive   /* Error/invalid color */
--vibe-muted         /* Disabled background */
--vibe-background    /* For focus ring contrast */
--vibe-foreground    /* Text color */
```

---

## 7. Component Accessibility Summary

| Component | Focus-Visible | aria-invalid | Disabled | ARIA Roles |
|-----------|---------------|--------------|----------|------------|
| Input | ✓ | ✓ | ✓ | aria-describedby, aria-errormessage |
| TextArea | ✓ | ✓ | ✓ | aria-describedby |
| Button | ✓ | - | ✓ | aria-disabled on links |
| Checkbox | ✓ | ✓ | ✓ | - |
| Radio | ✓ | ✓ | ✓ | - |
| Select | ✓ | ✓ | ✓ | - |
| Dialog | ✓ | - | - | role=dialog, aria-modal |
| AlertDialog | ✓ | - | - | role=alertdialog, aria-modal |
| Drawer | ✓ | - | ✓ | - |

---

## 8. Future Improvements

1. Add `role="tooltip"` to Tooltip component
2. Implement aria-live regions for dynamic content
3. Add skip links for keyboard users
4. Implement roving tabindex for list-based components
5. WCAG 2.1 AA compliance testing
