window.getTheme = () => document.documentElement.getAttribute('data-bs-theme') ?? 'light';

window.setTheme = (theme) => {
    if (theme !== 'light' && theme !== 'dark') return;
    localStorage.setItem('theme', theme); // Before setAttribute so observer reads correct value
    document.documentElement.setAttribute('data-bs-theme', theme);
};

window.toggleTheme = () => {
    const next = window.getTheme() === 'dark' ? 'light' : 'dark';
    window.setTheme(next);
    const label = next === 'dark' ? 'Switch to light mode' : 'Switch to dark mode';
    document.querySelectorAll('.theme-toggle-btn').forEach(btn => {
        btn.setAttribute('aria-label', label);
        btn.setAttribute('title', label);
    });
};

// Reads intent from localStorage, not from the DOM, to avoid a feedback loop with the observer.
function resolveStoredTheme() {
    const stored = localStorage.getItem('theme');
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    return (stored === 'light' || stored === 'dark') ? stored : (prefersDark ? 'dark' : 'light');
}

// Restore theme whenever Blazor Enhanced Navigation or anything else strips data-bs-theme from <html>.
// localStorage is written before setAttribute in setTheme, so this observer never incorrectly
// reverts a user-initiated toggle.
window._themeObserver = new MutationObserver(() => {
    const expected = resolveStoredTheme();
    if (document.documentElement.getAttribute('data-bs-theme') !== expected) {
        document.documentElement.setAttribute('data-bs-theme', expected);
    }
});
window._themeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['data-bs-theme'] });

// Set correct initial aria-label/title on the toggle button.
// ThemeToggle.razor renders before this script loads, so the button is already in the DOM.
(function () {
    const theme = window.getTheme();
    const label = theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode';
    document.querySelectorAll('.theme-toggle-btn').forEach(btn => {
        btn.setAttribute('aria-label', label);
        btn.setAttribute('title', label);
    });
}());
