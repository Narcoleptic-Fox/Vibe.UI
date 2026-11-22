# Vibe.UI Theming Guide

## Overview

Vibe.UI supports two approaches for theming: **CLI-based** (via `vibe init`) and **Library-based** (via NuGet package). Both use pure CSS with CSS variables following the shadcn/ui pattern.

---

## Option 1: CLI Approach (Recommended for Copy-Paste Workflow)

### Setup

```bash
# Initialize Vibe.UI with theme selection
vibe init

# Choose your theme when prompted:
# - Tide (default)
# - Dune
# - Ink
```

This generates CSS variables directly in your `app.css`:

```css
@layer vibe {
  :root {
    --vibe-background: hsl(0 0% 100%);
    --vibe-foreground: hsl(222.2 84% 4.9%);
    --vibe-primary: hsl(222.2 47.4% 11.2%);
    /* ...other variables... */
  }

  .dark {
    --vibe-background: hsl(222.2 84% 4.9%);
    --vibe-foreground: hsl(210 40% 98%);
    /* ...dark mode overrides... */
  }
}
```

### Add Components

```bash
vibe add button
vibe add card
```

---

## Option 2: Library Approach (NuGet Package)

### Installation

```bash
dotnet add package Vibe.UI --version 1.0.0-beta
```

### Basic Setup (No Theme Configuration)

```csharp
// Program.cs
builder.Services.AddVibeUI();
```

```html
<!-- index.html or _Layout.cshtml -->
<link href="_content/Vibe.UI/css/vibe-components.css" rel="stylesheet" />
```

This uses the default theme built into Vibe.UI.

---

### Advanced Setup (With Theme Configuration)

Configure the theme in DI registration:

```csharp
// Program.cs
builder.Services.AddVibeUI(options =>
{
    options.BaseColor = "Tide"; // Or: Ink, Dune, Custom
    options.BorderRadius = "0.75rem"; // Optional: customize border radius
});
```

Then add the `<ThemeProvider />` component to your layout:

```razor
<!-- App.razor or MainLayout.razor -->
@using Vibe.UI.Components

<ThemeProvider />

<!-- Your app content here -->
```

#### Custom Colors

For complete control, use custom colors. A custom theme starts from Tide and changes only
the colors you set. Colors that follow another keep following it: text on a surface follows
`Foreground`, the hover fill (`Accent`) follows `Secondary`, and the focus ring and links
follow `Primary`. Surfaces do not follow each other: `Background`, `Card`, `Popover` and
`Muted` are four levels of the page, so set each one you want to change.

```csharp
builder.Services.AddVibeUI(options =>
{
    options.BaseColor = "Custom";
    options.LightColors = new CustomThemeColors
    {
        Background = "hsl(0 0% 100%)",
        Foreground = "hsl(240 10% 3.9%)",
        Primary = "hsl(346 77% 50%)", // Custom red primary
        Secondary = "hsl(240 4.8% 95.9%)",
        Accent = "hsl(240 4.8% 95.9%)",
        Destructive = "hsl(0 84.2% 60.2%)",
        Border = "hsl(240 5.9% 90%)"
    };
    options.DarkColors = new CustomThemeColors
    {
        Background = "hsl(240 10% 3.9%)",
        Foreground = "hsl(0 0% 98%)",
        Primary = "hsl(346 77% 65%)", // Lighter red for dark mode
        Secondary = "hsl(240 3.7% 15.9%)",
        // ...other dark colors...
    };
});
```

---

## Dark Mode

Dark mode works the same way in both approaches using pure CSS:

### Add ThemeToggle Component

```razor
<ThemeToggle />
```

This toggles the `.dark` class on the `<html>` element. All CSS variables automatically switch to their dark variants.

### Manual Dark Mode Control

```csharp
@inject IJSRuntime JS

private async Task ToggleDarkMode()
{
    await JS.InvokeVoidAsync("eval",
        "document.documentElement.classList.toggle('dark')");
}
```

---

## Comparison: CLI vs Library

| Feature | **CLI (`vibe init`)** | **Library (NuGet)** |
|---------|----------------------|---------------------|
| **Setup** | `vibe init` generates CSS | Reference package CSS or use `ThemeProvider` |
| **Theme Selection** | Interactive prompt | Configure in `Program.cs` |
| **Custom Colors** | Edit generated CSS | Configure via `CustomThemeColors` |
| **Components** | Copy files to your project | Use from package |
| **Updates** | Manual (re-run commands) | Automatic (NuGet update) |
| **Flexibility** | Full control over code | Configuration-based |

---

## Built-in Themes

Three themes ship with Vibe.UI. They are its own, not ports of another library's palettes,
and they keep the shadcn/ui token names.

| Theme | Character | Primary |
| --- | --- | --- |
| **Tide** (default) | Cool blue-grey surfaces | Deep teal |
| **Ink** | True neutral grey | Near black in light mode, near white in dark |
| **Dune** | Warm sand surfaces | Muted plum |

Light and dark are built differently on purpose. In light mode the page is a mid grey, cards
and popovers are white, sunken areas are a darker grey with a border, and popovers are lifted
by shadow. In dark mode the four levels are four steps of lightness.

The earlier palettes (Slate, Gray, Zinc, Neutral, Stone, Blue) were removed. A project
configured with one of those names gets an error listing the names that are accepted.

---

## Where the Colors Are Defined

Every built-in color is defined once, in [`themes/`](../themes/README.md). The stylesheet's
color block and the palettes the theme service and the CLI use are generated from those
files, and a test fails if a generated file is edited by hand.

## Role Tokens

These tokens name a job a color does. Every built-in theme sets each of them.

| Token | Job |
| --- | --- |
| `--vibe-sunken-border` | Edge of a sunken well |
| `--vibe-link` | Links and inline actions |
| `--vibe-destructive-text`, `--vibe-success-text`, `--vibe-warning-text`, `--vibe-info-text` | A status color used as text or an icon |
| `--vibe-destructive-soft`, `--vibe-success-soft`, `--vibe-warning-soft`, `--vibe-info-soft` | A quiet status background the matching `-text` stays readable on |
| `--vibe-input` | The edge of a field or control, held to 3:1 against every surface |

If you write your own theme in CSS, set these as well as the surface and brand colors. They
have no fallback: a theme that sets only `--vibe-primary` keeps Tide's link color.

The components use them: status text and icons read `-text`, link-style buttons and the
`Link` component read `--vibe-link`, and bordered wells read `--vibe-sunken-border`.

## Surface Tokens

Each surface token is one level of the page. Components pick the level they sit on, so a
theme can give the levels different colors.

| Level | Token | Used for |
| --- | --- | --- |
| 0 | `--vibe-muted` | Sunken areas: tracks, wells, disabled and read-only fields |
| 1 | `--vibe-background` | The page itself |
| 2 | `--vibe-card` | Cards, panels, fields and buttons that sit on the page |
| 3 | `--vibe-popover` | Anything that floats: menus, toasts, sheets, hover cards |

`--vibe-accent` is the fill for hovered, pressed and highlighted items. A sunken surface
that darkens on hover (a filled input, a scrollbar thumb) still starts from `--vibe-muted`.

## CSS Variables Reference

### Core Colors
```css
--vibe-background       /* Page background */
--vibe-foreground       /* Main text color */
--vibe-primary          /* Primary brand color */
--vibe-secondary        /* Secondary surfaces */
--vibe-muted            /* Muted backgrounds */
--vibe-accent           /* Accent elements */
--vibe-destructive      /* Error/danger color */
```

### Semantic Colors
```css
--vibe-card             /* Card backgrounds */
--vibe-border           /* Border colors */
--vibe-input            /* Input backgrounds */
--vibe-ring             /* Focus ring color */
```

### Component-Specific Foregrounds
```css
--vibe-primary-foreground
--vibe-secondary-foreground
--vibe-muted-foreground
--vibe-accent-foreground
--vibe-destructive-foreground
--vibe-card-foreground
```

---

## Examples

### Example 1: Tide Theme (CLI)

```bash
vibe init
# Select: Tide
```

### Example 2: Custom Brand Colors (Library)

```csharp
builder.Services.AddVibeUI(options =>
{
    options.BaseColor = "Custom";
    options.LightColors = new CustomThemeColors
    {
        Primary = "hsl(262 83% 58%)",    // Purple
        Accent = "hsl(174 72% 56%)",     // Teal
        Destructive = "hsl(351 95% 71%)" // Pink
    };
});
```

### Example 3: Ink with Larger Border Radius

```csharp
builder.Services.AddVibeUI(options =>
{
    options.BaseColor = "Ink";
    options.BorderRadius = "1rem"; // 16px instead of default 8px
});
```

---

## Best Practices

1. **Choose One Approach**: Use either CLI or Library approach, not both
2. **Consistent Border Radius**: Keep border radius consistent across your app
3. **Test Dark Mode**: Always test your theme in both light and dark modes
4. **Accessibility**: Ensure sufficient contrast ratios (use tools like WebAIM)
5. **Custom Colors**: When using custom colors, maintain consistent hue relationships

---

## Troubleshooting

### Theme not applying?

**CLI Approach:**
- Ensure `app.css` is linked in your HTML
- Check that `@layer vibe` block exists in your CSS

**Library Approach:**
- Verify `<ThemeProvider />` is added to your layout
- Ensure `AddVibeUI()` with theme configuration is called in `Program.cs`

### Dark mode not working?

- Check that `<ThemeToggle />` component is present
- Verify `.dark` class is being toggled on `<html>` element (inspect in DevTools)
- Ensure dark mode CSS variables are defined

### Colors look wrong?

- Check HSL values are formatted correctly: `hsl(hue saturation% lightness%)`
- Verify no conflicting CSS is overriding Vibe.UI variables
- Clear browser cache and hard refresh
