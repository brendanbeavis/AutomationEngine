// Theme Manager - Handles theme switching, persistence, and application
(function () {
    window.applyTheme = function (theme) {
        // Validate theme
        const validThemes = ['cyberpunk', 'dark', 'light'];
        if (!validThemes.includes(theme)) {
            console.warn('Invalid theme:', theme);
            theme = 'cyberpunk';
        }

        // Remove all theme classes
        document.documentElement.classList.remove('theme-light', 'theme-dark', 'theme-cyberpunk');

        // Apply new theme class if not default cyberpunk
        if (theme === 'light') {
            document.documentElement.classList.add('theme-light');
        } else if (theme === 'dark') {
            document.documentElement.classList.add('theme-dark');
        }
        // cyberpunk is default, no class needed

        // Save to localStorage
        try {
            localStorage.setItem('automation-engine-theme', theme);
        } catch (e) {
            console.warn('Failed to save theme to localStorage:', e);
        }
    };

    window.getCurrentTheme = function () {
        try {
            return localStorage.getItem('automation-engine-theme') || 'cyberpunk';
        } catch (e) {
            console.warn('Failed to retrieve theme from localStorage:', e);
            return 'cyberpunk';
        }
    };

    window.initializeTheme = function () {
        const savedTheme = window.getCurrentTheme();
        window.applyTheme(savedTheme);
    };

    // Initialize theme on script load
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', window.initializeTheme);
    } else {
        window.initializeTheme();
    }
})();
