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
        private FileDownload _selectedItem;

        public ICommand NewDownload { get; private set; }

        public ICommand ClearDownloads { get; private set; }

        public ICommand CancelDownload { get; private set; }

        public ICommand ToggleDownloadStatus { get; private set; }

        public FileDownload SelectedItem
        {
            get => this._selectedItem;
            set
            {
                this._selectedItem = value;
            }
        }

        public DownloadsViewModel(DownloadManager downloadManager)
        {
            Downloads = downloadManager.Downloads;
            this._downloadManager = downloadManager;
            NewDownload = new Command(NewDownloadAction);
            this.ClearDownloads = new Command(ClearDownloadsAction);
            this.CancelDownload = new Command(CancelDownloadAction);
            this.ToggleDownloadStatus = new Command(ToggleDownloadStatusAction);
        }

        private void ToggleDownloadStatusAction(object obj)
        {
            if (this.SelectedItem != null)
            {
                if (this.SelectedItem.Status == FileDownloadStatuses.Paused)
                {
                    this._downloadManager.StartDownloadAsync(this.SelectedItem).GetAwaiter().GetResult();
                }
                else
                {
                    this._downloadManager.PauseDownload(this.SelectedItem).GetAwaiter().GetResult();
                }
            }
        }

        private void CancelDownloadAction(object obj)
        {
            if (this.SelectedItem != null)
            {
                this._downloadManager.CancelDownload(this.SelectedItem).GetAwaiter().GetResult();
            }
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
