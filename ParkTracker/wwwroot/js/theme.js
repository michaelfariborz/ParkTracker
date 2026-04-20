window.getTheme = () => document.documentElement.getAttribute('data-bs-theme') ?? 'light';

window.setTheme = (theme) => {
    if (theme !== 'light' && theme !== 'dark') return;
    document.documentElement.setAttribute('data-bs-theme', theme);
    localStorage.setItem('theme', theme);
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

// Re-apply theme after Blazor Enhanced Navigation strips data-bs-theme from <html>
document.addEventListener('blazor:navigated', () => {
    const stored = localStorage.getItem('theme');
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    const theme = (stored === 'light' || stored === 'dark') ? stored : (prefersDark ? 'dark' : 'light');
    document.documentElement.setAttribute('data-bs-theme', theme);
});
