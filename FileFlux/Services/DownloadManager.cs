using System.Collections.ObjectModel;
using System.Text.Json;

using FileFlux.Utilities;

using FileFlux.Model;
using System.Security.Cryptography;


namespace FileFlux.Services;

public partial class DownloadManager : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly DownloadServiceFactory _downloadServiceFactory;

    public ObservableCollection<Download> Downloads = new();

    public DownloadManager(DownloadServiceFactory downloadServiceFactory, SettingsService settingsService)
    {
        _settingsService = settingsService;
        this._downloadServiceFactory = downloadServiceFactory;
        this.LoadFromDisk();
    }

    public async Task<Download> NewDownload(string url)
    {
        try
        {
            var uri = new Uri(url);
            var _downloadService = this._downloadServiceFactory.GetService(uri);
            Download fileDownload = await _downloadService.GetMetadata(uri);

            var filename = fileDownload.FileName;
            if (!string.IsNullOrWhiteSpace(filename))
            {
                var savePathHint = Path.Combine(_settingsService.GetSaveLocation(), filename);
                if (this._settingsService.GetOverwriteBehaviour() == false)
                {
                    savePathHint = EnsureUniqueFileName(savePathHint);
                }

                fileDownload.FileName = Path.GetFileName(savePathHint);

                fileDownload.SavePath = savePathHint;
            }
            return fileDownload;
        }
        catch (Exception ex)
        {
            return await Task.FromException<Download>(ex);
        }
    }

    public async Task StartDownloadAsync(Download? fileDownload)
    {
        try
        {
            if (fileDownload != null && fileDownload.Url != null)
            {
                var _downloadService = this._downloadServiceFactory.GetService(fileDownload.Url);
                await _downloadService.StartDownloadAsync(fileDownload);
            }

        }
        catch (Exception ex)
        {
            if (fileDownload != null)
            {
                fileDownload.Status = FileDownloadStatuses.Failed;
                fileDownload.ErrorMessage = ex.Message;
            }
        }
    }

    public async Task PauseDownload(Download? fileDownload)
    {
        if (fileDownload != null && fileDownload.Url != null)
        {
            var _downloadService = this._downloadServiceFactory.GetService(fileDownload.Url);
            await _downloadService.PauseDownload(fileDownload);
        }
    }

    public async Task CancelDownload(Download fileDownload)
    {
        if (fileDownload != null && fileDownload.Url != null)
        {
            var _downloadService = this._downloadServiceFactory.GetService(fileDownload.Url);
            await _downloadService.CancelDownload(fileDownload);
        }
    }

    public async Task ResumeDownload(Download fileDownload)
    {
        if (fileDownload != null && fileDownload.Url != null)
        {
            var _downloadService = this._downloadServiceFactory.GetService(fileDownload.Url);
            fileDownload.CancellationTokenSource = new();
            await _downloadService.StartDownloadAsync(fileDownload);
        }
    }
    public void AddDownload(Download download)
    {
        Downloads.Add(download);
    }

    public async void RemoveDownload(Download download)
    {
        if (download.Status == FileDownloadStatuses.InProgress)
        {
            await this.CancelDownload(download);
        }

        Downloads.Remove(download);

    }

    public async Task<bool> VerifyDownload(Download fileDownload, string hash)
    {
        bool verified = false;

        if (fileDownload != null && !string.IsNullOrWhiteSpace(fileDownload.ETag) && !string.IsNullOrWhiteSpace(fileDownload.SavePath))
        {
            fileDownload.Status = FileDownloadStatuses.Verifying;
            using (var algo = MD5.Create())
            {
                using (var fs = new FileStream(fileDownload.SavePath, FileMode.Open))
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

    public void ClearDownloads()
    {
        var itemsToRemove = Downloads.Where(item => item.Status != FileDownloadStatuses.InProgress).ToList();

        foreach (Download fileDownload in itemsToRemove)
        {
            Downloads.Remove(fileDownload);
        }
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
                    bool fileExists = File.Exists(download.SavePath);
                    if (fileExists)
                    {
                        var fileinfo = new FileInfo(download?.SavePath);
                        if (fileinfo.Length == download.TotalDownloaded)
                        {
                            Downloads.Add(download);
                            if (download.Status == FileDownloadStatuses.Paused)
                            {
                                _ = this.StartDownloadAsync(download);
                            }
                        }
                    }
                }
            }
        }
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

    public static string GetLocalAppDataPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Constants.AppLocalDataFolder);
    }

    public void Dispose()
    {
        var downloadsToPause = new Collection<Download>();

        foreach (Download download in Downloads)
        {

            if (download.Status == FileDownloadStatuses.InProgress)
            {
                downloadsToPause.Add(download);
            }
        }

        foreach (Download downloadToPause in downloadsToPause)
        {
            this.PauseDownload(downloadToPause).ContinueWith((task) =>
            {
                task.Wait();
            });
        }

        this.SaveToDisk();

    }
}