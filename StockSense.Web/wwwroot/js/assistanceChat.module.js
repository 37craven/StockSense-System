export function scrollToBottom(element) {
    if (!element) {
        return;
    }

    var attempts = 0;
    var maxAttempts = 10;
    var lastHeight = 0;

    function scroll() {
        if (attempts >= maxAttempts) {
            return;
        }
        var currentHeight = element.scrollHeight;
        element.scrollTop = currentHeight;
        attempts++;
        if (currentHeight !== lastHeight) {
            lastHeight = currentHeight;
            requestAnimationFrame(scroll);
        }
    }

    requestAnimationFrame(scroll);
}

export function registerChatComposer(textarea, sendButton) {
    if (!textarea || !sendButton || textarea.dataset.stockSenseComposer === "true") {
        return;
    }

    textarea.dataset.stockSenseComposer = "true";
    textarea.addEventListener("keydown", function (event) {
        if (event.key === "Enter" && !event.shiftKey && !event.isComposing) {
            event.preventDefault();
            if (!sendButton.disabled) {
                sendButton.click();
            }
        }
    });
}
