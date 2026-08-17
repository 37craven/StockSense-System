(function () {
    if (window.stockSenseSettingsNavInitialized) return;
    window.stockSenseSettingsNavInitialized = true;

    document.addEventListener("click", function (event) {
        const button = event.target.closest("[data-settings-theme-toggle]");
        if (!button) return;

        const enableDark = !document.documentElement.classList.contains("dark");
        window.setTheme(enableDark);
        button.setAttribute("aria-label", enableDark ? "Switch to light mode" : "Switch to dark mode");
        button.setAttribute("title", enableDark ? "Switch to light mode" : "Switch to dark mode");
    });
})();
