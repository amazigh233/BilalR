(function () {
    "use strict";

    // Derive the Zambiq host from this script's own URL, so the embed snippet is host-agnostic:
    // the iframe always points to the same origin that served widget.js.
    var currentScript = document.currentScript;
    if (!currentScript) {
        var scripts = document.getElementsByTagName("script");
        for (var s = scripts.length - 1; s >= 0; s--) {
            if (scripts[s].src && scripts[s].src.indexOf("widget.js") !== -1) {
                currentScript = scripts[s];
                break;
            }
        }
    }

    var widgetOrigin = currentScript
        ? new URL(currentScript.src, window.location.href).origin
        : window.location.origin;

    var PROCESSED_ATTR = "data-zambiq-processed";
    var DEFAULT_HEIGHT = 720;

    function isValidGuid(value) {
        return /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(value);
    }

    function renderWidget(container) {
        if (container.getAttribute(PROCESSED_ATTR) === "true") {
            return;
        }

        var restaurantId = container.getAttribute("data-zambiq-restaurant");
        if (!restaurantId || !isValidGuid(restaurantId)) {
            container.setAttribute(PROCESSED_ATTR, "true");
            container.textContent = "Zambiq: ongeldig of ontbrekend restaurant-id.";
            return;
        }

        var height = parseInt(container.getAttribute("data-height"), 10);
        if (isNaN(height) || height <= 0) {
            height = DEFAULT_HEIGHT;
        }

        var iframe = document.createElement("iframe");
        iframe.src = widgetOrigin + "/embed/booking/" + encodeURIComponent(restaurantId);
        iframe.title = "Reserveren";
        iframe.loading = "lazy";
        iframe.style.width = "100%";
        iframe.style.height = height + "px";
        iframe.style.border = "0";
        iframe.setAttribute("frameborder", "0");
        iframe.setAttribute("allowtransparency", "true");

        container.setAttribute(PROCESSED_ATTR, "true");
        container.innerHTML = "";
        container.appendChild(iframe);
    }

    function renderAll() {
        var containers = document.querySelectorAll("[data-zambiq-restaurant]");
        for (var i = 0; i < containers.length; i++) {
            renderWidget(containers[i]);
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", renderAll);
    } else {
        renderAll();
    }

    // Expose a manual trigger for dynamically added containers (e.g. SPA navigation).
    window.ZambiqWidget = { render: renderAll };
})();
