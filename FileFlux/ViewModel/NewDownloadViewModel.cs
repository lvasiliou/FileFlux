using CommunityToolkit.Mvvm.Input;

using FileFlux.Model;
using FileFlux.Services;

using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace FileFlux.ViewModel
{
    public class NewDownloadViewModel : INotifyPropertyChanged
    {
        private readonly DownloadManager _downloadManager;

        private FileDownload _fileDownload;
        private string _url;

        public ICommand CancelCommand { get; private set; }

        public ICommand SaveCommand { get; private set; }

        public IAsyncRelayCommand GetFileCommand { get; private set; }

        private string Url
        {
            get => this._url;
            set
            {
                this._url = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Url)));
            }
        }

        public FileDownload FileDownload
        {
            get => this._fileDownload;
            set
            {
                this._fileDownload = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileDownload)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public NewDownloadViewModel(DownloadManager downloadManager)
        {
            this._downloadManager = downloadManager;
            this.CancelCommand = new Command(CancelAction);
            this.GetFileCommand = new AsyncRelayCommand(GetFileActionAsync);
        }

        private async Task GetFileActionAsync()
        {
            await _downloadManager.NewDownload(this._url).ContinueWith((task) =>
            {
                if (task.IsFaulted)
                {
                    Debug.WriteLine(task.Exception?.Message);
                    return;
                }

                this.FileDownload = task.Result;
            });
        }        

        private void CancelAction(object obj)
        {
            var window = App.Current?.Windows[0] as Window;
            window.Page.Navigation.PopModalAsync();
        }
    }
}
