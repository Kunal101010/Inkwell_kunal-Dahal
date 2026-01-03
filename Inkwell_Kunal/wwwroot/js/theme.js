window.theme = {
    setTheme: function (theme) {
        try {
            localStorage.setItem('theme', theme);
            document.documentElement.classList.remove('light', 'dark', 'custom');
            document.documentElement.classList.add(theme);
        } catch (e) { console.error(e); }
    },
    getTheme: function () {
        try { return localStorage.getItem('theme') || 'light'; } catch (e) { return 'light'; }
    },
    setAccent: function (hex) {
        try {
            localStorage.setItem('accent', hex);
            document.documentElement.style.setProperty('--accent', hex);
        } catch (e) { console.error(e); }
    },
    getAccent: function () {
        try { return localStorage.getItem('accent') || ''; } catch (e) { return ''; }
    },
    apply: function (theme, accent) {
        try {
            if (accent) document.documentElement.style.setProperty('--accent', accent);
            document.documentElement.classList.remove('light', 'dark', 'custom');
            document.documentElement.classList.add(theme);
        } catch (e) { console.error(e); }
    }
};
