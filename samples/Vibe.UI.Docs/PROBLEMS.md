# Vibe.UI.Docs - Known Issues

This file tracks known issues to revisit later.

---

## 1. LivePreview Code Tab Not Displaying Code

**Status**: DEFERRED - Low Priority
**Priority**: Low
**Component**: `Shared/LivePreview.razor`

### Description
When clicking the "Code" tab in the LivePreview component, the code panel appears empty. The CodeBlock component (used in Examples sections) works fine with Shiki syntax highlighting.

### Investigation Notes (2025-11-26)
- Shiki web bundle doesn't include Razor/C# languages
- Mapped razor → html, csharp → typescript as fallbacks
- Console logs show highlighting IS working (346 chars returned)
- StateHasChanged IS being called
- But UI doesn't update - suspected Blazor WASM rendering issue
- Template changes weren't being applied even after clean build

### Current State
- Component simplified to just display `@Code` directly (no Shiki)
- Still not working - deeper Blazor rendering issue
- **Workaround**: Use CodeBlock component in Examples section (works fine)

### Next Steps (When Revisiting)
1. Check if `Code` parameter is actually being passed (might be empty)
2. Compare LivePreview render lifecycle with CodeBlock
3. Consider using `@key` directive to force re-render
4. Test with a completely new simple component

### Files Modified
- `wwwroot/js/shiki-interop.js` - Language mappings for web bundle
- `Shared/LivePreview.razor` - Simplified to remove Shiki dependency

---

## Notes
- Created: 2025-11-25
- Last Updated: 2025-11-26
- Status: Deferred for later investigation
