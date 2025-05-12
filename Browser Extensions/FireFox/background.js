browser.webRequest.onHeadersReceived.addListener(
    function (details) {
        const ranges = details.responseHeaders.find(x => x.name === "Accept-Ranges");
        const disposition = details.responseHeaders.find(x => x.name === "Content-Disposition");
        if (ranges !== undefined | disposition != undefined) {
            const originalUrl = details.url;
            const encodedUrl = encodeURIComponent(originalUrl);
            const redirectUrl = `fileflux:${encodedUrl}`;

            debugger;
            return { redirectUrl: redirectUrl };
        }
    },
    {
        urls: ["<all_urls>"],
        types: ["main_frame", "sub_frame", "xmlhttprequest", "image", "stylesheet", "script", "object", "other", "download"]        
    },
    ["blocking", "responseHeaders"] // Blocking option to cancel the request
);
