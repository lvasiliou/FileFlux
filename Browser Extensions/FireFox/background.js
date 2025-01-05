const isChrome = typeof chrome !== 'undefined' && typeof chrome.downloads !== 'undefined';
const isFirefox = typeof browser !== 'undefined' && typeof browser.downloads !== 'undefined';

const downloadsAPI = isChrome ? chrome.downloads : browser.downloads;

// Function to modify the URL and initiate the download with the fileflux:// protocol
function modifyDownloadUrl(downloadItem) {
    const originalUrl = downloadItem.finalUrl || downloadItem.url;

    const modifiedUrl = `fileflux://${originalUrl}`;

    if (isChrome) {
        chrome.tabs.create({ url: modifiedUrl }, (tab) => {
            console.log("Opened fileflux:// protocol in a new tab, tab ID:", tab.id);
        });
    } else if (isFirefox) {
        browser.tabs.create({ url: modifiedUrl });
    }
}

// Add a listener for download requests before the filename is determined
const downloadDeterminingFilenameListener = (downloadItem, suggest) => {
    if (downloadItem.url.startsWith('fileflux://')) {
        return;
    }

    const originalUrl = downloadItem.finalUrl || downloadItem.url;


    const modifiedUrl = `fileflux://${originalUrl}`;

    suggest({ filename: modifiedUrl });
};

if (isChrome) {
    chrome.downloads.onDeterminingFilename.addListener(downloadDeterminingFilenameListener);
} else if (isFirefox) {
    browser.downloads.onCreated.addListener(modifyDownloadUrl);
}
