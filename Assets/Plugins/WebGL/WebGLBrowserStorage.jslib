mergeInto(LibraryManager.library, {
    Lutuoan_SaveToLocalStorage: function (keyPtr, valuePtr) {
        try {
            var key = UTF8ToString(keyPtr);
            var val = UTF8ToString(valuePtr);
            if (typeof window !== "undefined" && window.localStorage) {
                window.localStorage.setItem(key, val);
                return 1;
            }
        } catch (e) {}
        return 0;
    },

    Lutuoan_LoadFromLocalStorage: function (keyPtr) {
        try {
            var key = UTF8ToString(keyPtr);
            if (typeof window !== "undefined" && window.localStorage) {
                var val = window.localStorage.getItem(key);
                if (val === null || val === undefined) return 0;
                var bufferSize = lengthBytesUTF8(val) + 1;
                var buffer = _malloc(bufferSize);
                stringToUTF8(val, buffer, bufferSize);
                return buffer;
            }
        } catch (e) {}
        return 0;
    },

    Lutuoan_RemoveFromLocalStorage: function (keyPtr) {
        try {
            var key = UTF8ToString(keyPtr);
            if (typeof window !== "undefined" && window.localStorage) {
                window.localStorage.removeItem(key);
            }
        } catch (e) {}
    },

    Lutuoan_IsMobileBrowser: function () {
        try {
            if (typeof navigator === "undefined") return 0;
            var ua = navigator.userAgent || "";
            if (/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(ua)) {
                return 1;
            }
            if (navigator.maxTouchPoints && navigator.maxTouchPoints > 1 && /MacIntel/i.test(navigator.platform || "")) {
                return 1;
            }
        } catch (e) {}
        return 0;
    },

    Lutuoan_PromptMobileText: function (titlePtr, defaultPtr) {
        try {
            if (typeof window === "undefined" || typeof window.prompt !== "function") return 0;
            var title = UTF8ToString(titlePtr);
            var def = UTF8ToString(defaultPtr);
            var result = window.prompt(title, def);
            if (result === null || result === undefined) return 0;
            var bufferSize = lengthBytesUTF8(result) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(result, buffer, bufferSize);
            return buffer;
        } catch (e) {}
        return 0;
    },

    Lutuoan_SyncIdbfs: function () {
        try {
            if (typeof FS !== "undefined" && typeof FS.syncfs === "function") {
                FS.syncfs(false, function (err) {});
            }
        } catch (e) {}
    }
});
