# Vibe.UI.CLI Architecture Documentation

## Overview

Vibe.UI.CLI is a dotnet global tool for managing Vibe.UI components using a shadcn/ui-inspired approach. Components are copied directly into projects for full ownership.

---

## 1. Command Structure

| Command | Purpose |
|---------|---------|
| `vibe init` | Initialize Vibe.UI in a project |
| `vibe add <component>` | Add a component to the project |
| `vibe list` | List all available components |
| `vibe update [component]` | Update components |
| `vibe css` | Generate CSS using Vibe.CSS JIT |

---

## 2. InitCommand

### Options
- `-y, --yes`: Skip confirmation prompts
- `-p, --path`: Project directory
- `--minimal`: Essential infrastructure only
- `--no-theme`: Skip theme system
- `--with-charts`: Include Chart.js support
- `--with-css`: Add Vibe.CSS package reference

### Creates
1. `vibe.json` - Configuration file
2. `Vibe/` - Infrastructure (Base, Services, Enums)
3. `wwwroot/css/` - vibe-base.css, vibe-utilities.css
4. `wwwroot/js/` - vibe-theme.js, vibe-dom.js, etc.

### Color Schemes
Supports: Slate, Gray, Zinc, Neutral, Stone, Blue

---

## 3. AddCommand

### Options
- `-y, --yes`: Skip confirmation
- `-o, --overwrite`: Overwrite existing files
- `-n, --name`: Custom component name
- `--output`: Custom output directory

### Features
- Validates infrastructure exists
- Resolves and installs dependencies first
- Tracks file hashes for change detection
- Creates backups before overwriting modified files

---

## 4. vibe.json Configuration

```json
{
  "projectType": "Blazor",
  "theme": "both",
  "componentsDirectory": "Components/vibe",
  "cssVariables": true,
  "tailwind": null,
  "aliases": {}
}
```

---

## 5. Template Packaging

Templates are bundled via MSBuild Pack:

```xml
<None Include="../Vibe.UI/Components/Inputs/*.razor"
      Pack="true"
      PackagePath="Templates/Components/Inputs/" />
```

### Package Structure
```
Vibe.UI.CLI.nupkg
├── Tools/net9.0/any/
│   └── CLI executable
└── Templates/
    ├── Components/ (by category)
    ├── Infrastructure/
    └── wwwroot/
```

---

## 6. Component Categories

| Category | Count |
|----------|-------|
| Inputs | 23 |
| Form | 7 |
| DataDisplay | 8 |
| Layout | 12 |
| Navigation | 14 |
| Overlay | 17 |
| Feedback | 9 |
| DateTime | 3 |
| Utility | 6 |
| Advanced | 4 |
| Disclosure | 5 |
| Theme | 2 |
| **TOTAL** | **150** |

---

## 7. File Change Detection

- SHA256 hashing via `.vibe/checksums.json`
- Timestamped backups in `.vibe/backups/`
- Warns users about modifications before overwrite

---

## 8. CssCommand

### Options
- `-o, --output`: Output CSS file
- `--with-base`: Include vibe-base.css
- `-w, --watch`: Watch mode
- `-v, --verbose`: Detailed output
- `--scan-only`: Analyze without generating

### Watch Mode
- 500ms debounce
- FileSystemWatcher for changes
- Ctrl+C to stop

---

## 9. Design Decisions

1. **Flat directory structure** - Components in `Components/vibe/` (shadcn/ui pattern)
2. **Copy-first philosophy** - Source code ownership, not package dependencies
3. **SHA256 file hashing** - Track modifications, create backups
4. **Color scheme overrides** - Append CSS variables to vibe-base.css

---

## 10. Path Resolution

The CLI tries multiple paths to find templates:
1. Development: `../../../src/Vibe.UI/Components/`
2. Packaged: `Templates/Components/`
3. Global tool: `../../Templates/Components/`
