using CommunityToolkit.Mvvm.Input;

using FileFlux.Model;
using FileFlux.Services;
using FileFlux.Utilities;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace FileFlux.ViewModel
{
    public class DownloadsViewModel : BindableObject
    {
        public ObservableCollection<Download> Downloads { get; }

        private readonly DownloadManager _downloadManager;

        public ICommand NewDownload { get; private set; }

        public ICommand ClearDownloads { get; private set; }

        public IAsyncRelayCommand CancelDownload { get; private set; }

        public IAsyncRelayCommand ToggleDownloadStatus { get; private set; }

        public IRelayCommand DeleteCommand { get; private set; }

        public IRelayCommand OpenFileCommand { get; private set; }

        public DownloadsViewModel(DownloadManager downloadManager)
        {
            Downloads = downloadManager.Downloads;
            this._downloadManager = downloadManager;
            NewDownload = new Command(NewDownloadAction);
            this.ClearDownloads = new Command(ClearDownloadsAction);
            this.CancelDownload = new AsyncRelayCommand<Download>(CancelDownloadAction);
            this.ToggleDownloadStatus = new AsyncRelayCommand<Download>(ToggleDownloadStatusAction);
            this.DeleteCommand = new RelayCommand<Download>(DeleteAction);
            this.OpenFileCommand = new RelayCommand<Download>(OpenAction);            
        }

        private void DeleteAction(Download? fileDownload)
        {

            if (fileDownload != null)
            {
                this._downloadManager.RemoveDownload(fileDownload);
                try
                {
                    if (File.Exists(fileDownload.SavePath))
                    {
                        File.Delete(fileDownload.SavePath);
                    }
                }
                catch { }
            }
        }

        private void OpenAction(Download fileDownload)
        {
            if (fileDownload != null && fileDownload.Status == FileDownloadStatuses.Completed)
            {
#if WINDOWS
                Process.Start(new ProcessStartInfo(fileDownload.SavePath) { UseShellExecute = true });
#endif
            }
        }

        private async Task ToggleDownloadStatusAction(Download? fileDownload)
        {
            if (fileDownload != null)
            {
                switch (fileDownload.Status)
                {
                    case FileDownloadStatuses.InProgress:
                        await this._downloadManager.PauseDownload(fileDownload);
                        break;
                    case FileDownloadStatuses.Paused:
                        await this._downloadManager.ResumeDownload(fileDownload);
                        break;
                }
            }
        }

        private async Task CancelDownloadAction(Download? fileDownload)
        {
            if (fileDownload != null)
            {
                await this._downloadManager.CancelDownload(fileDownload);
                this._downloadManager.RemoveDownload(fileDownload);
            }
        }

        private void NewDownloadAction(Object obj)
        {
            Utility.OpenNewDownloadWindow(this._downloadManager);
        }

        private void ClearDownloadsAction(Object obj)
        {
            this._downloadManager.Downloads.Clear();
        }
    }
}
