using CommunityToolkit.Mvvm.Input;

using FileFlux.Model;
using FileFlux.Pages;
using FileFlux.Services;

using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FileFlux.ViewModel
{
    public class DownloadsViewModel : BindableObject
    {
        public ObservableCollection<FileDownload> Downloads { get; }

        private readonly DownloadManager _downloadManager;

        public ICommand NewDownload { get; private set; }

        public ICommand ClearDownloads { get; private set; }

        public IAsyncRelayCommand CancelDownload { get; private set; }

        public IAsyncRelayCommand ToggleDownloadStatus { get; private set; }

        public IRelayCommand DeleteCommand { get; private set; }

        public DownloadsViewModel(DownloadManager downloadManager)
        {
            Downloads = downloadManager.Downloads;
            this._downloadManager = downloadManager;
            NewDownload = new Command(NewDownloadAction);
            this.ClearDownloads = new Command(ClearDownloadsAction);
            this.CancelDownload = new AsyncRelayCommand<FileDownload>(CancelDownloadAction);
            this.ToggleDownloadStatus = new AsyncRelayCommand<FileDownload>(ToggleDownloadStatusAction);
            this.DeleteCommand = new RelayCommand<FileDownload>(DeleteAction);
        }

        private void DeleteAction(FileDownload fileDownload)
        {
            if (File.Exists(fileDownload.SavePath))
            {
                File.Delete(fileDownload.SavePath);
            }

            this._downloadManager.RemoveDownload(fileDownload);
        }

        private async Task ToggleDownloadStatusAction(FileDownload fileDownload)
        {
            
        }

        private async Task CancelDownloadAction(FileDownload fileDownload)
        {
            await this._downloadManager.CancelDownload(fileDownload);
            this._downloadManager.RemoveDownload(fileDownload);
        }

        private void NewDownloadAction(Object obj)
        {
            string url = string.Empty;
            var window = App.Current?.Windows[0] as Window;
            var newDownloadm = new NewDownloadViewModel(this._downloadManager);
            if (window != null)
            {
                window.Page.Navigation.PushModalAsync(new NewDownloadPage(newDownloadm)).ContinueWith(x =>
                {
                    FileDownload fileDownload = x.IsCompleted ? newDownloadm.FileDownload : null;

                    if (fileDownload != null)
                    {
                        MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            this._downloadManager.AddDownload(fileDownload);
                            this._downloadManager.StartDownloadAsync(fileDownload);
                        });
                    }

                });
            }
        }

        //window.Page.DisplayPromptAsync("New Download", "URL:").ContinueWith(x =>
        //{
        //    url = x.IsCompleted ? x.Result : string.Empty;
        //    if (!string.IsNullOrEmpty(url) && Uri.IsWellFormedUriString(url, UriKind.Absolute))
        //    {
        //        this._downloadManager.NewDownload(url).ContinueWith(x =>
        //        {
        //            MainThread.InvokeOnMainThreadAsync(() =>
        //            {
        //                this._downloadManager.AddDownload(x.Result);
        //                this._downloadManager.StartDownloadAsync(x.Result);
        //            });
        //        });
        //    }
        //});


        private void ClearDownloadsAction(Object obj)
        {
            this._downloadManager.Downloads.Clear();
        }
    }
}
