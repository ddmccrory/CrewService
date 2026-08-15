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

(function initializeClientErrorCapture() {
    if (window.__crewClientErrorCaptureInitialized) {
        return;
    }

    window.__crewClientErrorCaptureInitialized = true;

    var recentKeys = new Map();
    var dedupeWindowMs = 10000;

    function shouldCapture(key) {
        var now = Date.now();
        var last = recentKeys.get(key);
        recentKeys.set(key, now);
        return !last || (now - last) > dedupeWindowMs;
    }

    function postClientError(payload) {
        try {
            fetch("/internal/client-errors", {
                method: "POST",
                credentials: "include",
                keepalive: true,
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(payload)
            });
        } catch (_ignored) {
            // Avoid recursive failures from the capture pipeline itself.
        }
    }

    window.addEventListener("error", function (event) {
        var message = event && event.message ? event.message : "Unhandled browser error.";
        var stack = event && event.error && event.error.stack ? String(event.error.stack) : null;
        var key = "error|" + message + "|" + (event && event.filename ? event.filename : "");

        if (!shouldCapture(key)) {
            return;
        }

        postClientError({
            sourceApp: "BlazorWasm",
            sourceLayer: "BrowserRuntime",
            severity: "Error",
            errorCode: "CLIENT_WINDOW_ERROR",
            errorKind: "ClientRuntime",
            message: message,
            exceptionType: event && event.error && event.error.name ? String(event.error.name) : "WindowError",
            stackTrace: stack,
            url: window.location.pathname,
            method: "window.onerror",
            userAgent: navigator.userAgent,
            metadata: {
                fileName: event && event.filename ? String(event.filename) : null,
                lineNumber: event && typeof event.lineno === "number" ? String(event.lineno) : null,
                columnNumber: event && typeof event.colno === "number" ? String(event.colno) : null
            }
        });
    });

    window.addEventListener("unhandledrejection", function (event) {
        var reason = event ? event.reason : null;
        var reasonText = reason && reason.message ? String(reason.message) : String(reason);
        var stack = reason && reason.stack ? String(reason.stack) : null;
        var key = "rejection|" + reasonText;

        if (!shouldCapture(key)) {
            return;
        }

        postClientError({
            sourceApp: "BlazorWasm",
            sourceLayer: "BrowserRuntime",
            severity: "Error",
            errorCode: "CLIENT_UNHANDLED_REJECTION",
            errorKind: "ClientRuntime",
            message: reasonText || "Unhandled promise rejection.",
            exceptionType: reason && reason.name ? String(reason.name) : "UnhandledPromiseRejection",
            stackTrace: stack,
            url: window.location.pathname,
            method: "window.unhandledrejection",
            userAgent: navigator.userAgent
        });
    });
})();

