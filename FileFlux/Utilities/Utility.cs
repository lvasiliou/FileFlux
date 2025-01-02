using FileFlux.Model;
using FileFlux.Pages;
using FileFlux.Services;
using FileFlux.ViewModel;

namespace FileFlux.Utilities
{
    internal class Utility
    {
        public static void OpenNewDownloadWindow()
        {
            var downloadManager = Application.Current.Handler.MauiContext.Services.GetService<DownloadManager>();
            string url = string.Empty;
            var window = App.Current?.Windows[0] as Window;
            var newDownloadm = new NewDownloadViewModel(downloadManager);
            if (window != null)
            {
                window.Page.Navigation.PushModalAsync(new NewDownloadPage(newDownloadm)).ContinueWith(x =>
                {
                    FileDownload fileDownload = newDownloadm.FileDownload;

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
    }
}
