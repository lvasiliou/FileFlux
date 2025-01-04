using FileFlux.Model;
using FileFlux.Pages;
using FileFlux.Services;
using FileFlux.ViewModel;

namespace FileFlux.Utilities
{
    internal class Utility
    {
        public static void OpenNewDownloadWindow(DownloadManager downloadManager)
        {
            string url = string.Empty;
            var window = App.Current?.Windows[0] as Window;
            var newDownloadm = new NewDownloadViewModel(downloadManager);
            if (window != null)
            {
                window.Page?.Navigation.PushModalAsync(new NewDownloadPage(newDownloadm)).ContinueWith(x =>
                {
                    FileDownload? fileDownload = newDownloadm.FileDownload;

                    if (fileDownload != null)
                    {
                        MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            downloadManager.AddDownload(fileDownload);
                            _ = downloadManager.StartDownloadAsync(fileDownload);
                        });
                    }

                });
            }
        }

        public static void OpenSettingsWindow(SettingsViewModel settingsViewModel)
        {
            var window = App.Current?.Windows[0] as Window;
            if (window != null)
            {
                window.Page?.Navigation.PushModalAsync(new SettingsPage(settingsViewModel));
            }
        }
    }
}
