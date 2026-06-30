var PortalBridge = {
    Portal_Request: function(requestJsonPtr) {
        try {
            var requestJson = UTF8ToString(requestJsonPtr);
            var message = JSON.parse(requestJson);

            if (!message.source) {
                message.source = "setterlun-unity";
            }

            if (!window.parent || window.parent === window) {
                console.warn("[PortalBridge] No parent portal window is available.");
                return;
            }

            window.parent.postMessage(message, "*");
        } catch (error) {
            console.error("[PortalBridge] Portal_Request failed:", error);
        }
    },

    Portal_NotifyReady: function() {
        try {
            if (!window.parent || window.parent === window) {
                return;
            }

            window.parent.postMessage({
                source: "setterlun-unity",
                type: "unity.ready",
                payload: {}
            }, "*");
        } catch (error) {
            console.error("[PortalBridge] Portal_NotifyReady failed:", error);
        }
    },

    Portal_Logout: function() {
        try {
            if (!window.parent || window.parent === window) {
                return;
            }

            window.parent.postMessage({
                source: "setterlun-unity",
                type: "backend.request",
                requestId: "logout_" + Date.now(),
                op: "logout",
                payload: {}
            }, "*");
        } catch (error) {
            console.error("[PortalBridge] Portal_Logout failed:", error);
        }
    }
};

mergeInto(LibraryManager.library, PortalBridge);
