(function () {
    "use strict";

    if (window.parent === window || !window.location.pathname.toLowerCase().startsWith("/embed/booking/")) {
        return;
    }

    var MESSAGE_TYPE = "zambiq:widget:resize";
    var lastHeight = 0;
    var scheduled = false;

    function measureHeight() {
        if (!document.body) {
            return document.documentElement.scrollHeight;
        }

        return Math.max(document.body.scrollHeight, document.body.offsetHeight) + 2;
    }

    function sendHeight() {
        scheduled = false;
        var height = Math.ceil(measureHeight());
        if (height <= 0 || height === lastHeight) {
            return;
        }

        lastHeight = height;
        window.parent.postMessage({ type: MESSAGE_TYPE, height: height }, "*");
    }

    function scheduleHeight() {
        if (scheduled) {
            return;
        }

        scheduled = true;
        window.requestAnimationFrame(sendHeight);
    }

    window.addEventListener("load", scheduleHeight);
    window.addEventListener("resize", scheduleHeight);
    document.addEventListener("DOMContentLoaded", scheduleHeight);
    document.documentElement.classList.add("zambiq-embed-document");
    if (document.body) {
        document.body.classList.add("zambiq-embed-body");
    }

    if (window.ResizeObserver) {
        new ResizeObserver(scheduleHeight).observe(document.documentElement);
    }

    if (window.MutationObserver) {
        new MutationObserver(scheduleHeight).observe(document.documentElement, {
            attributes: true,
            childList: true,
            subtree: true
        });
    }

    scheduleHeight();
})();
