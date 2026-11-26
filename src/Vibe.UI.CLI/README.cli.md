# Vibe.UI CLI

CLI tool for adding Vibe.UI components to your Blazor projects.

Requires the .NET 10 SDK.

## Installation

```bash
dotnet tool install -g Vibe.UI.CLI --version 1.0.0-beta
```

## Quick Start

```bash
# 1. Initialize Vibe.UI infrastructure
cd MyBlazorApp
vibe init

# This creates:
# - Vibe/ folder with base classes and services
# - wwwroot/js/vibe-theme.js
# - wwwroot/css/vibe-base.css and vibe-utilities.css

# 2. Add to Program.cs:
builder.Services.AddVibeUI();

# 3. Add components
vibe add button
vibe add input
vibe add card

# Components are now in Components/!
```

## Blazor Hosting Models

`vibe init` detects common Blazor project shapes:

- Standalone Blazor WebAssembly projects install into the current project and use direct `wwwroot/index.html` stylesheet links.
- Blazor Web Apps with a server project and `.Client` project prompt for the install target. With `--yes`, the CLI installs into the `.Client` project because Interactive WebAssembly and Interactive Auto components must be bundled by the client project.
- Blazor Web Apps should register Vibe.UI in both projects when components run in both render modes:

```csharp
// Server Program.cs
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddVibeUI();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MyApp.Client._Imports).Assembly);

// Client Program.cs
builder.Services.AddVibeUI();
```

For .NET 10 Blazor Web Apps, add generated styles in the server root component with `@Assets[...]`:

```razor
<link rel="stylesheet" href="@Assets["css/vibe-base.css"]" />
<link rel="stylesheet" href="@Assets["css/vibe-utilities.css"]" />
```

Standalone Blazor WebAssembly projects use direct links in `wwwroot/index.html`:

```html
<link href="css/vibe-base.css" rel="stylesheet" />
<link href="css/vibe-utilities.css" rel="stylesheet" />
```

## Project Structure

After `vibe init`:
```
MyProject/
├── Vibe/                    ← Infrastructure
│   ├── Base/
│   ├── Services/
│   └── Themes/
├── Components/              ← Components (added via vibe add)
│   ├── Button.razor         ← Flat structure (default)
│   ├── Input.razor
│   └── Card.razor
├── wwwroot/
│   └── js/
│       └── vibe-theme.js
└── Program.cs
```

### Directory Structure Philosophy

**Vibe.UI uses a FLAT directory structure by default**, inspired by shadcn/ui:

✅ **Default (Flat Structure):**
```
Components/
├── Button.razor
├── Input.razor
├── Card.razor
├── Dialog.razor
└── Alert.razor
```

This design provides:
- **Simpler imports**: `@using MyApp.Components.Button` vs `@using MyApp.Components.Input.Button`
- **Easier discovery**: All components visible in one directory
- **Less cognitive load**: No need to remember which category a component belongs to
- **Cleaner autocomplete**: Better IDE experience
- **Scalable**: Works well even with 111 components

**Why not category subdirectories?**

The source code IS organized by category (Input/, Form/, DataDisplay/, etc.) for maintainer convenience. However, components are installed flat for user convenience.

**Want custom organization?** Use the `--output` flag:

```bash
# Custom category structure
vibe add button --output Components/Input
vibe add dialog --output Components/Overlay

# Result:
Components/
├── Input/
│   └── Button.razor
└── Overlay/
    └── Dialog.razor
```

## Features

✅ **Source code control** - All code in your project
✅ **Zero dependencies** - No Vibe.UI package required
✅ **Full customization** - Modify any component
✅ **Custom naming** - `vibe add button --name PrimaryButton`
✅ **Custom location** - `vibe add button --output UI/Components`

## Commands

### `vibe init`
Copies infrastructure (base classes, services, theme system) to `Vibe/` folder.

Options:
- `--minimal` - Core infrastructure only
- `--no-theme` - Skip theme system
- `--with-charts` - Copy the `vibe-chart.js` interop script (the Chart.js library must also be loaded by the app)
- `--with-css` - Add build-time `Vibe.UI.CSS` generation. Hosted Web Apps configure the server as the single CSS owner and scan both server and client sources.
- `--with-fluentvalidation` - Copy the FluentValidation adapter to `Vibe/FluentValidation/`, add the `FluentValidation` package reference to the same project, and add `@using global::Vibe.UI.FluentValidation` to its `_Imports.razor`. This is the vendored form of the `Vibe.UI.FluentValidation` package, for projects that have the components as source and no `Vibe.UI` package. In a hosted Web App it goes into the client, like the components. An existing `FluentValidation` reference is left at the version the app chose. Under central package management (`ManagePackageVersionsCentrally` with a `Directory.Packages.props`) the version goes into `Directory.Packages.props` and the reference has none, as `dotnet add package` does.

#### FluentValidation

```bash
vibe init --with-fluentvalidation
```

Then register the adapter and your validators, in the client and on the server if the app has both:

```csharp
builder.Services.AddVibeFluentValidation();   // namespace Vibe.UI
builder.Services.AddScoped<IValidator<Person>, PersonValidator>();
```

and put `<FluentValidator />` inside an `EditForm`. Labels, required and limits are then read from the validator, and its errors are written into the form's `EditContext`.

To add the adapter to a project that is already initialised, run `vibe update --infrastructure --with-fluentvalidation`.

### `vibe add <component>`
Adds a component to `Components/`.

Options:
- `--name <name>` - Rename component
- `--output <path>` - Custom output directory
- `--overwrite` - Overwrite existing files
- `-y` - Skip confirmation prompt

### `vibe list`
Shows all available components.

### `vibe update <component>`
Updates an existing tracked component after confirmation. Locally modified
tracked files are backed up before replacement.

It does not touch the infrastructure in `Vibe/`. If that was written by an older CLI, or
by one that did not record its version, it says so and names `vibe update --infrastructure`.

### `vibe update --infrastructure`
Refreshes what `vibe init` copied, from this CLI:

- everything in `Vibe/`: base classes, the types components share, metadata, services,
  configuration, enums, `ServiceCollectionExtensions.cs`, and the FluentValidation adapter if
  the project has it
- the interop scripts in `wwwroot/js/`, keeping the chart and theme scripts only if the project has them
- `vibe-utilities.css` and `vibe-preflight.css`

It does not touch components, the components directory in `vibe.json`, or `vibe-base.css`,
which holds the colour you chose. It records the CLI version in `vibe.json`.

Nothing records yet whether you edited a copied file, so it lists every file it will replace or
add and asks before writing; any change you made to a listed file is lost. `--yes` skips the
question. Files the CLI no longer ships are left in place.

Options:
- `--with-fluentvalidation` - Also add the FluentValidation adapter (see `vibe init`)
- `-y|--yes` - Skip the confirmation

### `vibe css [path]`
Scans source files or generates the application utility stylesheet. Use
`--fail-on-unknown` in CI and `--ignore` for explicit non-utility hooks that
share the configured prefix.

## Available Components

**Input:** Button, Checkbox, ColorPicker, FileUpload, Input, InputOTP, MultiSelect, Radio, RadioGroup, Rating, Select, Slider, Switch, TagInput, TextArea, Toggle

**Form:** Form, FormField, Label, ValidatedInput, Combobox

**DataDisplay:** Avatar, Badge, Chart, DataTable, Progress, Table, Timeline

**Layout:** AspectRatio, Card, Separator

**Navigation:** Breadcrumb, Pagination, Tabs

**Overlay:** Dialog, Drawer, Popover, Tooltip

**Feedback:** Alert, Confetti, EmptyState, Skeleton, Spinner, Toast

**Advanced:** KanbanBoard, TreeView, VirtualScroll

**Disclosure:** Accordion, Carousel, Collapsible

**Theme:** ThemeToggle

And more! Run `vibe list` for the complete list.

## Documentation

Full documentation: https://narcoleptic-fox.github.io/Vibe.UI/

## Inspired by shadcn/ui

This CLI follows the shadcn/ui philosophy: copy source code instead of package dependencies.
You own the code and can customize it however you need.
