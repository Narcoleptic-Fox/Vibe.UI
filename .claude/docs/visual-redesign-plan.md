# Vibe.UI Complete Visual Redesign Plan

**Goal:** Transform Vibe.UI's visual presentation to match shadcn/ui's polished, modern aesthetic across components, documentation site, and styling architecture.

**Timeline:** 2-3 days of focused work
**Impact:** High - Dramatically improves first impressions and competitive positioning

---

## Table of Contents

1. [Architecture Decision: Move to Single CSS File](#1-architecture-decision-move-to-single-css-file)
2. [Component Visual Polish](#2-component-visual-polish)
3. [Documentation Site Redesign](#3-documentation-site-redesign)
4. [Implementation Order](#4-implementation-order)
5. [Detailed File Changes](#5-detailed-file-changes)

---

## 1. Architecture Decision: Move to Single CSS File

### Why Single CSS File?

**Current State:**
- 91 separate `.razor.css` scoped files
- Difficult to maintain consistency
- Harder for users to customize when they copy components
- CSS variables scattered across files

**Proposed State:**
- Single `vibe-components.css` file in `wwwroot/css/`
- All component styles in one place
- Easy to audit for consistency
- Users can easily customize by editing one file
- Better for CDN delivery (single file to cache)

### Benefits

✅ **Easier Customization**
- Users see all styles in one file
- Can use browser dev tools to find and override styles
- No scoped CSS "magic" to fight against

✅ **Better Consistency**
- All spacing, colors, transitions in one place
- Easy to audit for 4px grid alignment
- Can see pattern inconsistencies immediately

✅ **Shadcn/ui Aligned**
- shadcn/ui uses global Tailwind (all styles visible)
- Our approach: single CSS file (same transparency)

✅ **Better DX for CLI Users**
- `vibe add button` copies component + points to CSS file
- Users can tweak styles without hunting through files

### Migration Strategy

1. **Create** `src/Vibe.UI/wwwroot/css/vibe.css`
2. **Consolidate** all `.razor.css` files into sections
3. **Delete** individual `.razor.css` files
4. **Update** component markup to use non-scoped class names
5. **Test** that all components still render correctly

**Estimated Time:** 4-6 hours (tedious but straightforward)

---

## 2. Component Visual Polish

### Overview

Update component CSS to match shadcn/ui's visual standards while keeping Vibe.UI's strengths (opinionated colors, elegant transitions).

### Key Changes

#### A. Border Radius Strategy (CRITICAL)

**Problem:** Everything uses 8px - too uniform, lacks depth

**Solution:** Varied radius creates visual hierarchy

```css
/* NEW CSS VARIABLES */
:root {
  --vibe-radius: 0.625rem;          /* Default: 10px */
  --vibe-radius-sm: 0.375rem;       /* Small (buttons, inputs): 6px */
  --vibe-radius-md: 0.5rem;         /* Medium: 8px */
  --vibe-radius-lg: 0.75rem;        /* Large (cards): 12px */
  --vibe-radius-xl: 1rem;           /* Extra large: 16px */
}
```

**Apply to Components:**
- Buttons: `var(--vibe-radius-sm)` (6px - sharper, more clickable)
- Inputs: `var(--vibe-radius-sm)` (6px - matches buttons)
- Cards: `var(--vibe-radius-lg)` (12px - softer, more friendly)
- Modals: `var(--vibe-radius-lg)` (12px)
- Tooltips: `var(--vibe-radius-md)` (8px - small but visible)

**Impact:** Immediate visual hierarchy, feels more polished

#### B. Spacing Consistency (CRITICAL)

**Problem:** Mixed spacing units, not aligned to 4px/8px grid

**Solution:** Standardize all spacing to 4px increments

```css
/* SPACING SCALE - Use exclusively */
--vibe-spacing-1: 0.25rem;   /* 4px */
--vibe-spacing-2: 0.5rem;    /* 8px */
--vibe-spacing-3: 0.75rem;   /* 12px */
--vibe-spacing-4: 1rem;      /* 16px */
--vibe-spacing-5: 1.25rem;   /* 20px */
--vibe-spacing-6: 1.5rem;    /* 24px */
--vibe-spacing-8: 2rem;      /* 32px */
--vibe-spacing-10: 2.5rem;   /* 40px */
--vibe-spacing-12: 3rem;     /* 48px */
--vibe-spacing-16: 4rem;     /* 64px */
--vibe-spacing-20: 5rem;     /* 80px */
```

**Audit All Components:**
- Button padding: Use spacing-3 (12px) and spacing-4 (16px)
- Card padding: Use spacing-6 (24px)
- Card gap: Use spacing-6 (24px) NOT spacing-4 (16px)
- Input padding: Use spacing-3/spacing-4

**Impact:** Better rhythm, more professional feel

#### C. Shadow Depth (HIGH IMPACT)

**Problem:** Shadows too subtle (0.05 opacity), cards look flat

**Solution:** Layered shadows with better contrast

```css
/* SHADOW SYSTEM */
--vibe-shadow-sm: 0 1px 2px 0 rgb(0 0 0 / 0.05);
--vibe-shadow-md: 0 4px 6px -1px rgb(0 0 0 / 0.1),
                  0 2px 4px -2px rgb(0 0 0 / 0.1);
--vibe-shadow-lg: 0 10px 15px -3px rgb(0 0 0 / 0.1),
                  0 4px 6px -4px rgb(0 0 0 / 0.1);
--vibe-shadow-xl: 0 20px 25px -5px rgb(0 0 0 / 0.1),
                  0 8px 10px -6px rgb(0 0 0 / 0.1);
```

**Apply to Components:**
- Cards: `var(--vibe-shadow-sm)` by default
- Elevated Cards: `var(--vibe-shadow-md)`
- Modals: `var(--vibe-shadow-lg)`
- Dropdowns: `var(--vibe-shadow-md)`

**Impact:** Better depth perception, components feel more "real"

#### D. Typography Consistency (MEDIUM IMPACT)

**Problem:** Button font sizes scale too much (12px → 14px → 16px)

**Solution:** Standardized sizing for professionalism

```css
/* BUTTON FONT SIZES */
.vibe-button-small {
  font-size: 0.875rem;   /* 14px (was 12px) */
  height: 2rem;          /* 32px */
  padding: 0 var(--vibe-spacing-3);  /* 12px */
}

.vibe-button-medium {
  font-size: 0.875rem;   /* 14px (same) */
  height: 2.5rem;        /* 40px */
  padding: 0 var(--vibe-spacing-4);  /* 16px */
}

.vibe-button-large {
  font-size: 0.875rem;   /* 14px (was 16px) */
  height: 3rem;          /* 48px */
  padding: 0 var(--vibe-spacing-8);  /* 32px (was 24px) */
}
```

**Impact:** More professional, matches shadcn/ui's consistency

#### E. Focus States (ACCESSIBILITY)

**Problem:** Input focus rings too subtle (25% opacity)

**Solution:** Increase visibility for better accessibility

```css
/* BEFORE */
.vibe-input:focus {
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--vibe-ring) 25%, transparent);
}

/* AFTER */
.vibe-input:focus {
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--vibe-ring) 50%, transparent);
}
```

**Changes:**
- Increase thickness: 2px → 3px
- Increase opacity: 25% → 50%
- Better WCAG compliance
- More visible for keyboard users

**Impact:** Better accessibility, more obvious focus states

#### F. Card Improvements (HIGH IMPACT)

**Problem:** Cards feel cramped, shadows too light

**Solution:** More breathing room, better shadows

```css
.vibe-card {
  border-radius: var(--vibe-radius-lg);  /* 12px (was 8px) */
  box-shadow: var(--vibe-shadow-sm);      /* Slightly darker */
  padding: var(--vibe-spacing-6);         /* 24px (consistent) */
  border: 1px solid var(--vibe-border);
  background: var(--vibe-card);
}

.vibe-card-content {
  display: flex;
  flex-direction: column;
  gap: var(--vibe-spacing-6);  /* 24px (was 16px) - MORE SPACE */
}

.vibe-card-header {
  display: flex;
  flex-direction: column;
  gap: var(--vibe-spacing-2);  /* 8px between title/description */
  padding-bottom: var(--vibe-spacing-4);  /* 16px bottom margin */
}

.vibe-card-footer {
  display: flex;
  align-items: center;
  padding-top: var(--vibe-spacing-4);  /* 16px top margin */
  border-top: 1px solid var(--vibe-border);
}

/* ELEVATED VARIANT */
.vibe-card-elevated {
  box-shadow: var(--vibe-shadow-md);  /* More prominent */
}
```

**Impact:** Cards feel premium, more like shadcn/ui

#### G. Transition Consistency (KEEP AS-IS)

**Current:** 200ms ease
**shadcn/ui:** 150ms default

**Decision:** KEEP 200ms - it's actually BETTER (more elegant)

```css
/* KEEP THIS - IT'S A STRENGTH */
.vibe-button,
.vibe-input,
.vibe-card {
  transition: all 0.2s ease;
}
```

**Rationale:**
- 200ms feels more deliberate and luxurious
- 150ms can feel rushed
- This is a Vibe.UI differentiator (in a good way)

---

## 3. Documentation Site Redesign

### Overview

Transform the docs site from "functional" to "stunning" - first impressions matter enormously.

### A. Hero Section Overhaul (CRITICAL)

**Current Problems:**
- Basic gradient
- Small typography
- Static, boring
- No visual interest

**New Hero Design:**

```html
<!-- Hero Section - Make it POP -->
<section class="hero">
    <div class="hero-background">
        <!-- Animated gradient background -->
        <div class="hero-gradient-1"></div>
        <div class="hero-gradient-2"></div>
        <div class="hero-gradient-3"></div>
    </div>

    <div class="hero-content">
        <div class="hero-badge">
            <span class="badge-icon">⚡</span>
            <span>shadcn/ui for Blazor</span>
        </div>

        <h1 class="hero-title">
            Build beautiful<br />
            Blazor apps faster
        </h1>

        <p class="hero-subtitle">
            Copy-paste 90+ accessible components.<br />
            Customize everything. Own your code.
        </p>

        <div class="hero-actions">
            <a href="/getting-started" class="hero-btn-primary">
                Get Started
                <span class="btn-arrow">→</span>
            </a>
            <button class="hero-btn-secondary" onclick="copyInstallCommand()">
                <span class="copy-icon">📋</span>
                <span>dotnet tool install -g Vibe.UI.CLI</span>
            </button>
        </div>

        <!-- Live Component Preview -->
        <div class="hero-preview">
            <div class="preview-window">
                <div class="preview-tabs">
                    <button class="tab active">Preview</button>
                    <button class="tab">Code</button>
                    <button class="theme-toggle">🌙</button>
                </div>
                <div class="preview-content">
                    <!-- Live Button component with all variants -->
                    <Button Variant="Primary">Primary</Button>
                    <Button Variant="Secondary">Secondary</Button>
                    <Button Variant="Outline">Outline</Button>
                </div>
            </div>
        </div>
    </div>
</section>
```

**Hero CSS:**

```css
.hero {
  position: relative;
  min-height: 90vh;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  padding: 4rem 2rem;
}

/* Animated Gradient Background */
.hero-background {
  position: absolute;
  inset: 0;
  z-index: 0;
  background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
}

.hero-gradient-1,
.hero-gradient-2,
.hero-gradient-3 {
  position: absolute;
  border-radius: 50%;
  filter: blur(100px);
  opacity: 0.3;
  animation: float 20s ease-in-out infinite;
}

.hero-gradient-1 {
  width: 500px;
  height: 500px;
  background: var(--brand-teal);
  top: -100px;
  left: -100px;
  animation-delay: 0s;
}

.hero-gradient-2 {
  width: 400px;
  height: 400px;
  background: var(--brand-lilac);
  bottom: -50px;
  right: -50px;
  animation-delay: 5s;
}

.hero-gradient-3 {
  width: 300px;
  height: 300px;
  background: #3b82f6;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  animation-delay: 10s;
}

@keyframes float {
  0%, 100% { transform: translate(0, 0); }
  25% { transform: translate(30px, -30px); }
  50% { transform: translate(-20px, 20px); }
  75% { transform: translate(20px, 30px); }
}

/* Hero Content */
.hero-content {
  position: relative;
  z-index: 1;
  max-width: 1200px;
  text-align: center;
}

.hero-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 9999px;
  color: white;
  font-size: 0.875rem;
  font-weight: 500;
  backdrop-filter: blur(10px);
  margin-bottom: 2rem;
  animation: slideDown 0.5s ease-out;
}

.hero-title {
  font-size: clamp(3rem, 8vw, 6rem);
  font-weight: 800;
  line-height: 1.1;
  color: white;
  margin-bottom: 1.5rem;
  animation: slideUp 0.6s ease-out 0.1s backwards;
}

.hero-subtitle {
  font-size: clamp(1.125rem, 2vw, 1.5rem);
  line-height: 1.6;
  color: rgba(255, 255, 255, 0.8);
  margin-bottom: 3rem;
  animation: slideUp 0.6s ease-out 0.2s backwards;
}

/* Hero Actions */
.hero-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  justify-content: center;
  margin-bottom: 4rem;
  animation: slideUp 0.6s ease-out 0.3s backwards;
}

.hero-btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 1rem 2rem;
  background: white;
  color: #0f172a;
  font-weight: 600;
  font-size: 1.125rem;
  border-radius: 0.75rem;
  text-decoration: none;
  transition: all 0.2s ease;
}

.hero-btn-primary:hover {
  transform: translateY(-2px);
  box-shadow: 0 20px 25px -5px rgb(0 0 0 / 0.2);
}

.hero-btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem 1.5rem;
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.2);
  color: white;
  font-weight: 500;
  font-size: 0.875rem;
  font-family: 'Consolas', 'Monaco', monospace;
  border-radius: 0.75rem;
  backdrop-filter: blur(10px);
  cursor: pointer;
  transition: all 0.2s ease;
}

.hero-btn-secondary:hover {
  background: rgba(255, 255, 255, 0.15);
  border-color: rgba(255, 255, 255, 0.3);
}

/* Hero Preview Window */
.hero-preview {
  animation: slideUp 0.6s ease-out 0.4s backwards;
}

.preview-window {
  max-width: 900px;
  margin: 0 auto;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1rem;
  overflow: hidden;
  backdrop-filter: blur(20px);
  box-shadow: 0 25px 50px -12px rgb(0 0 0 / 0.5);
}

.preview-tabs {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  background: rgba(0, 0, 0, 0.3);
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.preview-tabs .tab {
  padding: 0.5rem 1rem;
  background: transparent;
  border: none;
  color: rgba(255, 255, 255, 0.6);
  font-weight: 500;
  cursor: pointer;
  border-radius: 0.375rem;
  transition: all 0.2s ease;
}

.preview-tabs .tab.active {
  background: rgba(255, 255, 255, 0.1);
  color: white;
}

.preview-tabs .theme-toggle {
  margin-left: auto;
  padding: 0.5rem;
  background: transparent;
  border: none;
  color: white;
  font-size: 1.25rem;
  cursor: pointer;
  border-radius: 0.375rem;
  transition: all 0.2s ease;
}

.preview-tabs .theme-toggle:hover {
  background: rgba(255, 255, 255, 0.1);
}

.preview-content {
  padding: 3rem;
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  justify-content: center;
  align-items: center;
  min-height: 200px;
}

/* Animations */
@keyframes slideDown {
  from {
    opacity: 0;
    transform: translateY(-20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes slideUp {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* Mobile Responsive */
@media (max-width: 768px) {
  .hero {
    min-height: 100vh;
    padding: 2rem 1rem;
  }

  .hero-title {
    font-size: 2.5rem;
  }

  .hero-actions {
    flex-direction: column;
    width: 100%;
  }

  .hero-btn-primary,
  .hero-btn-secondary {
    width: 100%;
    justify-content: center;
  }
}
```

**Impact:** MASSIVE - This is what makes people go "wow"

### B. Component Preview Cards

**Current Problems:**
- Basic code blocks
- No interactivity
- Can't toggle dark mode
- No copy button

**New Preview Design:**

```html
<div class="component-preview-card">
    <!-- Preview Header -->
    <div class="preview-header">
        <h3>Button</h3>
        <div class="preview-actions">
            <button class="preview-btn" onclick="togglePreviewTheme()">
                <span class="icon">🌙</span>
            </button>
            <button class="preview-btn" onclick="copyCode()">
                <span class="icon">📋</span>
                <span class="tooltip">Copy</span>
            </button>
        </div>
    </div>

    <!-- Tabs -->
    <div class="preview-tabs">
        <button class="preview-tab active" onclick="showTab('preview')">
            Preview
        </button>
        <button class="preview-tab" onclick="showTab('code')">
            Code
        </button>
    </div>

    <!-- Preview Panel -->
    <div class="preview-panel active">
        <div class="preview-stage">
            <!-- Live component here -->
            <Button Variant="Primary">Primary</Button>
        </div>
    </div>

    <!-- Code Panel -->
    <div class="code-panel">
        <pre><code class="language-razor">@* Component code here *@</code></pre>
    </div>
</div>
```

**Preview Card CSS:**

```css
.component-preview-card {
  border: 1px solid var(--docs-border);
  border-radius: var(--vibe-radius-lg);
  overflow: hidden;
  background: var(--docs-surface);
  box-shadow: var(--vibe-shadow-sm);
  margin-bottom: 2rem;
}

.preview-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.5rem;
  border-bottom: 1px solid var(--docs-border);
}

.preview-header h3 {
  font-size: 1.25rem;
  font-weight: 600;
  margin: 0;
}

.preview-actions {
  display: flex;
  gap: 0.5rem;
}

.preview-btn {
  position: relative;
  padding: 0.5rem;
  background: transparent;
  border: 1px solid var(--docs-border);
  border-radius: var(--vibe-radius-sm);
  cursor: pointer;
  transition: all 0.2s ease;
}

.preview-btn:hover {
  background: var(--docs-bg);
  border-color: var(--docs-primary);
}

.preview-btn .tooltip {
  position: absolute;
  top: -2rem;
  right: 0;
  padding: 0.25rem 0.5rem;
  background: var(--docs-code-bg);
  color: var(--docs-code-text);
  font-size: 0.75rem;
  border-radius: var(--vibe-radius-sm);
  white-space: nowrap;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.2s ease;
}

.preview-btn:hover .tooltip {
  opacity: 1;
}

.preview-tabs {
  display: flex;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  background: var(--docs-bg);
  border-bottom: 1px solid var(--docs-border);
}

.preview-tab {
  padding: 0.5rem 1rem;
  background: transparent;
  border: none;
  color: var(--docs-text-muted);
  font-weight: 500;
  cursor: pointer;
  border-radius: var(--vibe-radius-sm);
  transition: all 0.2s ease;
}

.preview-tab.active {
  background: var(--docs-surface);
  color: var(--docs-primary);
}

.preview-panel {
  display: none;
  padding: 3rem;
}

.preview-panel.active {
  display: block;
}

.preview-stage {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  align-items: center;
  justify-content: center;
  min-height: 200px;
  padding: 2rem;
  background:
    repeating-linear-gradient(
      45deg,
      transparent,
      transparent 10px,
      var(--docs-border) 10px,
      var(--docs-border) 11px
    );
  border-radius: var(--vibe-radius-md);
}

.code-panel {
  display: none;
  padding: 0;
  background: var(--docs-code-bg);
}

.code-panel.active {
  display: block;
}

.code-panel pre {
  margin: 0;
  padding: 1.5rem;
  overflow-x: auto;
}

.code-panel code {
  color: var(--docs-code-text);
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
  font-size: 0.875rem;
  line-height: 1.6;
}
```

**Impact:** Makes documentation feel professional and interactive

### C. Features Section

**Current:** Basic grid with emojis
**Proposed:** Animated cards with icons and hover effects

```html
<section class="features-section">
    <div class="section-header">
        <h2>Why Vibe.UI?</h2>
        <p>Everything you need to build modern Blazor applications</p>
    </div>

    <div class="features-grid">
        <div class="feature-card">
            <div class="feature-icon">
                <svg><!-- Icon --></svg>
            </div>
            <h3>93+ Components</h3>
            <p>Production-ready components for every use case</p>
            <a href="/components" class="feature-link">
                Browse components →
            </a>
        </div>

        <div class="feature-card">
            <div class="feature-icon">
                <svg><!-- Icon --></svg>
            </div>
            <h3>Copy, Not Install</h3>
            <p>Own your code. Customize everything.</p>
            <a href="/cli" class="feature-link">
                Learn about CLI →
            </a>
        </div>

        <div class="feature-card">
            <div class="feature-icon">
                <svg><!-- Icon --></svg>
            </div>
            <h3>Built-in Theming</h3>
            <p>Light/dark mode with runtime switching</p>
            <a href="/theming" class="feature-link">
                Explore themes →
            </a>
        </div>

        <!-- 3 more feature cards -->
    </div>
</section>
```

**Features CSS:**

```css
.features-section {
  padding: 6rem 2rem;
  background: var(--docs-surface);
}

.section-header {
  text-align: center;
  margin-bottom: 4rem;
}

.section-header h2 {
  font-size: clamp(2rem, 5vw, 3rem);
  font-weight: 800;
  margin-bottom: 1rem;
}

.section-header p {
  font-size: 1.25rem;
  color: var(--docs-text-muted);
}

.features-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 2rem;
  max-width: 1200px;
  margin: 0 auto;
}

.feature-card {
  padding: 2rem;
  background: var(--docs-bg);
  border: 1px solid var(--docs-border);
  border-radius: var(--vibe-radius-lg);
  transition: all 0.2s ease;
}

.feature-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--vibe-shadow-lg);
  border-color: var(--docs-primary);
}

.feature-icon {
  width: 3rem;
  height: 3rem;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, var(--brand-teal), var(--brand-lilac));
  border-radius: var(--vibe-radius-md);
  margin-bottom: 1.5rem;
}

.feature-icon svg {
  width: 1.5rem;
  height: 1.5rem;
  color: white;
}

.feature-card h3 {
  font-size: 1.25rem;
  font-weight: 600;
  margin-bottom: 0.5rem;
}

.feature-card p {
  color: var(--docs-text-muted);
  margin-bottom: 1rem;
}

.feature-link {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  color: var(--docs-primary);
  font-weight: 500;
  text-decoration: none;
  transition: gap 0.2s ease;
}

.feature-link:hover {
  gap: 0.5rem;
}
```

**Impact:** More engaging, guides users to explore

### D. Code Block Improvements

**Current:** Basic pre/code blocks
**Proposed:** Syntax-highlighted with copy button

```html
<div class="code-block">
    <div class="code-block-header">
        <span class="code-block-lang">razor</span>
        <button class="code-block-copy" onclick="copyCode(this)">
            <svg class="icon-copy"><!-- Copy icon --></svg>
            <svg class="icon-check"><!-- Check icon --></svg>
            <span>Copy</span>
        </button>
    </div>
    <pre><code class="language-razor">@* Code here *@</code></pre>
</div>
```

**Code Block CSS:**

```css
.code-block {
  border: 1px solid var(--docs-border);
  border-radius: var(--vibe-radius-md);
  overflow: hidden;
  margin: 1.5rem 0;
  background: var(--docs-code-bg);
}

.code-block-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.75rem 1rem;
  background: rgba(0, 0, 0, 0.2);
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.code-block-lang {
  font-size: 0.75rem;
  text-transform: uppercase;
  font-weight: 600;
  color: var(--docs-code-text);
  opacity: 0.6;
  letter-spacing: 0.05em;
}

.code-block-copy {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.375rem 0.75rem;
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: var(--vibe-radius-sm);
  color: var(--docs-code-text);
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.code-block-copy:hover {
  background: rgba(255, 255, 255, 0.15);
}

.code-block-copy .icon-check {
  display: none;
}

.code-block-copy.copied .icon-copy {
  display: none;
}

.code-block-copy.copied .icon-check {
  display: block;
}

.code-block pre {
  margin: 0;
  padding: 1.5rem;
  overflow-x: auto;
}

.code-block code {
  color: var(--docs-code-text);
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
  font-size: 0.875rem;
  line-height: 1.6;
}

/* Syntax Highlighting - Use PrismJS or highlight.js */
```

**Impact:** Better UX, easier to copy code

### E. Typography Scale

**Current:** Inconsistent sizes
**Proposed:** Proper type scale

```css
/* TYPOGRAPHY SYSTEM */
:root {
  --font-sans: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto',
               'Oxygen', 'Ubuntu', 'Cantarell', 'Fira Sans', 'Droid Sans',
               'Helvetica Neue', sans-serif;
  --font-mono: 'Consolas', 'Monaco', 'Courier New', monospace;

  /* Type Scale */
  --text-xs: 0.75rem;      /* 12px */
  --text-sm: 0.875rem;     /* 14px */
  --text-base: 1rem;       /* 16px */
  --text-lg: 1.125rem;     /* 18px */
  --text-xl: 1.25rem;      /* 20px */
  --text-2xl: 1.5rem;      /* 24px */
  --text-3xl: 1.875rem;    /* 30px */
  --text-4xl: 2.25rem;     /* 36px */
  --text-5xl: 3rem;        /* 48px */
  --text-6xl: 3.75rem;     /* 60px */

  /* Line Heights */
  --leading-none: 1;
  --leading-tight: 1.25;
  --leading-snug: 1.375;
  --leading-normal: 1.5;
  --leading-relaxed: 1.625;
  --leading-loose: 2;
}

/* Apply to Elements */
body {
  font-family: var(--font-sans);
  font-size: var(--text-base);
  line-height: var(--leading-normal);
}

h1 {
  font-size: var(--text-4xl);
  line-height: var(--leading-tight);
  font-weight: 800;
}

h2 {
  font-size: var(--text-3xl);
  line-height: var(--leading-tight);
  font-weight: 700;
}

h3 {
  font-size: var(--text-2xl);
  line-height: var(--leading-snug);
  font-weight: 600;
}

code, pre {
  font-family: var(--font-mono);
}
```

**Impact:** Better hierarchy, more professional

---

## 4. Implementation Order

### Phase 1: Architecture (Day 1 - Morning)

**Priority: CRITICAL**
**Time: 4-6 hours**

1. Create `src/Vibe.UI/wwwroot/css/vibe-components.css`
2. Consolidate all 91 `.razor.css` files into sections:
   - CSS Variables & Reset
   - Button Styles
   - Card Styles
   - Input Styles
   - ... (organized by component category)
3. Delete individual `.razor.css` files
4. Update component markup if needed (remove scoped class prefixes)
5. Test all components still render correctly

**Success Criteria:**
- All components render identically
- Single CSS file under 50KB (minified)
- All styles in logical sections with comments

### Phase 2: Component Visual Polish (Day 1 - Afternoon)

**Priority: HIGH**
**Time: 3-4 hours**

1. Update CSS variables (radius, spacing, shadows)
2. Apply new border radius strategy
3. Increase card spacing
4. Improve focus states
5. Standardize button font sizes
6. Enhance card shadows
7. Test visually across all components

**Success Criteria:**
- Components look noticeably more polished
- Consistent 4px spacing grid
- Better visual hierarchy

### Phase 3: Doc Site Hero (Day 2 - Morning)

**Priority: HIGH**
**Time: 3-4 hours**

1. Redesign hero section
2. Add animated gradient background
3. Implement live component preview
4. Add copy-install button
5. Make responsive for mobile

**Success Criteria:**
- Hero section makes you go "wow"
- Animations are smooth, not janky
- Works on mobile

### Phase 4: Component Preview Cards (Day 2 - Afternoon)

**Priority: MEDIUM**
**Time: 3-4 hours**

1. Create new preview card component
2. Add tabs (Preview/Code)
3. Add copy button with animation
4. Add dark mode toggle for previews
5. Update all component pages

**Success Criteria:**
- Interactive previews
- Copy button works reliably
- Theme toggle works

### Phase 5: Polish & Refinement (Day 3)

**Priority: LOW**
**Time: 4-6 hours**

1. Update features section
2. Improve code blocks
3. Add more micro-interactions
4. Mobile responsive testing
5. Performance optimization
6. Browser testing

**Success Criteria:**
- Site feels premium
- Fast load times
- Works in all browsers

---

## 5. Detailed File Changes

### A. Component CSS Migration

**New File:** `src/Vibe.UI/wwwroot/css/vibe-components.css`

```css
/* ========================================
   VIBE.UI COMPONENT LIBRARY
   Blazor components inspired by shadcn/ui
   ======================================== */

/* ========================================
   CSS VARIABLES & DESIGN TOKENS
   ======================================== */

:root {
  /* Colors - Keep Vibe.UI's opinionated blue */
  --vibe-background: #ffffff;
  --vibe-foreground: #111111;
  --vibe-primary: #0066cc;
  --vibe-primary-foreground: #ffffff;
  --vibe-secondary: #f4f4f4;
  --vibe-secondary-foreground: #111111;
  --vibe-muted: #f1f1f1;
  --vibe-muted-foreground: #666666;
  --vibe-accent: #0080ff;
  --vibe-accent-foreground: #ffffff;
  --vibe-destructive: #e11d48;
  --vibe-destructive-foreground: #ffffff;
  --vibe-border: #e2e2e2;
  --vibe-input: #ffffff;
  --vibe-ring: #0066cc;
  --vibe-card: #ffffff;
  --vibe-card-foreground: #111111;

  /* Border Radius - NEW: Varied sizes */
  --vibe-radius: 0.625rem;          /* Default: 10px */
  --vibe-radius-sm: 0.375rem;       /* Small: 6px */
  --vibe-radius-md: 0.5rem;         /* Medium: 8px */
  --vibe-radius-lg: 0.75rem;        /* Large: 12px */
  --vibe-radius-xl: 1rem;           /* Extra large: 16px */

  /* Spacing - 4px grid system */
  --vibe-spacing-1: 0.25rem;        /* 4px */
  --vibe-spacing-2: 0.5rem;         /* 8px */
  --vibe-spacing-3: 0.75rem;        /* 12px */
  --vibe-spacing-4: 1rem;           /* 16px */
  --vibe-spacing-5: 1.25rem;        /* 20px */
  --vibe-spacing-6: 1.5rem;         /* 24px */
  --vibe-spacing-8: 2rem;           /* 32px */
  --vibe-spacing-10: 2.5rem;        /* 40px */
  --vibe-spacing-12: 3rem;          /* 48px */
  --vibe-spacing-16: 4rem;          /* 64px */
  --vibe-spacing-20: 5rem;          /* 80px */

  /* Shadows - Layered for depth */
  --vibe-shadow-sm: 0 1px 2px 0 rgb(0 0 0 / 0.05);
  --vibe-shadow-md: 0 4px 6px -1px rgb(0 0 0 / 0.1),
                    0 2px 4px -2px rgb(0 0 0 / 0.1);
  --vibe-shadow-lg: 0 10px 15px -3px rgb(0 0 0 / 0.1),
                    0 4px 6px -4px rgb(0 0 0 / 0.1);
  --vibe-shadow-xl: 0 20px 25px -5px rgb(0 0 0 / 0.1),
                    0 8px 10px -6px rgb(0 0 0 / 0.1);

  /* Typography */
  --vibe-font: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto',
               'Oxygen', 'Ubuntu', 'Cantarell', 'Fira Sans', 'Droid Sans',
               'Helvetica Neue', sans-serif;

  /* Transitions - Keep 200ms (Vibe.UI strength) */
  --vibe-transition: all 0.2s ease;
}

/* Dark Theme */
.dark {
  --vibe-background: #0f172a;
  --vibe-foreground: #f8fafc;
  --vibe-primary: #3b82f6;
  --vibe-primary-foreground: #ffffff;
  --vibe-secondary: #1e293b;
  --vibe-secondary-foreground: #f8fafc;
  --vibe-muted: #1e293b;
  --vibe-muted-foreground: #94a3b8;
  --vibe-accent: #60a5fa;
  --vibe-accent-foreground: #0f172a;
  --vibe-destructive: #ef4444;
  --vibe-destructive-foreground: #ffffff;
  --vibe-border: #334155;
  --vibe-input: #1e293b;
  --vibe-ring: #3b82f6;
  --vibe-card: #1e293b;
  --vibe-card-foreground: #f8fafc;
}

/* ========================================
   BUTTON COMPONENT
   ======================================== */

.vibe-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--vibe-spacing-2);
  border-radius: var(--vibe-radius-sm);  /* NEW: 6px for buttons */
  font-family: var(--vibe-font);
  font-weight: 500;
  cursor: pointer;
  transition: var(--vibe-transition);
  border: 1px solid transparent;
  white-space: nowrap;
}

.vibe-button:focus-visible {
  outline: none;
  box-shadow: 0 0 0 2px var(--vibe-background),
              0 0 0 4px var(--vibe-ring);
}

.vibe-button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  pointer-events: none;
}

/* Button Sizes - UPDATED */
.vibe-button-small {
  height: 2rem;                           /* 32px */
  padding: 0 var(--vibe-spacing-3);       /* 12px */
  font-size: 0.875rem;                    /* 14px - UPDATED from 12px */
}

.vibe-button-medium {
  height: 2.5rem;                         /* 40px */
  padding: 0 var(--vibe-spacing-4);       /* 16px */
  font-size: 0.875rem;                    /* 14px */
}

.vibe-button-large {
  height: 3rem;                           /* 48px */
  padding: 0 var(--vibe-spacing-8);       /* 32px - UPDATED from 24px */
  font-size: 0.875rem;                    /* 14px - UPDATED from 16px */
}

/* Button Variants */
.vibe-button-primary {
  background-color: var(--vibe-primary);
  color: var(--vibe-primary-foreground);
}

.vibe-button-primary:hover:not(:disabled) {
  background-color: color-mix(in srgb, var(--vibe-primary) 90%, black);
}

.vibe-button-secondary {
  background-color: var(--vibe-secondary);
  color: var(--vibe-secondary-foreground);
}

.vibe-button-secondary:hover:not(:disabled) {
  background-color: color-mix(in srgb, var(--vibe-secondary) 90%, black);
}

.vibe-button-destructive {
  background-color: var(--vibe-destructive);
  color: var(--vibe-destructive-foreground);
}

.vibe-button-destructive:hover:not(:disabled) {
  background-color: color-mix(in srgb, var(--vibe-destructive) 90%, black);
}

.vibe-button-outline {
  background-color: transparent;
  border-color: var(--vibe-border);
  color: var(--vibe-foreground);
}

.vibe-button-outline:hover:not(:disabled) {
  background-color: var(--vibe-secondary);
}

.vibe-button-ghost {
  background-color: transparent;
  color: var(--vibe-foreground);
}

.vibe-button-ghost:hover:not(:disabled) {
  background-color: var(--vibe-secondary);
}

.vibe-button-link {
  background-color: transparent;
  color: var(--vibe-primary);
  text-decoration: underline;
  text-underline-offset: 4px;
}

.vibe-button-link:hover:not(:disabled) {
  text-decoration: none;
}

/* Button States */
.vibe-button-loading {
  position: relative;
  color: transparent;
  pointer-events: none;
}

.vibe-button-loading::after {
  content: '';
  position: absolute;
  width: 1rem;
  height: 1rem;
  border: 2px solid currentColor;
  border-right-color: transparent;
  border-radius: 50%;
  animation: vibe-spin 0.6s linear infinite;
}

@keyframes vibe-spin {
  to { transform: rotate(360deg); }
}

.vibe-button-full-width {
  width: 100%;
}

.vibe-button-icon-only {
  padding: var(--vibe-spacing-2);
  aspect-ratio: 1;
}

/* ========================================
   CARD COMPONENT
   ======================================== */

.vibe-card {
  display: flex;
  flex-direction: column;
  border-radius: var(--vibe-radius-lg);    /* NEW: 12px for cards */
  border: 1px solid var(--vibe-border);
  background-color: var(--vibe-card);
  color: var(--vibe-card-foreground);
  box-shadow: var(--vibe-shadow-sm);       /* UPDATED: Better shadow */
  transition: var(--vibe-transition);
}

.vibe-card-elevated {
  box-shadow: var(--vibe-shadow-md);       /* NEW: More prominent */
}

.vibe-card:hover {
  box-shadow: var(--vibe-shadow-md);
}

.vibe-card-header {
  display: flex;
  flex-direction: column;
  gap: var(--vibe-spacing-2);              /* 8px between title/desc */
  padding: var(--vibe-spacing-6);          /* 24px */
  padding-bottom: var(--vibe-spacing-4);   /* 16px bottom */
}

.vibe-card-title {
  font-size: 1.25rem;
  font-weight: 600;
  line-height: 1.25;
}

.vibe-card-description {
  font-size: 0.875rem;
  color: var(--vibe-muted-foreground);
  line-height: 1.5;
}

.vibe-card-content {
  display: flex;
  flex-direction: column;
  gap: var(--vibe-spacing-6);              /* 24px - UPDATED from 16px */
  padding: 0 var(--vibe-spacing-6);        /* 24px sides */
  padding-bottom: var(--vibe-spacing-6);   /* 24px bottom */
  flex: 1;
}

.vibe-card-footer {
  display: flex;
  align-items: center;
  gap: var(--vibe-spacing-2);
  padding: var(--vibe-spacing-4) var(--vibe-spacing-6);  /* 16px top/bottom, 24px sides */
  border-top: 1px solid var(--vibe-border);
}

/* ========================================
   INPUT COMPONENT
   ======================================== */

.vibe-input {
  display: flex;
  width: 100%;
  border-radius: var(--vibe-radius-sm);    /* NEW: 6px for inputs */
  border: 1px solid var(--vibe-border);
  background-color: var(--vibe-input);
  color: var(--vibe-foreground);
  font-family: var(--vibe-font);
  font-size: 0.875rem;
  transition: var(--vibe-transition);
}

.vibe-input:focus {
  outline: none;
  border-color: var(--vibe-ring);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--vibe-ring) 50%, transparent);  /* UPDATED: 3px, 50% opacity */
}

.vibe-input:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.vibe-input::placeholder {
  color: var(--vibe-muted-foreground);
}

/* Input Sizes */
.vibe-input-small {
  height: 2rem;                            /* 32px */
  padding: 0 var(--vibe-spacing-3);        /* 12px */
}

.vibe-input-medium {
  height: 2.5rem;                          /* 40px */
  padding: 0 var(--vibe-spacing-4);        /* 16px */
}

.vibe-input-large {
  height: 3rem;                            /* 48px */
  padding: 0 var(--vibe-spacing-5);        /* 20px */
}

/* Input States */
.vibe-input-error {
  border-color: var(--vibe-destructive);
}

.vibe-input-error:focus {
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--vibe-destructive) 50%, transparent);
}

/* ========================================
   ... CONTINUE FOR ALL 91 COMPONENTS ...
   ======================================== */

/* Organize by category:
   - Layout (Card, AspectRatio, Separator, etc.)
   - Input (Button, Input, Select, Checkbox, etc.)
   - Data Display (Table, Badge, Avatar, etc.)
   - Navigation (Tabs, Breadcrumb, Pagination, etc.)
   - Overlay (Dialog, Popover, Tooltip, etc.)
   - Feedback (Alert, Toast, Skeleton, etc.)
   - Advanced (TreeView, KanbanBoard, etc.)
*/
```

### B. Documentation Site Files

**File:** `samples/Vibe.UI.Docs/wwwroot/css/docs.css`

Update with all the hero, preview, and feature styles from Section 3 above.

**File:** `samples/Vibe.UI.Docs/Pages/Home.razor`

Replace with the new hero HTML from Section 3A.

**File:** `samples/Vibe.UI.Docs/Shared/ComponentPage.razor`

Update to use new preview card component from Section 3B.

### C. JavaScript Interactions

**File:** `samples/Vibe.UI.Docs/wwwroot/js/docs.js`

```javascript
// Copy Code Button
function copyCode(button) {
  const codeBlock = button.closest('.code-block');
  const code = codeBlock.querySelector('code').textContent;

  navigator.clipboard.writeText(code).then(() => {
    button.classList.add('copied');
    setTimeout(() => button.classList.remove('copied'), 2000);
  });
}

// Copy Install Command
function copyInstallCommand() {
  const command = 'dotnet tool install -g Vibe.UI.CLI';

  navigator.clipboard.writeText(command).then(() => {
    // Show success toast
    showToast('Copied to clipboard!');
  });
}

// Toggle Preview Theme
function togglePreviewTheme() {
  const preview = document.querySelector('.preview-stage');
  preview.classList.toggle('dark');
}

// Show Tab
function showTab(tabName) {
  const tabs = document.querySelectorAll('.preview-tab');
  const panels = document.querySelectorAll('.preview-panel, .code-panel');

  tabs.forEach(tab => tab.classList.remove('active'));
  panels.forEach(panel => panel.classList.remove('active'));

  event.target.classList.add('active');
  document.querySelector(`.${tabName}-panel`).classList.add('active');
}

// Toast Notification
function showToast(message) {
  const toast = document.createElement('div');
  toast.className = 'toast';
  toast.textContent = message;
  document.body.appendChild(toast);

  setTimeout(() => toast.classList.add('show'), 10);
  setTimeout(() => {
    toast.classList.remove('show');
    setTimeout(() => toast.remove(), 300);
  }, 2000);
}
```

---

## 6. Success Metrics

### Before (Current State)
- Components look "okay"
- Docs site is functional but basic
- No interactivity in previews
- 91 separate CSS files
- Inconsistent spacing/sizing

### After (Target State)
- Components look polished and modern
- Docs site makes great first impression
- Interactive component previews
- Single CSS file for easy customization
- Consistent 4px spacing grid
- Better visual hierarchy
- Animations and micro-interactions
- Professional typography

### Measurable Goals
- [ ] Site load time < 2 seconds
- [ ] Lighthouse score > 90
- [ ] Mobile responsive on all pages
- [ ] All components visually consistent
- [ ] Zero CSS duplication
- [ ] User feedback: "Looks as good as shadcn/ui"

---

## 7. Maintenance & Future

### Documentation
- Document the new design system
- Create a "Design Principles" page
- Show before/after comparisons
- Explain the CSS architecture decision

### Long-term Considerations
- Consider adding more themes (Neutral variant)
- Potentially migrate to OKLCH colors (future)
- Add more component variants as needed
- Keep docs site updated with latest components

### Community Feedback
- Share redesign on Twitter/Reddit
- Gather user feedback
- Iterate on any issues
- Celebrate the improved aesthetic!

---

## 8. Conclusion

This comprehensive redesign will transform Vibe.UI from "functional" to "stunning" while maintaining all the architectural benefits we achieved with the hybrid theming approach.

**Key Wins:**
1. Components will look competitive with shadcn/ui
2. Docs site will make a killer first impression
3. Single CSS file makes customization easy
4. Better consistency across all 90+ components
5. More professional, modern aesthetic

**Timeline:** 2-3 focused days
**Effort:** High (but worthwhile)
**Impact:** Transformative

Ready to make Vibe.UI look AMAZING? 🚀
