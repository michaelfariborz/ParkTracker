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
