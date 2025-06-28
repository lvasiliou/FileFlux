chrome.downloads.onCreated.addListener((downloadItem) => {
  const message = { url: downloadItem.url };
  chrome.runtime.sendNativeMessage("com.example.download_interceptor", message, (response) => {
    if (chrome.runtime.lastError) {
      console.error("Native messaging error:", chrome.runtime.lastError.message);
    } else {
      console.log("Response from native app:", response);
    }
  });
});
