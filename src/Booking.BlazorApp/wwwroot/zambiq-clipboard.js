window.ZambiqClipboard = {
    copy: function (text) {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            return navigator.clipboard.writeText(text).then(
                function () { return true; },
                function () { return fallbackCopy(text); }
            );
        }

        return fallbackCopy(text);
    }
};

function fallbackCopy(text) {
    var textArea = document.createElement("textarea");
    textArea.value = text;
    textArea.setAttribute("readonly", "");
    textArea.style.position = "fixed";
    textArea.style.opacity = "0";
    document.body.appendChild(textArea);
    textArea.select();

    var copied = false;
    try {
        copied = document.execCommand("copy");
    } catch (_) {
        copied = false;
    }

    document.body.removeChild(textArea);
    return copied;
}
