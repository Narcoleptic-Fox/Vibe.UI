# Vibe.UI Hosting Compatibility

This repo keeps two small compatibility fixtures under `samples/` to prove Vibe.UI works in the two hosting shapes consumers are likely to use.

## Standalone Blazor WebAssembly

Project:

```bash
samples/Vibe.UI.Compatibility.StandaloneClient/Vibe.UI.Compatibility.StandaloneClient.csproj
```

Purpose:

- Verifies a standalone client app can reference the local Vibe.UI project.
- Verifies Vibe.UI static web assets load through `_content/Vibe.UI/...`.
- Verifies `AddVibeUI()` works in a WebAssembly DI container.
- Verifies core interactive components update on the client runtime.

Run locally:

```bash
dotnet run --project samples/Vibe.UI.Compatibility.StandaloneClient/Vibe.UI.Compatibility.StandaloneClient.csproj
```

## Blazor Web App With Server And Client

Server project:

```bash
samples/Vibe.UI.Compatibility.WebApp/Vibe.UI.Compatibility.WebApp/Vibe.UI.Compatibility.WebApp.csproj
```

Client project:

```bash
samples/Vibe.UI.Compatibility.WebApp/Vibe.UI.Compatibility.WebApp.Client/Vibe.UI.Compatibility.WebApp.Client.csproj
```

Purpose:

- Verifies Vibe.UI renders during static SSR.
- Verifies Vibe.UI works in `InteractiveServer`.
- Verifies Vibe.UI works in `InteractiveWebAssembly` from the `.Client` project.
- Verifies Vibe.UI works in `InteractiveAuto`, including server prerendering before WebAssembly activation.
- Verifies `AddVibeUI()` is registered in both server and client DI containers.
- In package mode, keeps `Vibe.UI.CSS` ownership on the server project so hosted rebuilds do not emit duplicate `wwwroot/css/Vibe.UI.CSS` static assets from both server and client.

When `Vibe.UI.CSS` is referenced by both hosted projects, the server project scans the shared sample root and owns the generated `wwwroot/css/Vibe.UI.CSS`. The `.Client` project keeps its `StaticWebAssetProjectMode=Default` role and does not publish its own copy of that generated CSS file.

The CLI follows the same topology for `vibe init --with-css`: source components remain in the recommended client project, while the `Vibe.UI.CSS` package and shared scan-root configuration are written to the server project.

Run locally:

```bash
dotnet run --project samples/Vibe.UI.Compatibility.WebApp/Vibe.UI.Compatibility.WebApp/Vibe.UI.Compatibility.WebApp.csproj
```

Routes:

- `/` - static SSR smoke page
- `/server` - Interactive Server smoke page
- `/client` - Interactive WebAssembly smoke page
- `/auto` - Interactive Auto smoke page
- `/boundary/inside/server`, `/boundary/inside/client`, `/boundary/inside/auto` - a static page hosting one interactive component that holds a `Form` and its `PropertyInput` fields
- `/boundary/field-across/server`, `/boundary/field-across/client`, `/boundary/field-across/auto` - broken on purpose: a static page hands `For` to an interactive `PropertyInput`
- `/boundary/form-across` - broken on purpose: a static page hands child content to an interactive `Form`
- `/boundary/none` - a `Form` with no render mode anywhere
- `/grid/inside/server`, `/grid/inside/client`, `/grid/inside/auto` - a static page hosting one interactive component that holds a `VibeGrid`, its provider and its columns
- `/grid/provider-across` - broken on purpose: a static page hands a provider to an interactive `VibeGrid`
- `/grid/two/keyed`, `/grid/two/unkeyed` - two grids in one component, with and without a `@key` each

## The interactive boundary

A `PropertyInput` takes an expression and a `Form` takes child content. Neither can be serialized. Blazor requires that parameters passed from a statically rendered parent to an interactive child are JSON serializable ([render mode propagation](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes#render-mode-propagation)), so a form and its fields must sit inside one component, and that component gets the render mode. A page with its own `@rendermode` is such a component: everything on it is inside the boundary.

The `/boundary` routes pin what happens otherwise, as observed on .NET 10:

| Written on a static page | Result |
|---|---|
| `<ReservationForm @rendermode="..." />`, the form and fields inside it | Works in Interactive Server, WebAssembly and Auto |
| `<PropertyInput For="..." @rendermode="..." />`, any of the three interactive modes | HTTP 500 while prerendering. `NotSupportedException: Serialization and deserialization of 'System.Type' instances is not supported.` followed by `Path: $.ParameterValues.Type.` for Server and Auto, `Path: $.Type.` for WebAssembly. The message names neither `PropertyInput` nor `For` |
| `<Form @rendermode="InteractiveServer">` with fields inside (tried in Interactive Server only) | HTTP 500. ``InvalidOperationException: Cannot pass the parameter 'ChildContent' to component 'Form`1' with rendermode 'InteractiveServerRenderMode'`` |
| `<Form>` with fields, no render mode | HTTP 200. The field is drawn with its label, and typing never reaches the model. No error |

`VibeGrid` meets the same rule: its provider is a delegate and its columns are child content. A provider handed across fails with ``InvalidOperationException: Cannot pass the parameter 'ItemsProvider' to component 'VibeGrid`1' with rendermode 'InteractiveServerRenderMode'``, which does name the parameter. Inside one interactive component it loads, pages and sorts with one request each, in all three render modes. The page the server prerenders is not fetched again: the server's HTML reports one request and the interactive component reports none. Two grids in one component each start with their own page only when each has a `@key`; without keys each fetches again, with its own rows.

All three compile with no warning, so nothing stops them before the page is requested. Do not repair the two broken pages: the tests assert that they fail.

## A 404 in Safari's console on pages below the root

Found 2026-11-27, on .NET 10. It is not a missing file and nothing on the page is broken.

- The app's bundled stylesheet (`<App>.styles.css`) imports the library's scoped bundle, and the server sends a matching header on it: `Link: <_content/Vibe.UI/Vibe.UI.<hash>.bundle.scp.css>; rel="preload"; as="style"`. The address is relative.
- WebKit resolves that address against the page, not the stylesheet. On `/boundary/none` it asks for `/boundary/_content/Vibe.UI/Vibe.UI.<hash>.bundle.scp.css`, gets a 404 or drops the request, and then loads the right file from the `@import`.
- Chromium and Firefox do not make the extra request. A page at the root does not show it in any browser, which is why the smoke pages never did.

The header comes from the .NET SDK's static web assets, not from Vibe.UI, so any Blazor Web App with scoped CSS from a component library will show it. The browser tests ignore exactly that address and report every other error response by its URL.

## CI Gate

The CI workflow restores and builds both fixtures in Release configuration, installs the Playwright browser runtime, and runs the `Category=Compatibility` browser smoke tests in Chromium, Firefox, and WebKit.

The fixtures use project references by default for normal development. The local package validation script flips them to package references with `VibeUsePackageReferences=true`, restores from the packed `.nupkg` files, and builds both hosting shapes before a package can be published.

The hosted Web App package-mode validation deliberately builds the server project twice back to back. That guards the Windows/.NET SDK `10.0.301` duplicate-static-web-asset regression where both hosted projects used to claim the same generated `wwwroot/css/Vibe.UI.CSS`.

Set `BROWSER=chromium`, `BROWSER=firefox`, or `BROWSER=webkit` to run a specific browser locally. In PowerShell, use `$env:BROWSER = "chromium"` before invoking `dotnet test`. Set `VIBE_STANDALONE_BASE_URL` or `VIBE_WEBAPP_BASE_URL` when the target app is already running and should not be started by the test harness.

The browser tests verify:

- Standalone Blazor WebAssembly client-side interactivity.
- Blazor Web App static SSR rendering.
- Blazor Web App `InteractiveServer` interactivity.
- Blazor Web App `InteractiveWebAssembly` interactivity from the `.Client` project.
- Blazor Web App `InteractiveAuto` prerender and activation behavior.
- The interactive boundary: a form inside one interactive component works in each render mode, and the three ways of getting it wrong fail as described above.
- A `VibeGrid` inside one interactive component: one request while the server prerenders and none when it becomes interactive, then one per page change and one per sort, in each render mode.
- Two grids in one component, keyed and unkeyed.
