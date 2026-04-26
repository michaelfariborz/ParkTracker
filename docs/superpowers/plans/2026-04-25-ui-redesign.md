# UI Redesign — Modern Adventure Aesthetic Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Elevate ParkTracker's visual design from generic Bootstrap to a bold "Modern Adventure" aesthetic with DM Sans typography, a lime accent color system, a hero visit counter, SVG map markers, and refined component polish.

**Architecture:** All changes are CSS/HTML/JS only — no Blazor service or data layer changes. The CSS is reorganized around CSS custom properties (tokens) so future theming is easier. New styles are appended to `app.css`; existing hardcoded hex values are replaced with token references.

**Tech Stack:** .NET 10 Blazor Server, Bootstrap 5, Leaflet, DM Sans + DM Mono (Google Fonts)

**Worktree:** `.worktrees/feature-ui-redesign` on branch `feature/ui-redesign`

**Build command:** `cd .worktrees/feature-ui-redesign && dotnet build 2>&1 | tail -5`

---

## Files Modified

| File | Change |
|------|--------|
| `ParkTracker/Program.cs` | Add Google Fonts to CSP (`style-src`, `font-src`) |
| `ParkTracker/Components/App.razor` | Add Google Fonts preconnect + stylesheet link |
| `ParkTracker/wwwroot/app.css` | Full rewrite: CSS tokens, DM Sans/Mono, grain bg, new component styles |
| `ParkTracker/Components/Pages/Home.razor` | Hero visit counter with progress bar |
| `ParkTracker/Components/Pages/ParkList.razor` | Row accent, search class, visited badge class, mono counter |
| `ParkTracker/Components/Layout/NavMenu.razor` | SVG mountain icon replaces tree emoji |
| `ParkTracker/Components/Layout/NavMenu.razor.css` | Active link accent → `var(--color-accent)` |
| `ParkTracker/Components/Shared/EditVisitModal.razor` | "Remove Visit" button moved left with `me-auto` |
| `ParkTracker/wwwroot/js/leafletInterop.js` | SVG pin markers replace circle dots |

---

## Task 1: CSS Foundation — Tokens, Typography, Google Fonts, CSP

### Files
- Modify: `ParkTracker/Program.cs:91-92`
- Modify: `ParkTracker/Components/App.razor:17` (before bootstrap CSS link)
- Modify: `ParkTracker/wwwroot/app.css` (full replacement)

- [ ] **Step 1: Update CSP in Program.cs to allow Google Fonts**

In `ParkTracker/Program.cs`, replace lines 88–92:

```csharp
context.Response.Headers["Content-Security-Policy"] =
    "default-src 'self'; " +
    "script-src 'self' 'unsafe-inline' https://unpkg.com; " +
    "style-src 'self' 'unsafe-inline' https://unpkg.com; " +
    "img-src 'self' data: https://*.tile.openstreetmap.org;";
```

With:

```csharp
context.Response.Headers["Content-Security-Policy"] =
    "default-src 'self'; " +
    "script-src 'self' 'unsafe-inline' https://unpkg.com; " +
    "style-src 'self' 'unsafe-inline' https://unpkg.com https://fonts.googleapis.com; " +
    "font-src 'self' https://fonts.gstatic.com; " +
    "img-src 'self' data: https://*.tile.openstreetmap.org;";
```

- [ ] **Step 2: Add Google Fonts to App.razor**

In `ParkTracker/Components/App.razor`, insert after `<base href="/" />` (line 7), before the `<ResourcePreloader />` line:

```html
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link href="https://fonts.googleapis.com/css2?family=DM+Mono:ital,wght@0,300;0,400;0,500;1,300&family=DM+Sans:ital,opsz,wght@0,9..40,300;0,9..40,400;0,9..40,500;0,9..40,600;0,9..40,700;1,9..40,400&display=swap" rel="stylesheet" />
```

- [ ] **Step 3: Rewrite app.css with CSS tokens and typography**

Replace the entire contents of `ParkTracker/wwwroot/app.css` with:

```css
/* ============================================================
   CSS Custom Properties
   ============================================================ */
:root {
    --color-primary: #2d6a4f;
    --color-primary-dark: #1b4332;
    --color-accent: #a8e063;
    --color-accent-dim: rgba(168, 224, 99, 0.25);
    --color-text-muted: #6b7280;
    --color-base: #f5f5f2;
    --font-sans: 'DM Sans', 'Helvetica Neue', Helvetica, Arial, sans-serif;
    --font-mono: 'DM Mono', 'Consolas', 'Courier New', monospace;
}

[data-bs-theme="dark"] {
    --color-primary: #40916c;
    --color-primary-dark: #2d6a4f;
    --color-accent-dim: rgba(168, 224, 99, 0.15);
    --color-base: #0f1a12;
}

/* ============================================================
   Base Typography & Background
   ============================================================ */
html, body {
    font-family: var(--font-sans);
}

body {
    background-color: var(--color-base);
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='300' height='300'%3E%3Cfilter id='noise'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='4' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='300' height='300' filter='url(%23noise)' opacity='0.035'/%3E%3C/svg%3E");
    background-repeat: repeat;
    background-size: 300px;
}

[data-bs-theme="dark"] body {
    background-image: none;
}

/* ============================================================
   Links & Buttons
   ============================================================ */
a, .btn-link {
    color: var(--color-primary);
}

.btn-primary {
    color: #fff;
    background-color: var(--color-primary);
    border-color: var(--color-primary-dark);
}

.btn-primary:hover, .btn-primary:focus, .btn-primary:active {
    background-color: var(--color-primary-dark);
    border-color: var(--color-primary-dark);
}

.btn:focus, .btn:active:focus, .btn-link.nav-link:focus, .form-control:focus, .form-check-input:focus {
    box-shadow: 0 0 0 0.1rem white, 0 0 0 0.25rem var(--color-accent-dim);
}

/* ============================================================
   Layout
   ============================================================ */
.content {
    padding-top: 1.1rem;
}

h1:focus {
    outline: none;
}

/* ============================================================
   Form Validation
   ============================================================ */
.valid.modified:not([type=checkbox]) {
    outline: 1px solid #26b050;
}

.invalid {
    outline: 1px solid #e50000;
}

.validation-message {
    color: #e50000;
}

/* ============================================================
   Error Boundary
   ============================================================ */
.blazor-error-boundary {
    background: url(data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNTYiIGhlaWdodD0iNDkiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyIgeG1sbnM6eGxpbms9Imh0dHA6Ly93d3cudzMub3JnLzE5OTkveGxpbmsiIG92ZXJmbG93PSJoaWRkZW4iPjxkZWZzPjxjbGlwUGF0aCBpZD0iY2xpcDAiPjxyZWN0IHg9IjIzNSIgeT0iNTEiIHdpZHRoPSI1NiIgaGVpZ2h0PSI0OSIvPjwvY2xpcFBhdGg+PC9kZWZzPjxnIGNsaXAtcGF0aD0idXJsKCNjbGlwMCkiIHRyYW5zZm9ybT0idHJhbnNsYXRlKC0yMzUgLTUxKSI+PHBhdGggZD0iTTI2My41MDYgNTFDMjY0LjcxNyA1MSAyNjUuODEzIDUxLjQ4MzcgMjY2LjYwNiA1Mi4yNjU4TDI2Ny4wNTIgNTIuNzk4NyAyNjcuNTM5IDUzLjYyODMgMjkwLjE4NSA5Mi4xODMxIDI5MC41NDUgOTIuNzk1IDI5MC42NTYgOTIuOTk2QzI5MC44NzcgOTMuNTEzIDI5MSA5NC4wODE1IDI5MSA5NC42NzgyIDI5MSA5Ny4wNjUxIDI4OS4wMzggOTkgMjg2LjYxNyA5OUwyNDAuMzgzIDk5QzIzNy45NjMgOTkgMjM2IDk3LjA2NTEgMjM2IDk0LjY3ODIgMjM2IDk0LjM3OTkgMjM2LjAzMSA5NC4wODg2IDIzNi4wODkgOTMuODA3MkwyMzYuMzM4IDkzLjAxNjIgMjM2Ljg1OCA5Mi4xMzE0IDI1OS40NzMgNTMuNjI5NCAyNTkuOTYxIDUyLjc5ODUgMjYwLjQwNyA1Mi4yNjU4QzI2MS4yIDUxLjQ4MzcgMjYyLjI5NiA1MSAyNjMuNTA2IDUxWk0yNjMuNTg2IDY2LjAxODNDMjYwLjczNyA2Ni4wMTgzIDI1OS4zMTMgNjcuMTI0NSAyNTkuMzEzIDY5LjMzNyAyNTkuMzEzIDY5LjYxMDIgMjU5LjMzMiA2OS44NjA4IDI1OS4zNzEgNzAuMDg4N0wyNjEuNzk1IDg0LjAxNjEgMjY1LjM4IDg0LjAxNjEgMjY3LjgyMSA2OS43NDc1QzI2Ny44NiA2OS43MzA5IDI2Ny44NzkgNjkuNTg3NyAyNjcuODc5IDY5LjMxNzkgMjY3Ljg3OSA2Ny4xMTgyIDI2Ni40NDggNjYuMDE4MyAyNjMuNTg2IDY2LjAxODNaTTI2My41NzYgODYuMDU0N0MyNjEuMDQ5IDg2LjA1NDcgMjU5Ljc4NiA4Ny4zMDA1IDI1OS43ODYgODkuNzkyMSAyNTkuNzg2IDkyLjI4MzcgMjYxLjA0OSA5My41Mjk1IDI2My41NzYgOTMuNTI5NSAyNjYuMTE2IDkzLjUyOTUgMjY3LjM4NyA5Mi4yODM3IDI2Ny4zODcgODkuNzkyMSAyNjcuMzg3IDg3LjMwMDUgMjY2LjExNiA4Ni4wNTQ3IDI2My41NzYgODYuMDU0N1oiIGZpbGw9IiNGRkU1MDAiIGZpbGwtcnVsZT0iZXZlbm9kZCIvPjwvZz48L3N2Zz4=) no-repeat 1rem/1.8rem, #b32121;
    padding: 1rem 1rem 1rem 3.7rem;
    color: white;
}

.blazor-error-boundary::after {
    content: "An error has occurred."
}

/* ============================================================
   Form Utilities
   ============================================================ */
.darker-border-checkbox.form-check-input {
    border-color: #929292;
}

.form-floating > .form-control-plaintext::placeholder,
.form-floating > .form-control::placeholder {
    color: var(--bs-secondary-color);
    text-align: end;
}

.form-floating > .form-control-plaintext:focus::placeholder,
.form-floating > .form-control:focus::placeholder {
    text-align: start;
}

/* ============================================================
   Theme Toggle
   ============================================================ */
/* In global scope (not NavMenu.razor.css isolation) because ThemeToggle.razor
   renders as a separate component. font-size overrides NavMenu.razor.css's
   font-size: inherit for .btn-link. */
.theme-toggle-btn {
    font-size: 1.1rem;
    padding: 0.5rem 0.85rem;
    line-height: 1;
}

/* Show correct icon based on active theme — CSS-driven, no JS required */
.theme-icon-dark { display: none; }
[data-bs-theme="dark"] .theme-icon-light { display: none; }
[data-bs-theme="dark"] .theme-icon-dark { display: inline; }

/* ============================================================
   Visit Stats (Home page hero counter)
   ============================================================ */
.visit-stats__numbers {
    font-family: var(--font-mono);
    line-height: 1;
    margin-bottom: 0.5rem;
}

.visit-stats__count {
    font-size: 2.25rem;
    font-weight: 500;
    color: var(--color-primary);
}

.visit-stats__total {
    font-size: 1rem;
    color: var(--color-text-muted);
}

.visit-stats__bar {
    width: 180px;
    height: 5px;
    background: rgba(0, 0, 0, 0.1);
    border-radius: 3px;
    margin-bottom: 0.3rem;
    overflow: hidden;
}

[data-bs-theme="dark"] .visit-stats__bar {
    background: rgba(255, 255, 255, 0.12);
}

.visit-stats__fill {
    height: 100%;
    background: var(--color-accent);
    border-radius: 3px;
    transition: width 0.6s ease;
}

.visit-stats__label {
    font-family: var(--font-mono);
    font-size: 0.7rem;
    color: var(--color-text-muted);
    letter-spacing: 0.06em;
    text-transform: uppercase;
}

/* ============================================================
   Park List Enhancements
   ============================================================ */
.park-row--visited > td:first-child {
    border-left: 3px solid var(--color-accent);
    padding-left: calc(0.75rem - 3px);
}

.visited-badge {
    background-color: var(--color-accent) !important;
    color: #1b4332 !important;
    font-weight: 600;
}

.parks-tally {
    font-family: var(--font-mono);
    font-size: 0.85rem;
    color: var(--color-text-muted);
    letter-spacing: 0.03em;
}

.search-input {
    box-shadow: inset 0 1px 3px rgba(0, 0, 0, 0.06);
    font-size: 0.95rem;
    transition: box-shadow 0.15s ease, border-color 0.15s ease;
}

.search-input:focus {
    box-shadow: inset 0 1px 3px rgba(0, 0, 0, 0.06), 0 0 0 0.25rem var(--color-accent-dim);
    border-color: var(--color-primary);
}

/* ============================================================
   Modal Entrance Animation
   ============================================================ */
.modal.d-block .modal-dialog {
    animation: modal-slide-in 0.18s ease-out;
}

@keyframes modal-slide-in {
    from {
        opacity: 0;
        transform: translateY(-10px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}
```

- [ ] **Step 4: Build and verify**

```bash
cd .worktrees/feature-ui-redesign && dotnet build 2>&1 | tail -5
```

Expected:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

- [ ] **Step 5: Commit**

```bash
cd .worktrees/feature-ui-redesign && git add ParkTracker/Program.cs ParkTracker/Components/App.razor ParkTracker/wwwroot/app.css
git commit -m "feat: add CSS token system, DM Sans/Mono typography, and update CSP for Google Fonts"
```

---

## Task 2: Visit Counter Hero Stat

### Files
- Modify: `ParkTracker/Components/Pages/Home.razor`

- [ ] **Step 1: Add computed properties to @code block**

In `ParkTracker/Components/Pages/Home.razor`, add these two properties to the `@code` block, after the `private DotNetObjectReference<Home>? dotNetRef;` field (line 41):

```csharp
private int VisitedCount => parks.Count(p => p.Visits.Any());
private int ProgressPct => parks.Count > 0 ? VisitedCount * 100 / parks.Count : 0;
```

- [ ] **Step 2: Replace the counter div in the markup**

Replace lines 21–24 in `Home.razor`:

```html
<div class="mb-3 d-flex align-items-center justify-content-between">
    <span class="text-muted">Visited @parks.Count(p => p.Visits.Any()) of @parks.Count parks</span>
    <button class="btn btn-primary" @onclick="() => showModal = true">+ Add Visit</button>
</div>
```

With:

```html
<div class="mb-4 d-flex align-items-end justify-content-between">
    <div>
        <div class="visit-stats__numbers">
            <span class="visit-stats__count">@VisitedCount</span><span class="visit-stats__total"> / @parks.Count</span>
        </div>
        <div class="visit-stats__bar">
            <div class="visit-stats__fill" style="width: @ProgressPct%"></div>
        </div>
        <div class="visit-stats__label">@ProgressPct% explored</div>
    </div>
    <button class="btn btn-primary" @onclick="() => showModal = true">+ Add Visit</button>
</div>
```

- [ ] **Step 3: Build and verify**

```bash
cd .worktrees/feature-ui-redesign && dotnet build 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
cd .worktrees/feature-ui-redesign && git add ParkTracker/Components/Pages/Home.razor
git commit -m "feat: redesign visit counter as hero stat with progress bar"
```

---

## Task 3: Park List Improvements

### Files
- Modify: `ParkTracker/Components/Pages/ParkList.razor`

- [ ] **Step 1: Update the visit counter paragraph (line 17)**

Replace:
```html
<p class="text-muted">Visited @parks.Count(p => p.Visits.Any()) of @parks.Count parks</p>
```

With:
```html
<p class="parks-tally">Visited @parks.Count(p => p.Visits.Any()) of @parks.Count parks</p>
```

- [ ] **Step 2: Add search-input class to search box (line 22)**

Replace:
```html
           class="form-control"
```

With:
```html
           class="form-control search-input"
```

- [ ] **Step 3: Add visited row class and replace visited badge**

Replace the `<tr>` opening tag (line 43):
```html
                <tr>
```

With:
```html
                <tr class="@(hasVisits ? "park-row--visited" : "")">
```

Replace the visited `<span>` badge:
```html
                            <span class="badge bg-success">Visited</span>
```

With:
```html
                            <span class="badge visited-badge">Visited</span>
```

- [ ] **Step 4: Build and verify**

```bash
cd .worktrees/feature-ui-redesign && dotnet build 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
cd .worktrees/feature-ui-redesign && git add ParkTracker/Components/Pages/ParkList.razor
git commit -m "feat: add visited row accents, lime badge, and styled search input to park list"
```

---

## Task 4: SVG Map Markers

### Files
- Modify: `ParkTracker/wwwroot/js/leafletInterop.js`

- [ ] **Step 1: Replace circle icons with SVG pin markers**

In `leafletInterop.js`, replace the `addPins` function body (lines 27–59) with:

```javascript
    function addPins(parks, dotNetObjRef) {
        dotNetRef = dotNetObjRef;
        parks.forEach(function (park) {
            const fillColor = park.visited ? '#a8e063' : '#9ca3af';
            const strokeColor = park.visited ? '#1b4332' : '#6b7280';
            const icon = L.divIcon({
                className: '',
                html: `<svg width="20" height="30" viewBox="0 0 24 36" xmlns="http://www.w3.org/2000/svg" style="filter:drop-shadow(0 2px 3px rgba(0,0,0,0.3))"><path d="M12 0C5.373 0 0 5.373 0 12c0 9 12 24 12 24s12-15 12-24C24 5.373 18.627 0 12 0z" fill="${fillColor}" stroke="${strokeColor}" stroke-width="1.5"/><circle cx="12" cy="12" r="4" fill="white" opacity="0.85"/></svg>`,
                iconSize: [20, 30],
                iconAnchor: [10, 30],
                popupAnchor: [0, -32]
            });

            let popupHtml = `<strong>${escapeHtml(park.name)}</strong><br>${escapeHtml(park.state)}`;
            if (park.visited && park.visitDates && park.visitDates.length > 0) {
                const dates = park.visitDates
                    .map(d => d ? new Date(d).toLocaleDateString() : 'Date not recorded')
                    .join('<br>');
                popupHtml += `<br><em>Visited:</em><br>${dates}`;
            }
            popupHtml += `<br><button class="btn btn-sm btn-primary mt-2" onclick="leafletInterop.openAddVisit(${park.id})">+ Add Visit</button>`;

            const marker = L.marker([park.latitude, park.longitude], { icon })
                .addTo(map)
                .bindPopup(popupHtml);

            markers.push(marker);
        });
    }
```

- [ ] **Step 2: Build and verify**

```bash
cd .worktrees/feature-ui-redesign && dotnet build 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
cd .worktrees/feature-ui-redesign && git add ParkTracker/wwwroot/js/leafletInterop.js
git commit -m "feat: replace circle map markers with SVG pin icons using accent color palette"
```

---

## Task 5: Nav Logo + Active Link Accent

### Files
- Modify: `ParkTracker/Components/Layout/NavMenu.razor`
- Modify: `ParkTracker/Components/Layout/NavMenu.razor.css`

- [ ] **Step 1: Replace tree emoji with SVG mountain icon in NavMenu.razor**

Replace line 7 in `NavMenu.razor`:
```html
        <a class="navbar-brand" href="">&#127794; ParkTracker</a>
```

With:
```html
        <a class="navbar-brand" href="">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true" style="vertical-align:-2px;margin-right:5px"><path d="M2 20L8 9l3.5 5.5L14 8l8 12H2z" fill="currentColor"/></svg>ParkTracker</a>
```

- [ ] **Step 2: Update active nav link indicator color in NavMenu.razor.css**

Replace in `NavMenu.razor.css` line 23:
```css
    border-bottom: 2px solid #74c69d;
```

With:
```css
    border-bottom: 2px solid var(--color-accent);
```

- [ ] **Step 3: Build and verify**

```bash
cd .worktrees/feature-ui-redesign && dotnet build 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
cd .worktrees/feature-ui-redesign && git add ParkTracker/Components/Layout/NavMenu.razor ParkTracker/Components/Layout/NavMenu.razor.css
git commit -m "feat: replace nav tree emoji with SVG mountain icon and update active link accent"
```

---

## Task 6: Modal Polish — Entrance Animation + Destructive Button

### Files
- Modify: `ParkTracker/Components/Shared/EditVisitModal.razor`

The modal entrance animation is already in `app.css` (added in Task 1). This task only needs the button layout change.

- [ ] **Step 1: Move "Remove Visit" button to the left in EditVisitModal**

Replace lines 34–38 in `EditVisitModal.razor`:
```html
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" @onclick="OnClose">Cancel</button>
                    <button type="button" class="btn btn-outline-danger" @onclick="HandleDelete" disabled="@saving">Remove Visit</button>
                    <button type="submit" class="btn btn-primary" disabled="@saving">Update Visit</button>
                </div>
```

With:
```html
                <div class="modal-footer">
                    <button type="button" class="btn btn-outline-danger me-auto" @onclick="HandleDelete" disabled="@saving">Remove Visit</button>
                    <button type="button" class="btn btn-secondary" @onclick="OnClose">Cancel</button>
                    <button type="submit" class="btn btn-primary" disabled="@saving">Update Visit</button>
                </div>
```

- [ ] **Step 2: Build and verify**

```bash
cd .worktrees/feature-ui-redesign && dotnet build 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
cd .worktrees/feature-ui-redesign && git add ParkTracker/Components/Shared/EditVisitModal.razor
git commit -m "feat: separate Remove Visit button from safe actions in edit modal"
```

---

## Verification

After all tasks are complete, run the app and check each area visually:

```bash
cd .worktrees/feature-ui-redesign/ParkTracker && dotnet run
```

Checklist:
- [ ] DM Sans font loads (check Network tab — `fonts.gstatic.com` request succeeds)
- [ ] Home page visit counter shows large monospaced fraction with lime progress bar
- [ ] Park list visited rows have a lime left border accent
- [ ] Visited badge is lime-green with dark text (not Bootstrap green)
- [ ] Search input has lime focus ring instead of blue
- [ ] Map markers are pointed SVG pins (lime for visited, gray for unvisited)
- [ ] Clicking a map pin shows popup positioned above the pin (not overlapping it)
- [ ] Navbar brand shows the mountain SVG icon
- [ ] Active nav link underline is lime (`#a8e063`), not teal
- [ ] Opening AddVisitModal or EditVisitModal shows a subtle slide-down entrance animation
- [ ] EditVisitModal footer: "Remove Visit" is on the left, "Cancel" and "Update Visit" on the right
- [ ] Dark mode: toggle works, background grain absent, colors shift correctly
- [ ] Body background has subtle warm off-white tint (`#f5f5f2`) in light mode
