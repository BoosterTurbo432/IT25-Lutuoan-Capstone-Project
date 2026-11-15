mergeInto(LibraryManager.library, {
    GetBrowserIsMobile: function () {
        var ua = navigator.userAgent || "";
        var mobileUa = /Mobi|Android|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(ua);
        var coarse = window.matchMedia && window.matchMedia("(pointer: coarse)").matches;
        return (mobileUa || (coarse && navigator.maxTouchPoints > 0)) ? 1 : 0;
    },
    GetBrowserInnerWidth: function () {
        return Math.max(1, Math.round(window.innerWidth || 0));
    },
    GetBrowserInnerHeight: function () {
        return Math.max(1, Math.round(window.innerHeight || 0));
    }
});
