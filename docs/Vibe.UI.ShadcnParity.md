# Vibe.UI shadcn/ui Parity Roadmap

Goal: make `Vibe.UI` feel like “shadcn/ui for Blazor/.NET”: beautiful defaults, composable primitives, predictable APIs, and a copy‑into‑your‑app workflow that stays ergonomic as the library grows.

This document is a pragmatic checklist (phased) rather than a strict 1:1 port. Some shadcn/ui pieces map to `Vibe.CSS` (utilities) and some map to `Vibe.UI` (components + patterns).

## Current Inventory (Repo Snapshot)

`src/Vibe.UI/Components` currently contains ~`110` Razor components grouped into:
- Layout, Inputs, Form, DataDisplay, Navigation, Overlay, Feedback, Disclosure, DateTime, Utility, Theme, Advanced

Examples present already:
- Core primitives: `Button`, `Card`, `Input`, `Tabs`, `Dialog`, `Popover`, `Tooltip`, `DropdownMenu`, `Toast`
- “Shadcn-ish” patterns: `Command`, `Menubar`, `NavigationMenu`, `AlertDialog`
- Extras beyond shadcn: `Chart`, `KanbanBoard`, `RichTextEditor`, `VirtualScroll`

## Phase 0 — Define “Parity” and Guardrails

- [ ] Decide what “parity” means for Vibe.UI:
  - API feel (props, variants, naming)
  - Visual defaults (spacing, radii, shadows, typography)
  - Composition model (Root/Trigger/Content patterns)
- [ ] Document a component template:
  - required accessibility attributes/roles
  - keyboard interactions
  - theming approach (CSS variables) and class composition rules
- [ ] Lock down default tokens and visual language:
  - border radius scale
  - shadows
  - typography scale
  - neutral palette behavior in light/dark

## Phase 1 — Core shadcn/ui Primitives (Highest adoption)

### Buttons and inputs (polish + completeness)
- [ ] `Button` parity: icon buttons, loading state, as-child/link behavior, focus-ring consistency
- [ ] `Input` parity: `disabled/invalid`, prefix/suffix slots, consistent heights with Select/TextArea
- [ ] `Textarea` parity: resize rules, consistent spacing and error state
- [ ] `Select` parity: keyboard nav, searchable select (if desired), consistent item spacing
- [ ] `Checkbox`/`Radio` parity: hit target sizing, label alignment, indeterminate
- [ ] `Switch` parity: animations, disabled/checked styles

### Overlay primitives (composition model)
- [ ] `Dialog`/`AlertDialog` parity: focus trap, scroll lock, escape handling, aria labeling
- [ ] `Popover` parity: collision handling, focus behavior
- [ ] `Tooltip` parity: delay, hover/focus behavior, portal positioning
- [ ] `DropdownMenu`/`ContextMenu` parity: keyboard nav, submenus, typeahead

### Navigation primitives
- [ ] `Tabs` parity: keyboard nav, disabled tabs, vertical orientation
- [ ] `Breadcrumb` parity: separators, truncation
- [ ] `Pagination` parity: a11y labels + responsive collapse
- [ ] `NavigationMenu` parity: hover intent, focus management

## Phase 2 — shadcn/ui “Patterns” Components (Docs site feel)

These are often what makes a shadcn site feel like a shadcn site.

- [ ] `Command` (command palette) parity: filtering, groups, shortcuts, empty state, accessibility
- [ ] `DataTable` parity: sorting, filtering, pagination, selection (if you want TanStack‑like features)
- [ ] `Toast/Sonner` parity: stacking behavior, swipe to dismiss, variants
- [ ] `Accordion/Collapsible` parity: keyboard behavior and aria
- [ ] `Carousel` parity: touch, snap, buttons, dots
- [ ] `Sheet/Drawer` parity: placement variants, focus/scroll

## Phase 3 — Theming + Styling Model (Vibe.UI ↔ Vibe.CSS contract)

- [ ] Define the contract between Vibe.UI and Vibe.CSS:
  - Vibe.UI uses semantic tokens (`--vibe-*`) and/or `vibe-*` utilities
  - minimal bespoke component CSS where possible
- [ ] Ensure every component has:
  - consistent `dark` support
  - consistent focus rings
  - consistent disabled/readonly states
- [ ] Add a “headless mode” guideline (optional): allow consumers to fully override classes

## Phase 4 — Copy/Paste Workflow (shadcn CLI parity)

shadcn/ui parity is as much workflow as it is visuals.

- [ ] CLI parity:
  - [ ] `vibe add <component>` supports dependencies graph (adds required peers)
  - [ ] `vibe diff` and `vibe update` (optional but huge DX win)
  - [ ] component registry metadata (name, deps, files, docs links)
- [ ] Provide canonical “recipes” (patterns):
  - auth forms, settings pages, dashboards
  - modal + form flows
  - table + filters

## Phase 5 — Quality Bar (What makes it “shadcn quality”)

- [ ] Accessibility conformance tests for key components (keyboard + aria)
- [ ] Visual regression screenshots for docs (light/dark)
- [ ] Performance checks for heavy components (DataTable, VirtualScroll, Chart)
- [ ] API consistency review (naming: `Variant`, `Size`, `Class`, slots)

## Suggested Next Steps

1) Pick 10 “flagship” components to perfect first (the ones used on the docs landing + component pages).
2) Add visual regression screenshots to CI for those pages (home/components/button/input + overlays).
3) Iterate tokens + focus/disabled states until the suite looks consistently “shadcn”.

