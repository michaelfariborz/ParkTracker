window.getTheme = () => document.documentElement.getAttribute('data-bs-theme') ?? 'light';

window.setTheme = (theme) => {
    if (theme !== 'light' && theme !== 'dark') return;
    document.documentElement.setAttribute('data-bs-theme', theme);
    localStorage.setItem('theme', theme);
};

window.toggleTheme = () => {
    const next = window.getTheme() === 'dark' ? 'light' : 'dark';
    window.setTheme(next);
};
