# Dark Mode Toggle — Design Spec

**Date:** 2026-04-19  
**Status:** Approved

## Summary

Add a dark/light mode toggle to the ParkTracker navbar. The toggle persists the user's preference in `localStorage` and falls back to the OS `prefers-color-scheme` setting when no preference has been saved. Dark mode applies across all pages (main app and auth/account pages).

## Approach

Bootstrap 5.3.3 (already in use) has built-in dark mode via `data-bs-theme="dark"` on the `<html>` element. Setting this attribute flips all Bootstrap components automatically. A small set of CSS overrides in `app.css` handles the custom green brand colors that Bootstrap doesn't know about.

## Components

### 1. Theme Initialization Script (`App.razor`)

An inline `<script>` block inserted in `<head>` **before** stylesheet links runs immediately on page load to prevent a flash of the wrong theme.

```js
(function() {
    const stored = localStorage.getItem('theme');
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    const theme = stored ?? (prefersDark ? 'dark' : 'light');
    document.documentElement.setAttribute('data-bs-theme', theme);
})();
```

Logic:
- If `localStorage('theme')` is set, use it
- Otherwise, use OS `prefers-color-scheme`
- Apply result as `data-bs-theme` attribute on `<html>`

### 2. JS Helpers (`wwwroot/js/theme.js`)

Two small functions called from Blazor via `IJSRuntime`:

```js
window.getTheme = () => document.documentElement.getAttribute('data-bs-theme');
window.setTheme = (theme) => {
    document.documentElement.setAttribute('data-bs-theme', theme);
    localStorage.setItem('theme', theme);
};
```

`theme.js` is added as a `<script>` tag in `App.razor` body.

### 3. Toggle Component (`Components/Layout/ThemeToggle.razor`)

A Blazor component that renders a button in the navbar and wires up the JS interop.

- Injects `IJSRuntime`
- `_isDark` bool tracks current state
- `OnAfterRenderAsync` (first render only): calls `getTheme()` to initialize `_isDark`
- Click handler: computes new theme string, calls `setTheme(newTheme)`, flips `_isDark`, calls `StateHasChanged()`
- Button label: shows ☀️ when in dark mode (click to go light), 🌙 when in light mode (click to go dark)
- `aria-label` updates to match current action

### 4. Navbar Integration (`Components/Layout/NavMenu.razor`)

`<ThemeToggle />` is placed in the right-side `<ul class="navbar-nav ms-auto">` before the auth links.

### 5. CSS Overrides (`wwwroot/app.css`)

Bootstrap handles its own components. The following custom-branded elements need explicit dark overrides:

| Element | Light value | Dark override |
|---|---|---|
| `a, .btn-link` color | `#2d6a4f` | `#74c69d` (lighter green, readable on dark bg) |
| `.btn-primary` bg/border | `#2d6a4f` / `#1b4332` | `#40916c` / `#2d6a4f` |
| `.btn-primary:hover` bg | `#1b4332` | `#2d6a4f` |
| Focus box-shadow | `rgba(45,106,79,0.4)` | `rgba(116,198,157,0.4)` |

The navbar (`NavMenu.razor.css`) uses `background-color: #1b4332` (dark green) — no change needed; it reads fine in both modes.

`ParkList.razor` uses `table-dark` on `<thead>` — already correct in both themes.

## File Changes

| File | Change |
|---|---|
| `ParkTracker/Components/App.razor` | Add inline init script in `<head>`; add `<script src="js/theme.js">` in body |
| `ParkTracker/wwwroot/js/theme.js` | New file — `getTheme` and `setTheme` JS helpers |
| `ParkTracker/Components/Layout/ThemeToggle.razor` | New file — toggle button component |
| `ParkTracker/Components/Layout/NavMenu.razor` | Add `<ThemeToggle />` to right nav |
| `ParkTracker/wwwroot/app.css` | Add `[data-bs-theme="dark"]` overrides for custom greens |

## Out of Scope

- Per-user server-side preference storage (localStorage is sufficient)
- Custom dark palette for the Leaflet map tiles (map tiles are third-party; inverting them with CSS filter is a separate concern)
- Animated toggle transitions
