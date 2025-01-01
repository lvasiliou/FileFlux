browser.downloads.onCreated.addListener((downloadItem) => {
    try {
        console.log("Download started:", downloadItem);

        // Construct the custom protocol URL
        const protocolUrl = `customprotocol://${downloadItem.url}`;

        // Cancel the download
        browser.downloads.cancel(downloadItem.id).then(() => {
            console.log(`Download canceled: ${downloadItem.id}`);

            // Open the custom protocol URL in a new tab
            browser.tabs.create({ url: protocolUrl }).then(() => {
                console.log(`Redirected to: ${protocolUrl}`);
            });
        });
    } catch (error) {
        console.error("Error handling download:", error);
    }
});
