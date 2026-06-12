(function () {
    "use strict";

    var currentScript = document.currentScript;
    if (!currentScript) {
        var scripts = document.getElementsByTagName("script");
        for (var scriptIndex = scripts.length - 1; scriptIndex >= 0; scriptIndex--) {
            if (scripts[scriptIndex].src && scripts[scriptIndex].src.indexOf("widget.js") !== -1) {
                currentScript = scripts[scriptIndex];
                break;
            }
        }
    }

    var widgetOrigin = currentScript
        ? new URL(currentScript.src, window.location.href).origin
        : window.location.origin;

    var PROCESSED_ATTR = "data-zambiq-processed";
    var DEFAULT_HEIGHT = 720;
    var MIN_HEIGHT = 320;
    var MAX_HEIGHT = 5000;
    var RESIZE_MESSAGE = "zambiq:widget:resize";
    var widgetSequence = 0;
    var widgets = [];

    function isValidGuid(value) {
        return /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(value);
    }

    function readHeight(container) {
        var height = parseInt(container.getAttribute("data-height"), 10);
        if (isNaN(height) || height < MIN_HEIGHT) {
            return DEFAULT_HEIGHT;
        }

        return Math.min(height, MAX_HEIGHT);
    }

    function autoResizeEnabled(container) {
        return container.getAttribute("data-auto-resize") !== "false";
    }

    function setContainerState(container, state, message) {
        container.setAttribute("data-zambiq-state", state);
        container.style.position = "relative";
        container.style.width = "100%";
        container.style.maxWidth = "100%";

        if (message) {
            container.innerHTML = "";
            var status = document.createElement("p");
            status.textContent = message;
            status.style.margin = "0";
            status.style.padding = "1rem";
            status.style.color = "#66756e";
            status.style.background = "#faf9f6";
            status.style.border = "1px solid #e1e6e2";
            status.style.borderRadius = "12px";
            status.style.fontFamily = "system-ui, sans-serif";
            status.style.fontSize = "14px";
            container.appendChild(status);
        }
    }

    function resizeWidget(widget, requestedHeight) {
        if (!widget.autoResize) {
            return;
        }

        var height = Math.ceil(Number(requestedHeight));
        if (!isFinite(height) || height < MIN_HEIGHT) {
            return;
        }

        height = Math.min(height, MAX_HEIGHT);
        if (widget.height === height) {
            return;
        }

        widget.height = height;
        widget.iframe.style.height = height + "px";
    }

    function renderWidget(container) {
        if (!container || container.getAttribute(PROCESSED_ATTR) === "true") {
            return;
        }

        var restaurantId = container.getAttribute("data-zambiq-restaurant");
        if (!restaurantId || !isValidGuid(restaurantId)) {
            container.setAttribute(PROCESSED_ATTR, "true");
            setContainerState(container, "error", "Zambiq: ongeldig of ontbrekend restaurant-id.");
            return;
        }

        var fallbackHeight = readHeight(container);
        var shouldAutoResize = autoResizeEnabled(container);
        var title = container.getAttribute("data-title") || "Reserveren";
        var widgetId = "zambiq-widget-" + (++widgetSequence);

        var iframe = document.createElement("iframe");
        iframe.src = widgetOrigin + "/embed/booking/" + encodeURIComponent(restaurantId);
        iframe.title = title;
        iframe.loading = "lazy";
        iframe.style.display = "block";
        iframe.style.width = "100%";
        iframe.style.height = fallbackHeight + "px";
        iframe.style.border = "0";
        iframe.style.background = "transparent";
        iframe.style.transition = "height 180ms ease";
        iframe.setAttribute("frameborder", "0");
        iframe.setAttribute("scrolling", shouldAutoResize ? "no" : "auto");
        iframe.setAttribute("allowtransparency", "true");
        iframe.setAttribute("data-zambiq-widget-id", widgetId);

        var fallback = document.createElement("a");
        fallback.href = widgetOrigin + "/booking/" + encodeURIComponent(restaurantId);
        fallback.target = "_blank";
        fallback.rel = "noopener";
        fallback.textContent = "Reserveren bij " + title;

        container.setAttribute(PROCESSED_ATTR, "true");
        setContainerState(container, "loading");
        container.innerHTML = "";
        container.appendChild(iframe);
        container.appendChild(fallback);
        fallback.style.display = "none";

        var widget = {
            autoResize: shouldAutoResize,
            container: container,
            height: fallbackHeight,
            iframe: iframe,
            id: widgetId
        };
        widgets.push(widget);

        iframe.addEventListener("load", function () {
            container.setAttribute("data-zambiq-state", "ready");
        });
    }

    function renderAll(root) {
        var scope = root && root.querySelectorAll ? root : document;
        if (scope.matches && scope.matches("[data-zambiq-restaurant]")) {
            renderWidget(scope);
        }

        var containers = scope.querySelectorAll("[data-zambiq-restaurant]");
        for (var containerIndex = 0; containerIndex < containers.length; containerIndex++) {
            renderWidget(containers[containerIndex]);
        }
    }

    window.addEventListener("message", function (event) {
        if (event.origin !== widgetOrigin || !event.data || event.data.type !== RESIZE_MESSAGE) {
            return;
        }

        for (var widgetIndex = 0; widgetIndex < widgets.length; widgetIndex++) {
            var widget = widgets[widgetIndex];
            if (event.source === widget.iframe.contentWindow) {
                resizeWidget(widget, event.data.height);
                return;
            }
        }
    });

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", function () { renderAll(document); });
    } else {
        renderAll(document);
    }

    if (window.MutationObserver) {
        var observer = new MutationObserver(function (mutations) {
            for (var mutationIndex = 0; mutationIndex < mutations.length; mutationIndex++) {
                var addedNodes = mutations[mutationIndex].addedNodes;
                for (var nodeIndex = 0; nodeIndex < addedNodes.length; nodeIndex++) {
                    if (addedNodes[nodeIndex].nodeType === 1) {
                        renderAll(addedNodes[nodeIndex]);
                    }
                }
            }
        });

        observer.observe(document.documentElement, { childList: true, subtree: true });
    }

    window.ZambiqWidget = {
        origin: widgetOrigin,
        render: renderAll
    };
})();
