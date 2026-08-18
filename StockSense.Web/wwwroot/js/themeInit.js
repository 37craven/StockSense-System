(function() {
    var intendedDark = localStorage.getItem('theme') === 'dark';

    function applyTheme() {
        intendedDark = localStorage.getItem('theme') === 'dark';
        if (intendedDark) {
            document.documentElement.classList.add('dark');
        } else {
            document.documentElement.classList.remove('dark');
        }
    }

    applyTheme();
    // .NET 8 Enhanced Navigation theme hook
    document.addEventListener('blazor:enhancedload', applyTheme);

    const observer = new MutationObserver(function() {
        if (intendedDark && !document.documentElement.classList.contains('dark')) {
            document.documentElement.classList.add('dark');
        }
    });

    observer.observe(document.documentElement, {
        attributes: true,
        attributeFilter: ['class']
    });

    window.setTheme = function(dark) {
        intendedDark = dark;
        localStorage.setItem('theme', dark ? 'dark' : 'light');
        if (dark) {
            document.documentElement.classList.add('dark');
        } else {
            document.documentElement.classList.remove('dark');
        }
    };
})();