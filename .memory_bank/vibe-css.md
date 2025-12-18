# Vibe.CSS Framework Documentation

## Overview

Vibe.CSS is a JIT CSS utility framework for .NET. No Node.js required.

---

## 1. Pipeline Architecture

### Scanner → Generator → Emitter

1. **ClassScanner**: Scans .razor, .cshtml, .html for CSS classes
2. **UtilityGenerator**: Converts class names to CSS rules
3. **CssEmitter**: Outputs final CSS with deduplication

---

## 2. Supported Patterns

### HTML/Razor
```html
class="vibe-flex vibe-gap-4"
Class="vibe-text-primary"
@class="vibe-hidden"
class="@(condition ? "vibe-block" : "vibe-hidden")"
```

### C# Code
```csharp
CssClass = "vibe-p-4 vibe-bg-muted"
AdditionalClasses = "vibe-rounded"
```

---

## 3. Utility Categories

| Category | Examples |
|----------|----------|
| Display | flex, grid, block, hidden |
| Flexbox | flex-row, items-center, justify-between, gap-4 |
| Grid | grid-cols-3, col-span-2 |
| Spacing | p-4, m-2, gap-4 |
| Sizing | w-full, h-screen, max-w-md |
| Typography | text-xl, font-bold, text-center |
| Colors | bg-primary, text-muted, border-destructive |
| Borders | border, rounded-lg, ring-2 |
| Effects | shadow-md, opacity-50 |
| Layout | absolute, z-10, overflow-hidden |

---

## 4. Variants

### State Variants
- `hover:`, `focus:`, `active:`, `disabled:`
- `focus-visible:`, `focus-within:`
- `first:`, `last:`, `odd:`, `even:`

### Group Variants
- `group-hover:`, `group-focus:`, `group-focus-within:`

### Responsive
- `sm:` (640px), `md:` (768px), `lg:` (1024px)
- `xl:` (1280px), `2xl:` (1536px)

### Dark Mode
- `dark:` (applies .dark selector prefix)

### Chaining
```
dark:sm:hover:vibe-bg-primary
→ @media (min-width: 640px) { .dark .vibe-bg-primary:hover { ... } }
```

---

## 5. Color System

### Semantic Colors
- primary, secondary, muted, accent, destructive
- success, warning, info
- foreground variants for each

### Tailwind Palette (18 colors × 11 shades)
- slate, gray, zinc, neutral, stone
- red, orange, amber, yellow
- lime, green, emerald, teal, cyan, sky
- blue, indigo, violet, purple, fuchsia, pink, rose

### Opacity Syntax
```css
vibe-bg-red-500/50 → background-color: rgb(239 68 68 / 0.5)
```

---

## 6. MSBuild Integration

### Properties
```xml
<VibeCssEnabled>true</VibeCssEnabled>
<VibeCssOutput>wwwroot/css/vibe.css</VibeCssOutput>
<VibeCssPrefix>vibe</VibeCssPrefix>
<VibeCssIncludeBase>true</VibeCssIncludeBase>
```

### Auto-execution
- Runs on `BeforeBuild`
- Generates CSS at build time
- Cleaned on `Clean`

---

## 7. CLI Commands

```bash
# Generate CSS
vibe-css generate . -o wwwroot/css/vibe.css

# Scan classes
vibe-css scan . --prefix vibe

# Run tests
vibe-css test
```

---

## 8. CSS Variables (vibe-base.css)

### Colors
```css
--vibe-background, --vibe-foreground
--vibe-primary, --vibe-primary-foreground
--vibe-muted, --vibe-muted-foreground
--vibe-destructive, --vibe-destructive-foreground
--vibe-border, --vibe-ring
```

### Border Radius
```css
--vibe-radius: 0.5rem
--vibe-radius-sm: 0.3rem
--vibe-radius-lg: 0.75rem
```

### Transitions
```css
--vibe-transition: all 0.2s ease
--vibe-transition-fast: all 0.15s ease
```

---

## 9. Arbitrary Values

```css
vibe-w-[500px]    → width: 500px
vibe-p-[1.5rem]   → padding: 1.5rem
vibe-bg-[#ff0000] → background-color: #ff0000
```

---

## 10. Dark Mode

### Light Theme (`:root`)
- Background: white
- Foreground: dark gray
- Primary: dark

### Dark Theme (`.dark`)
- Background: dark gray
- Foreground: off-white
- Primary: white

Toggle via `<html class="dark">`.
