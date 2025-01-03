using System.Collections.ObjectModel;
using System.Text.Json;

using FileFlux.Model;


namespace FileFlux.Services;

public class DownloadManager : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly DownloadServiceFactory _downloadServiceFactory;

    public ObservableCollection<FileDownload> Downloads = new();

    public DownloadManager(DownloadServiceFactory downloadServiceFactory, SettingsService settingsService)
    {
        _settingsService = settingsService;
        this._downloadServiceFactory = downloadServiceFactory;
        this.LoadFromDisk();
    }

    public async Task<FileDownload> NewDownload(string url)
    {
        try
        {
            var uri = new Uri(url);
            var _downloadService = this._downloadServiceFactory.GetService(uri);
            FileDownload fileDownload = await _downloadService.GetMetadata(uri);

            var filename = fileDownload.FileName;
            var savePathHint = Path.Combine(_settingsService.GetSaveLocation(), filename);
            if (this._settingsService.GetOverwriteBehaviour() == false)
            {
                savePathHint = EnsureUniqueFileName(savePathHint);
            }

            fileDownload.FileName = Path.GetFileName(savePathHint);

            fileDownload.SavePath = savePathHint;
            return fileDownload;
        }
        catch (Exception ex)
        {
            return await Task.FromException<FileDownload>(ex);
        }
    }

    public async Task StartDownloadAsync(FileDownload fileDownload)
    {
        try
        {
            var _downloadService = this._downloadServiceFactory.GetService(fileDownload.Url);
            await _downloadService.StartDownloadAsync(fileDownload);
        }
        catch (Exception ex)
        {
            fileDownload.Status = FileDownloadStatuses.Failed;
            fileDownload.ErrorMessage = ex.Message;
        }
    }

    public async Task PauseDownload(FileDownload fileDownload)
    {
        var _downloadService = this._downloadServiceFactory.GetService(fileDownload.Url);
        await _downloadService.PauseDownload(fileDownload);
    }

    public async Task CancelDownload(FileDownload fileDownload)
    {
        var _downloadService = this._downloadServiceFactory.GetService(fileDownload.Url);
        await _downloadService.CancelDownload(fileDownload);
    }

    public async Task ResumeDownload(FileDownload fileDownload)
    {
        var _downloadService = this._downloadServiceFactory.GetService(fileDownload.Url);
        fileDownload.CancellationTokenSource = new();
        await _downloadService.StartDownloadAsync(fileDownload);
    }
    public void AddDownload(FileDownload download)
    {
        Downloads.Add(download);
    }

    public async void RemoveDownload(FileDownload download)
    {
        if (download.Status == FileDownloadStatuses.InProgress)
        {
            await this.CancelDownload(download);
        }

        Downloads.Remove(download);

    }

    public void ClearDownloads()
    {
        var itemsToRemove = Downloads.Where(item => item.Status != FileDownloadStatuses.InProgress).ToList();

        foreach (FileDownload fileDownload in itemsToRemove)
        {
            Downloads.Remove(fileDownload);
        }
    }

    public void SaveToDisk()
    {
        var json = JsonSerializer.Serialize(Downloads);
        var localAppDataPath = GetLocalAppDataPath();
        var filePath = Path.Combine(localAppDataPath, "downloads.json");
        var directoryName = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }

        File.WriteAllText(filePath, json);
    }

    public void LoadFromDisk()
    {
        var localAppDataPath = GetLocalAppDataPath();
        var filePath = Path.Combine(localAppDataPath, "downloads.json");
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            var downloads = JsonSerializer.Deserialize<ObservableCollection<FileDownload>>(json);

            if (downloads != null)
            {
                foreach (var download in downloads)
                {
                    Downloads.Add(download);

                    if (download.Status == FileDownloadStatuses.InProgress)
                    {
                        //restart download
                    }
                }
            }
        }
    }

    public static string EnsureUniqueFileName(string fullPath)
    {
        string directory = Path.GetDirectoryName(fullPath);
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
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FileFlux");
    }

    public void Dispose()
    {
        var downloadsToPause = new Collection<FileDownload>();

        foreach (FileDownload download in Downloads)
        {

            if (download.Status == FileDownloadStatuses.InProgress)
            {
                downloadsToPause.Add(download);
            }
        }

        foreach (FileDownload downloadToPause in downloadsToPause)
        {
            this.PauseDownload(downloadToPause).ContinueWith((task) =>
            {
                task.Wait();
            });
        }

        this.SaveToDisk();

    }
}