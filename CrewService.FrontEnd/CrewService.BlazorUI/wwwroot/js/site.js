document.addEventListener("submit", function (e) {
    var form = e.target;

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

