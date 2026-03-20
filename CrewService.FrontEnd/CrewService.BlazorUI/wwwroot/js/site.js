document.addEventListener("submit", function (e) {
    // Skip forms already handled by Blazor's interactive runtime or enhanced navigation
    if (e.defaultPrevented) return;

    var form = e.target;

    // Only process SSR forms that have an explicit method attribute;
    // interactive Blazor EditForm components render without method/action
    if (!form.hasAttribute("method")) return;

    // 1. Confirmation dialog — abort if cancelled
    if (form.hasAttribute("data-confirm")) {
        var message = form.getAttribute("data-confirm") || "Are you sure you want to delete this?";
        if (!confirm(message)) {
            e.preventDefault();
            return;
        }
    }

    // 2. Disable every submit button in the form to prevent double-clicks
    var buttons = form.querySelectorAll('button[type="submit"]');
    for (var i = 0; i < buttons.length; i++) {
        buttons[i].disabled = true;
    }
});

