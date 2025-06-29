﻿using CommunityToolkit.Mvvm.Input;

using FileFlux.Model;
using FileFlux.Services;

using System.ComponentModel;

namespace FileFlux.ViewModel
{
    public partial class NewDownloadViewModel : INotifyPropertyChanged
    {
        private readonly DownloadManager _downloadManager;

        private Download? _fileDownload = null;
        private string _url = string.Empty;
        private string _errorMesssage = string.Empty;

        public IRelayCommand CancelCommand { get; private set; }

        public IRelayCommand StartCommand { get; private set; }

        public IAsyncRelayCommand GetFileCommand { get; private set; }

        public string Url
        {
            get => this._url;
            set
            {
                this._url = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Url)));
            }
        }

        public Download? FileDownload
        {
            get => this._fileDownload;
            set
            {
                this._fileDownload = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileDownload)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DownloadValid)));
            }
        }

        public bool DownloadValid
        {
            get
            {
                return this.FileDownload != null && this.FileDownload.Status != FileDownloadStatuses.Failed;
            }
        }

        public string ErrorMessage
        {
            get => this._errorMesssage;
            set
            {
                this._errorMesssage = value;

                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorMessage)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public NewDownloadViewModel(DownloadManager downloadManager)
        {
            this._downloadManager = downloadManager;
            this.CancelCommand = new RelayCommand(CancelAction);
            this.GetFileCommand = new AsyncRelayCommand(GetFileActionAsync);
            this.StartCommand = new RelayCommand(StartDownload);

        }

        private void CancelAction()
        {
            this.PopModal();
        }

        private void StartDownload()
        {
            if (this._fileDownload != null)
            {
                _ = this._downloadManager.NewDownloadAsync(this._fileDownload);                
                this.PopModal();
            }
        }

        private async Task GetFileActionAsync()
        {
            var uri = new Uri(this._url);
            this.FileDownload = await _downloadManager.GetMetadataAsync(uri);           
        }

        private void PopModal()
        {
            var window = App.Current?.Windows[0] as Window;
            window?.Page?.Navigation.PopModalAsync();
        }
    }
}
