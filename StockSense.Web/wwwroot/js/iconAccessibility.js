// LucideIcon renders role="img" without a text alternative. Give every icon
// an accessible outcome:
//   - icons with aria-label stay announced
//   - icon-only buttons/links with a title get an accessible name from it
//   - everything else is decorative -> hidden from assistive tech
(function() {
    function hasAccessibleName(el) {
        return !!(el.getAttribute('aria-label') || el.getAttribute('aria-labelledby') ||
                  (el.textContent || '').trim());
    }
    function interactiveHost(el) {
        var n = el.parentElement;
        while (n && n !== document.body) {
            if (n.matches('button, a, [role="button"], [role="link"], summary')) return n;
            n = n.parentElement;
        }
        return null;
    }
    function fixIcons(root) {
        if (!root || !root.querySelectorAll) return;
        root.querySelectorAll('svg.lucide-icon').forEach(function(svg) {
            if (svg.getAttribute('aria-label')) {
                svg.removeAttribute('aria-hidden');
                return;
            }
            var host = interactiveHost(svg);
            if (host && !hasAccessibleName(host) && host.getAttribute('title')) {
                host.setAttribute('aria-label', host.getAttribute('title'));
            }
            svg.setAttribute('aria-hidden', 'true');
        });
    }
    fixIcons(document);
    document.addEventListener('blazor:enhancedload', function() { fixIcons(document); });
    new MutationObserver(function(mutations) {
        mutations.forEach(function(m) {
            m.addedNodes.forEach(function(n) {
                if (n.nodeType === 1) fixIcons(n);
            });
        });
    }).observe(document.body, { childList: true, subtree: true });
})();