# Vibe.UI

Modern Blazor component library with 93+ components.

## Installation

```bash
dotnet add package Vibe.UI
```

## Setup

### 1. Add CSS to your app

Add the following CSS references to your `_Host.cshtml`, `_Layout.cshtml`, or `index.html`:

```html
<head>
    <!-- Vibe.UI Base Styles (Required) -->
    <link href="_content/Vibe.UI/css/vibe-base.css" rel="stylesheet" />

    <!-- Vibe.UI Component Styles (Required) -->
    <link href="_content/Vibe.UI/Vibe.UI.styles.css" rel="stylesheet" />
</head>
```

Optional: Include utility classes for rapid styling:

```html
<head>
    <!-- All styles (base + utilities) -->
    <link href="_content/Vibe.UI/css/vibe-all.css" rel="stylesheet" />

    <!-- Component styles (still required) -->
    <link href="_content/Vibe.UI/Vibe.UI.styles.css" rel="stylesheet" />
</head>
```

See the [CSS Setup Guide](https://github.com/Dieshen/Vibe.UI/blob/main/docs/CSS-SETUP.md) for detailed configuration options.

### 2. Register services

```csharp
// Program.cs
builder.Services.AddVibeUI();
```

### 3. Use components

```razor
@using Vibe.UI.Components

<Button Variant="ButtonVariant.Primary">Click me</Button>
<Input Label="Name" @bind-Value="name" />
<Card>
  <h3>Card Title</h3>
  <p>Card content</p>
</Card>
```

## Alternative: CLI Approach (Recommended)

For full source code control, use the CLI instead:

```bash
dotnet tool install -g Vibe.UI.CLI
vibe init
vibe add button
```

### CLI Benefits:
- ✅ Own the source code
- ✅ Customize any component
- ✅ No package dependency

### Package Benefits:
- ✅ Quick setup
- ✅ Automatic updates
- ✅ Smaller project size

Choose the approach that fits your needs!

## Features

- 93+ production-ready components
- Built-in theming system (light/dark mode)
- Chart.js integration
- Form validation helpers
- ARIA-compliant accessibility
- Responsive design
- Minimal dependencies

## Documentation

Full documentation: https://github.com/Dieshen/Vibe.UI
