export function scrollToBottom(element) {
    if (!element) {
        return;
    }

    element.scrollTop = element.scrollHeight;
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
