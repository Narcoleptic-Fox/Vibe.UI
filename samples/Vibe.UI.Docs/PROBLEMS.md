# Vibe.UI.Docs - Known Issues

This file tracks known issues to revisit later.

---

## 1. LivePreview Code Tab Not Displaying Code

**Status**: RESOLVED - no longer reproduces (checked 2026-11-20)
**Component**: `Shared/LivePreview.razor`

### Description
Clicking the "Code" tab in the LivePreview component used to show an empty panel.

### Resolution
The Code tab now renders through the same `CodeBlock` component the Examples sections use.
Checked on `/components/button` on 2026-11-20: the panel shows the generated markup
(`<Button>Click me</Button>`) with the language label. `ShikiInteropTests` covers the
highlighting path.

### History (2025-11-26)
- Shiki web bundle doesn't include Razor/C# languages
- Mapped razor → html, csharp → typescript as fallbacks
- The panel stayed empty even though highlighting returned markup

---

## 2. Carousel throws when its page is left

**Status**: OPEN - library bug, not a docs bug
**Component**: `src/Vibe.UI/Components/Disclosure/Carousel.razor`

### Description
Navigating away from `/components/carousel` can log an unhandled
`NullReferenceException` from `Carousel.DisposeAsync()`. The method awaits the auto-play
task and then disposes `_autoPlayCancellationTokenSource`, which can already be null by
the time the await returns.

### Observed (2026-11-20)
Seen once while clicking through every page from the navigation rail, about 350 ms per page.

---

## Notes
- Created: 2025-11-25
- Last Updated: 2026-11-20
