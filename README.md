# Vibe.UI

[![build](https://img.shields.io/github/actions/workflow/status/Narcoleptic-Fox/Vibe.UI/ci.yml)](https://github.com/Narcoleptic-Fox/Vibe.UI/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/Vibe.UI.svg)](https://www.nuget.org/packages/Vibe.UI/)
[![codecov](https://codecov.io/gh/Narcoleptic-Fox/Vibe.UI/branch/main/graph/badge.svg)](https://codecov.io/gh/Narcoleptic-Fox/Vibe.UI)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

A Blazor component library in the style of [shadcn/ui](https://ui.shadcn.com/): Razor components
you can copy into your project and own, a Tailwind-style utility CSS generator that needs no
Node.js, and a CLI that ties the two together.

> **Status: pre-release, and the API is changing.**
>
> - `main` is an unpublished `1.0.0-beta` that targets .NET 10.
> - NuGet currently has `1.0.0-alpha1`, which targets .NET 9 and is older than everything
>   documented here.
> - The data table and form inputs are being redesigned around typed, expression-based APIs.
>   Expect breaking changes there before a beta is published.
>
> To use what this README describes, build from source as shown below.

## What is in the box

| Package | What it is |
| --- | --- |
| `Vibe.UI` | The component library: inputs, forms, overlays, navigation, data display, feedback, theming |
| `Vibe.UI.CSS` | Build-time utility CSS generator. Scans your Razor and C# for class names and emits only what you use |
| `Vibe.UI.CLI` | The `vibe` tool. Copies component source into your project and runs the CSS generator |

Browse the components in the [live catalog](https://narcoleptic-fox.github.io/Vibe.UI/), run
`vibe list`, or read the source under [`src/Vibe.UI/Components`](src/Vibe.UI/Components).

## Try it from source

You need the .NET 10 SDK.

### Option 1: copy components into your project (recommended)

This is the shadcn workflow. You get the source, you own it, and there is no package dependency.

```bash
git clone https://github.com/Narcoleptic-Fox/Vibe.UI.git
cd Vibe.UI
dotnet pack src/Vibe.UI.CLI -c Release -o ./artifacts/nupkg
dotnet tool install -g Vibe.UI.CLI --add-source ./artifacts/nupkg --version 1.0.0-beta
```

Then, in your Blazor app:

```bash
vibe init
vibe add button
vibe add card
```

Infrastructure goes in `Vibe/` and components in `Components/vibe/` by default. See the
[CLI guide](docs/CLI.md).

### Option 2: reference the library

Add a project reference to `src/Vibe.UI/Vibe.UI.csproj`, then load the styles in your root document
(`Components/App.razor`, `index.html`, or the equivalent for your hosting model):

```html
<link href="_content/Vibe.UI/css/vibe-base.css" rel="stylesheet" />
<link href="_content/Vibe.UI/Vibe.UI.bundle.scp.css" rel="stylesheet" />
```

Register the services in every process that renders components, server and client:

```csharp
using Vibe.UI;

builder.Services.AddVibeUI();
```

[CSS setup](docs/CSS-SETUP.md) and [hosting compatibility](docs/Compatibility.md) cover standalone
WebAssembly and each Blazor Web App render mode.

## Using components

```razor
@using Vibe.UI.Components

<Card>
    <CardTitle>Hello Vibe.UI</CardTitle>
    <CardDescription>Styled by design tokens and scoped CSS.</CardDescription>
    <ChildContent>
        <p>Card content goes here.</p>
    </ChildContent>
    <Footer>
        <Button Variant="ButtonVariant.Primary">Click me</Button>
    </Footer>
</Card>

<ThemeToggle />
```

`ThemeToggle` follows the system preference on first load, remembers the user's choice, and
switches by toggling the `.dark` class on `<html>`.

## Theming

Themes are CSS variables, the same approach shadcn/ui uses. Light values live on `:root`, dark
values under `.dark`:

```css
:root {
  --vibe-background: #ffffff;
  --vibe-foreground: #111111;
  --vibe-primary: #0066cc;
}

.dark {
  --vibe-background: #0f172a;
  --vibe-foreground: #f8fafc;
  --vibe-primary: #3b82f6;
}
```

`vibe init` asks for a base color and writes the matching variables. The full token list and the
runtime theme API are in [Theming](docs/THEMING.md).

## CLI

| Command | What it does |
| --- | --- |
| `vibe init` | Set up Vibe.UI in a project: infrastructure, base CSS, color scheme |
| `vibe add <component>` | Copy a component and its dependencies into your project |
| `vibe list` | List the available components |
| `vibe update [component]` | Update installed components |
| `vibe css` | Generate utility CSS with the Vibe.UI.CSS engine |

Common options: `-y, --yes` skips prompts, `-p, --path <path>` sets the project directory. Run
`vibe <command> --help` for the rest.

## Documentation

- [Documentation index](docs/README.md)
- [CLI](docs/CLI.md) and [CSS setup](docs/CSS-SETUP.md)
- [Vibe.UI.CSS](docs/VIBE-UI-CSS.md) and its [supported utility profile](docs/Vibe.UI.CSS.CoreProfile.md)
- [Component behavior profile](docs/Vibe.UI.ComponentBetaProfile.md)
- [Hosting compatibility](docs/Compatibility.md)
- [Theming](docs/THEMING.md), [Charts](docs/CHARTS.md), [Accessibility](docs/ACCESSIBILITY.md)

## Known limitations

- **Accessibility.** Automated axe, keyboard, focus and ARIA checks run in CI. Formal WCAG
  conformance still needs manual screen-reader, 200% zoom and forced-colors review.
- **Visual coverage.** Reviewed screenshot baselines exist for the highest-risk surfaces on
  Chromium and Windows only. Every component has tests, but not at equal depth.
- **Vibe.UI.CSS scope.** It guarantees the documented
  [core profile](docs/Vibe.UI.CSS.CoreProfile.md), not the full Tailwind compiler or its plugins.
  Class names must appear whole in scanned source; names built by string concatenation at runtime
  are not detected.
- **QR code.** The `QRCode` component is an experimental visual preview, not a scannable code.

## Building and testing

```bash
dotnet build Vibe.slnx
dotnet test tests/Vibe.UI.Tests
dotnet test tests/Vibe.UI.CSS.Tests
dotnet test tests/Vibe.UI.CLI.Tests
```

The browser suites in `tests/Vibe.UI.Docs.E2E` need Playwright browsers installed; see
[Visual regression](docs/VISUAL-REGRESSION.md).

```
src/      Vibe.UI, Vibe.UI.CSS, Vibe.UI.CLI
tests/    unit suites for each, plus Vibe.UI.Docs.E2E (browser)
samples/  Vibe.UI.Docs (the catalog site) and hosting compatibility fixtures
docs/     guides and reference
```

## Contributing and support

- Read the [contributing guide](.github/CONTRIBUTING.md) and
  [code of conduct](.github/CODE_OF_CONDUCT.md) first.
- Report bugs and ask questions in
  [GitHub Issues](https://github.com/Narcoleptic-Fox/Vibe.UI/issues).
- Report vulnerabilities through the [security policy](.github/SECURITY.md), not a public issue.

If Vibe.UI is useful to you: [Buy Me a Coffee](https://buymeacoffee.com/dieshen) or
[GitHub Sponsors](https://github.com/sponsors/Dieshen).

## License

MIT. See [LICENSE](LICENSE). Inspired by [shadcn/ui](https://ui.shadcn.com/).
