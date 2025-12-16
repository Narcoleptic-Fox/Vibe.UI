## Alpha 0.1.0 Readiness Checklist

### Docs site (`samples/Vibe.UI.Docs`)
- [x] `dotnet build samples/Vibe.UI.Docs/Vibe.UI.Docs.csproj -c Release` succeeds and regenerates `samples/Vibe.UI.Docs/wwwroot/css/vibe.css`
- [x] No Tailwind import/usage (no CDN, no `@tailwind`, no Tailwind build pipeline)
- [x] No legacy Bootstrap template leftovers shipped/linked (NavMenu/Bootstrap assets removed or not referenced)
- [x] Top-nav UX feels consistent (Docs + Components + search + theme toggle); no sidebar dependency
- [x] “Getting Started”, “Installation”, “CLI”, “Theming” pages match the component pages’ look and use `vibe-*` utilities
- [x] Command palette hotkey works (`Ctrl/Cmd+K`) and search results route correctly
- [x] Light + dark mode visually coherent (background, overlays, code blocks, links)

### Vibe.CSS (`src/Vibe.CSS`)
- [x] Docs generation works via MSBuild hook in `samples/Vibe.UI.Docs/Vibe.UI.Docs.csproj`
- [x] Variants work in docs patterns (`dark:*`, `hover:*`, `group-hover:*`, responsive variants)
- [x] Core “shadcn docs” utilities covered (spacing/layout/typography/borders/rings/gradients/transforms)
- [x] `vibe-prose` + `dark:vibe-prose-invert` is usable for long-form docs pages
- [x] Scanner handles Razor class expressions without noisy false-positives
- [x] Docs run prefix-only utilities (`VibeCssAllowUnprefixed=false`) and do not rely on unprefixed Tailwind-like class names

### Vibe.UI (`src/Vibe.UI`)
- [ ] Flagship components used in docs feel “shadcn quality”
  - [x] Button: focus-visible ring only; anchor disabled state (`aria-disabled`/`.vibe-button-disabled`)
  - [x] Input: wrapper disabled class; `aria-invalid`/`aria-describedby`/`aria-errormessage`; outlined focus ring
  - [x] Tabs: roving tabindex + keyboard navigation; `aria-controls`/`aria-labelledby`
  - [x] Tooltip: keyboard focus + Escape dismiss; `role="tooltip"` + stable `id`
  - [x] Popover: uses theme tokens; alignment uses `translate` (no transform/animation clash)
  - [x] Toast: dark mode uses `.dark` (no `prefers-color-scheme`)
  - [x] Dialog: focus trap + focus restore; `aria-labelledby`/`aria-describedby` wiring (root + composed title/description)
  - [x] Command: ARIA combobox/listbox semantics; keyboard navigation + disabled handling
- [ ] Focus rings / disabled / error states are consistent across the full flagship set
- [ ] Dark mode support works across the full flagship set (tokens + component CSS)
- [ ] Basic a11y sanity across the full flagship set (keyboard focus, Escape to close overlays, ARIA labeling)

### CLI (`src/Vibe.UI.CLI`)
- [ ] `vibe init` produces a working baseline (theme toggle, tokens, required JS/CSS)
- [ ] `vibe add <component>` installs dependencies correctly and produces compilable output
- [ ] Config file naming and docs are consistent (`vibe.json`)
- [ ] “Getting Started” + “Installation” docs match the current CLI behavior/options

### Release hygiene
- [ ] Version numbers set for alpha (`0.1.0`) where relevant
- [ ] Clear “alpha” messaging in README / docs (dogfooding expectations)
- [ ] Known limitations documented (utility gaps, missing variants, component rough edges)
