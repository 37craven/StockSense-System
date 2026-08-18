// Aggressive .NET 8 Title Enforcer
(function() {
    var titleMap = {
        '/': 'Home',
        '/Dashboard': 'Dashboard',
        '/assistance': 'Assistance',
        '/auth': 'Auth',
        '/my-bookings': 'My Appointments',
        '/appointment': 'Book Appointment',
        '/my-builds': 'My Builds',
        '/build': 'Browse All Parts',
        '/admin/management': 'System Management',
        '/admin/prebuilts': 'Manage Pre-Built Packages',
        '/admin/services': 'Manage Services',
        '/admin/transactions': 'Transaction History',
        '/admin/stock': 'Safety Stock',
        '/admin/assistance': 'Assistance',
        '/admin/checkout-terminal': 'Checkout Terminal',
        '/admin/orderslips': 'Generate Orders',
        '/admin/order-history': 'Order History',
        '/admin/appointments': 'Manage Appointments',
        '/admin/builds': 'Manage Builds',
        '/Error': 'Error'
    };

    var paramPatterns = [
        [/^\/admin\/order-slips\/\d+\/receive$/, 'Receive Order Slip'],
        [/^\/admin\/order-slips\/\d+$/, 'Order Slip Details']
    ];

    function getTitle(path) {
        var t = titleMap[path];
        if (t) return t;
        for (var i = 0; i < paramPatterns.length; i++) {
            if (paramPatterns[i][0].test(path)) return paramPatterns[i][1];
        }
        return null;
    }

    function enforceTitle() {
        var expectedTitle = getTitle(window.location.pathname);
        if (expectedTitle && document.title !== expectedTitle) {
            document.title = expectedTitle;
        }
    }

    // 1. Hook into standard browser navigation
    window.addEventListener('popstate', enforceTitle);

    // 2. Hook into .NET 8 Blazor Enhanced Navigation
    document.addEventListener('blazor:enhancedload', enforceTitle);

    // 3. Force override if Blazor quietly injects the raw URL into the DOM
    new MutationObserver(function() {
        enforceTitle();
    }).observe(document.head, {
        childList: true,
        characterData: true,
        subtree: true
    });

    // Initial load
    enforceTitle();
})();