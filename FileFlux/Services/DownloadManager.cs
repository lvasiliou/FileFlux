using FileFlux.Model;
using FileFlux.Utilities;

using System;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;

namespace FileFlux.Services;

public partial class DownloadManager : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly DownloadServiceFactory _downloadServiceFactory;

    public ObservableCollection<Download> Downloads = new();

    public DownloadManager(DownloadServiceFactory downloadServiceFactory, OriginGuardFactory originGuardFactory, SettingsService settingsService)
    {
        _settingsService = settingsService;
        this._downloadServiceFactory = downloadServiceFactory;
        this.LoadFromDisk();
    }


    public async Task NewDownloadAsync(Uri uri)
    {
        var _downloadService = this._downloadServiceFactory.GetService(uri);
        var metadata = await _downloadService.GetMetadata(uri);
        metadata.Status = FileDownloadStatuses.Pending;

        await this.NewDownloadAsync(metadata);
    }

    public async Task NewDownloadAsync(Download download)
    {
        if (download == null)
        {
            throw new ArgumentNullException(nameof(download));
        }


        if (!this.Downloads.Contains(download, new DownloadEqualityComparer()))
        {
            this.Downloads.Add(download);
        }

        var _downloadService = this._downloadServiceFactory.GetService(download.Uri);

        download.CancellationTokenSource = new CancellationTokenSource();
        
        var filename = download.FileName;
        if (!string.IsNullOrWhiteSpace(filename))
        {
            var savePathHint = Path.Combine(_settingsService.GetSaveLocation(), filename);
            if (this._settingsService.GetOverwriteBehaviour() == false)
            {
                savePathHint = EnsureUniqueFileName(savePathHint);
            }

            download.FileName = Path.GetFileName(savePathHint);

            download.FilePath = savePathHint;
        }

        await _downloadService.StartDownloadAsync(download);
    }

    public async Task<Download> GetMetadataAsync(Uri uri)
    {
        var _downloadService = this._downloadServiceFactory.GetService(uri);
        var download = await _downloadService.GetMetadata(uri);

        _ = Task.Run(async () =>
        {
            download.Status = FileDownloadStatuses.Measuring;
            (download.OptimalBufferSize, download.OptimalChunks) = await _downloadService.BenchmarkDownloadStrategyAsync(uri.ToString(), download.TotalBytes);
            download.Status = FileDownloadStatuses.Pending;
        });

        return download;
    }

    public async Task ResumeDownloadAsync(Download download)
    {
        if (download.Status == FileDownloadStatuses.Paused || download.Status == FileDownloadStatuses.Failed)
        {
            var _downloadService = this._downloadServiceFactory.GetService(download.Uri);
            download.CancellationTokenSource = new CancellationTokenSource();
            await _downloadService.StartDownloadAsync(download);
        }
    }

    public async Task PauseDownloadAsync(Download download)
    {
        var _downloadService = this._downloadServiceFactory.GetService(download.Uri);
        await _downloadService.PauseDownloadAsync(download);
    }

    public async Task CancelDownloadAsync(Download download)
    {
        var _downloadService = this._downloadServiceFactory.GetService(download.Uri);
        await _downloadService.CancelDownloadAsync(download);
        this.Downloads.Remove(download);
    }

    public void Dispose()
    {
        var downloadsToPause = new Collection<Download>();

        foreach (Download download in Downloads)
        {

            if (download.Status == FileDownloadStatuses.Downloading)
            {
                downloadsToPause.Add(download);
            }
        }

        foreach (Download downloadToPause in downloadsToPause)
        {
            _ = this.PauseDownloadAsync(downloadToPause);
        }

        SaveToDisk();
    }

    public async Task<bool> VerifyDownload(Download fileDownload, string hash)
    {
        bool verified = false;

        if (fileDownload != null && !string.IsNullOrWhiteSpace(fileDownload.FilePath))
        {
            fileDownload.Status = FileDownloadStatuses.Verifying;
            using (var algo = MD5.Create())
            {
                using (var fs = new FileStream(fileDownload.FilePath, FileMode.Open))
                {
                    fs.Position = 0;
                    byte[] bytes = await algo.ComputeHashAsync(fs);
                    string computeHash = BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
                    string normalisedHash = hash.Replace("0x", string.Empty).ToLowerInvariant();
                    bool comparison = string.Compare(computeHash, normalisedHash, StringComparison.OrdinalIgnoreCase) == 0;
                    verified = comparison;
                }
            }
        }

        return verified;
    }

    public void SaveToDisk()
    {
        var json = JsonSerializer.Serialize(this.Downloads);
        var localAppDataPath = GetLocalAppDataPath();
        var filePath = Path.Combine(localAppDataPath, Constants.DownloadsPersistenceFileName);
        var directoryName = Path.GetDirectoryName(filePath) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }

        File.WriteAllText(filePath, json);
    }

    public void LoadFromDisk()
    {
        var localAppDataPath = GetLocalAppDataPath();
        var filePath = Path.Combine(localAppDataPath, Constants.DownloadsPersistenceFileName);
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            var downloads = JsonSerializer.Deserialize<ObservableCollection<Download>>(json);

            if (downloads != null)
            {
                foreach (var download in downloads)
                {
                    var fileinfo = new FileInfo(download?.FilePath);

                    download.CancellationTokenSource = new CancellationTokenSource();
                    this.Downloads.Add(download);
                    if (download.Status == FileDownloadStatuses.Paused)
                    {
                        _ = this.ResumeDownloadAsync(download);
                    }
                }
            }
        }
    }

    public static string GetLocalAppDataPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Constants.AppLocalDataFolder);
    }



    public static string EnsureUniqueFileName(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            throw new ArgumentException(Constants.SavePathEmptyException, nameof(fullPath));
        }

        string directory = Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException(Constants.DirectoryNotDeterminedException);
        string filename = Path.GetFileNameWithoutExtension(fullPath);
        string extension = Path.GetExtension(fullPath);
        string newFullPath = fullPath;
        int counter = 1;


        while (File.Exists(newFullPath))
        {
            newFullPath = Path.Combine(directory, $"{filename} ({counter}){extension}");
            counter++;
        }

        return newFullPath;
    }

    public void ClearDownloads()
    {
        var itemsToRemove = this.Downloads.Where(item => item.Status != FileDownloadStatuses.Downloading).ToList();

        foreach (Download fileDownload in itemsToRemove)
        {
            this.Downloads.Remove(fileDownload);
        }
    }
}