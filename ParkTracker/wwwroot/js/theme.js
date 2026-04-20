window.getTheme = () => document.documentElement.getAttribute('data-bs-theme') ?? 'light';

window.setTheme = (theme) => {
    document.documentElement.setAttribute('data-bs-theme', theme);
    localStorage.setItem('theme', theme);
};
