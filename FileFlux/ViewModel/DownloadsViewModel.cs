using CommunityToolkit.Mvvm.Input;

using FileFlux.Model;
using FileFlux.Services;
using FileFlux.Utilities;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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

        private IAsyncRelayCommand? _cancelDownload;
        public IAsyncRelayCommand CancelDownload => _cancelDownload ??= new AsyncRelayCommand<Download>(CancelDownloadAction);

        private IAsyncRelayCommand? _toggleDownloadStatus;
        public IAsyncRelayCommand ToggleDownloadStatus => _toggleDownloadStatus ??= new AsyncRelayCommand<Download>(ToggleDownloadStatusAction);

        private IAsyncRelayCommand? _deleteCommand;
        public IAsyncRelayCommand DeleteCommand => _deleteCommand ??= new AsyncRelayCommand<Download>(DeleteAction);

        private IRelayCommand? _openFileCommand;
        public IRelayCommand OpenFileCommand => _openFileCommand ??= new RelayCommand<Download>(OpenAction);

        private IRelayCommand? _showInFolderCommand;
        public IRelayCommand ShowInFolderCommand => _showInFolderCommand ??= new RelayCommand<Download>(ShowInFolderAction);

        public DownloadsViewModel(DownloadManager downloadManager)
        {
            Downloads = downloadManager.Downloads;
            this._downloadManager = downloadManager;
            NewDownload = new Command(NewDownloadAction);
            this.ClearDownloads = new Command(ClearDownloadsAction);
            //this.Downloads.CollectionChanged += (object? sender, NotifyCollectionChangedEventArgs args) =>
            //{
            //    if (args.NewItems is not null)
            //    {
            //        foreach (Download item in args.NewItems)
            //        {
            //            item.PropertyChanged += (s, e) =>
            //            {
            //                item.PropertyChanged += DownloadChanged;
            //            };
            //        }
            //    }

            //    if (args.OldItems is not null)
            //    {
            //        foreach (Download item in args.OldItems)
            //        {
            //            item.PropertyChanged -= DownloadChanged;
            //        }
            //    }
            //};
        }

        private void DownloadChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Download download && e.PropertyName == nameof(Download.Status))
            {
                this.ToggleDownloadStatus.NotifyCanExecuteChanged();
            }
        }

        private async Task DeleteAction(Download? fileDownload)
        {

            if (fileDownload != null)
            {
                await this._downloadManager.CancelDownloadAsync(fileDownload);

            }
        }

        private void OpenAction(Download? fileDownload)
        {
            if (fileDownload != null && fileDownload.Status == FileDownloadStatuses.Completed)
            {
#if WINDOWS
                Process.Start(new ProcessStartInfo(fileDownload.FilePath) { UseShellExecute = true });
#endif
            }
        }

        private void ShowInFolderAction(Download? download)
        {
            if (download != null && download.Status == FileDownloadStatuses.Completed)
            {
#if WINDOWS
                if (File.Exists(download.FilePath))
                {
                    var argument = $"/select,\"{download.FilePath}\"";
                    Process.Start("explorer.exe", argument);
                }
                #endif
            }
        }

        private bool _isToggling;

        private async Task ToggleDownloadStatusAction(Download? fileDownload)
        {
            if (_isToggling || fileDownload == null)
                return;

            _isToggling = true;

            try
            {
                switch (fileDownload.Status)
                {
                    case FileDownloadStatuses.Downloading:
                        await _downloadManager.PauseDownloadAsync(fileDownload);
                        break;
                    case FileDownloadStatuses.Paused:
                        await _downloadManager.ResumeDownloadAsync(fileDownload);
                        break;
                }

                _isToggling = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling download status: {ex.Message}");
            }
            finally
            {
                _isToggling = false;
            }
        }

        private async Task CancelDownloadAction(Download? fileDownload)
        {
            if (fileDownload != null)
            {
                await this._downloadManager.CancelDownloadAsync(fileDownload);
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
