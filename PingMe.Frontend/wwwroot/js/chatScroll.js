(function () {
    const getElement = function (selectorOrId) {
        if (!selectorOrId) {
            return null;
        }

        if (selectorOrId instanceof HTMLElement) {
            return selectorOrId;
        }

        const value = selectorOrId.toString();

        return document.getElementById(value) || document.querySelector(value);
    };

    const resizeTextarea = function (el) {
        if (!el) {
            return;
        }

        el.style.height = "32px";
        const nextHeight = Math.min(el.scrollHeight, 156);
        el.style.height = nextHeight + "px";
        el.style.overflowY = el.scrollHeight > 156 ? "auto" : "hidden";
    };

    const api = {
        getScrollHeight: function (containerId) {
            const el = document.getElementById(containerId);
            return el ? el.scrollHeight : 0;
        },

        restoreScrollAfterPrepend: function (containerId, oldScrollHeight) {
            requestAnimationFrame(() => {
                const el = document.getElementById(containerId);
                if (!el) {
                    return;
                }

                const delta = el.scrollHeight - (oldScrollHeight || 0);
                el.scrollTop = el.scrollTop + delta;
            });
        },

        isNearBottom: function (containerId, threshold) {
            const el = document.getElementById(containerId);
            if (!el) {
                return true;
            }

            const limit = typeof threshold === "number" ? threshold : 160;
            return el.scrollHeight - el.scrollTop - el.clientHeight <= limit;
        },

        scrollToBottom: function (containerId) {
            const el = document.getElementById(containerId);
            if (!el) {
                return false;
            }

            el.scrollTop = el.scrollHeight;
            return true;
        },

        scrollToBottomWithRetry: function (containerId) {
            const scroll = () => {
                const el = document.getElementById(containerId);
                if (!el) {
                    return;
                }

                el.scrollTop = el.scrollHeight;
            };

            const delays = [0, 50, 150, 300, 600];
            delays.forEach(delay => {
                window.setTimeout(() => {
                    requestAnimationFrame(scroll);
                }, delay);
            });

            requestAnimationFrame(() => {
                const el = document.getElementById(containerId);
                if (!el) {
                    return;
                }

                el.querySelectorAll("img").forEach(img => {
                    if (img.complete) {
                        return;
                    }

                    img.addEventListener("load", scroll, { once: true });
                    img.addEventListener("error", scroll, { once: true });
                });
            });
        },

        scrollElementIntoView: function (elementId, block) {
            window.setTimeout(() => {
                const el = document.getElementById(elementId);
                if (!el) {
                    return false;
                }

                el.scrollIntoView({ behavior: "smooth", block: block || "center" });
                return true;
            }, 60);
        },

        focusTextareaToEnd: function (selectorOrId) {
            requestAnimationFrame(() => {
                const el = getElement(selectorOrId);
                if (!el) {
                    return;
                }

                el.focus();
                const pos = el.value ? el.value.length : 0;

                if (typeof el.setSelectionRange === "function") {
                    el.setSelectionRange(pos, pos);
                }
            });
        },

        getTextareaCursorPosition: function (selectorOrId) {
            const el = getElement(selectorOrId);
            return el && typeof el.selectionStart === "number" ? el.selectionStart : 0;
        },

        autoResizeTextarea: function (selectorOrId) {
            requestAnimationFrame(() => resizeTextarea(getElement(selectorOrId)));
        }
    };

    window.pingmeChatScroll = api;
    window.pingmeChat = Object.assign(window.pingmeChat || {}, api);
})();
