(function () {
    if (window.stockSensePasswordVisibilityInitialized) {
        return;
    }

    window.stockSensePasswordVisibilityInitialized = true;

    document.addEventListener('click', function (event) {
        const button = event.target.closest('[data-password-toggle]');
        if (!button) {
            return;
        }

        const targetId = button.getAttribute('data-password-target');
        const input = targetId ? document.getElementById(targetId) : null;
        if (!input || (input.type !== 'password' && input.type !== 'text')) {
            return;
        }

        const isVisible = input.type === 'text';
        input.type = isVisible ? 'password' : 'text';
        button.setAttribute('aria-pressed', String(!isVisible));

        const showIcon = button.querySelector('[data-password-icon-show]');
        const hideIcon = button.querySelector('[data-password-icon-hide]');
        showIcon?.classList.toggle('hidden', !isVisible);
        hideIcon?.classList.toggle('hidden', isVisible);

        const fieldName = button.getAttribute('data-password-label')
            || (targetId.includes('confirm') ? 'confirm password' : 'password');
        button.setAttribute('aria-label', `${isVisible ? 'Show' : 'Hide'} ${fieldName}`);
    });
})();
