# Vibe.UI.Docs - Known Issues

This file tracks known issues to revisit later.

---

## 1. LivePreview Code Tab Not Displaying Code

**Status**: Open
**Priority**: Medium
**Component**: `Shared/LivePreview.razor`

### Description
When clicking the "Code" tab in the LivePreview component, the code does not display. The Shiki syntax highlighting may be failing silently.

### Symptoms
- Code tab shows blank/empty content
- No visible errors in browser console (need to verify)
- Fallback plain `<pre><code>` should show but doesn't

### Potential Causes
1. Shiki highlighter not initialized properly
2. `highlightCode` JS function returning null/undefined
3. `Code` parameter not being passed correctly from parent
4. Race condition between component render and JS interop

### Files Involved
- `Shared/LivePreview.razor` - Main component
- `wwwroot/js/shiki-interop.js` - Shiki JS interop
- `Pages/Components/ButtonView.razor` - Example usage

### Debugging Steps to Try
1. Add `console.log` in `highlightCode` JS function to verify it's called
2. Check if `Code` property has value in LivePreview
3. Verify fallback `<pre><code>` renders when `highlightedCode` is null
4. Test with simpler static code string instead of dynamic `GeneratedCode`

### Workaround
Use `CodeBlock` component directly for code display (it works correctly).

---

## Notes
- Created: 2025-11-25
- Last Updated: 2025-11-25
