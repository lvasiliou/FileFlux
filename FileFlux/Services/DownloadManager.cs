using FileFlux.Localization;
using System.Collections.ObjectModel;
using System.Text.Json;
using FileFlux.Model;
using FileFlux.Contracts;
using System;

namespace FileFlux.Services;
public class DownloadManager
{
    private readonly SettingsService _settingsService;
    private readonly DownloadServiceFactory _downloadServiceFactory;

    public ObservableCollection<FileDownload> Downloads { get; } = new();

    public DownloadManager(DownloadServiceFactory downloadServiceFactory, SettingsService settingsService)
    {
        _settingsService = settingsService;
        this._downloadServiceFactory = downloadServiceFactory;
    }

    public async Task<FileDownload> NewDownload(string url)
    {
        try
        {
            var uri = new Uri(url);
            IDownloadService service = this._downloadServiceFactory.GetService(uri);
            FileDownload fileDownload = await service.GetMetadata(uri);

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
            IDownloadService service = this._downloadServiceFactory.GetService(fileDownload.Url);

            await service.StartDownloadAsync(fileDownload);
        }
        catch (Exception ex)
        {
            fileDownload.Status = FileDownloadStatuses.Failed;
            fileDownload.ErrorMessage = ex.Message;
        }
    }

    public async Task PauseDownload(FileDownload fileDownload)
    {
        await fileDownload.CancellationTokenSource.CancelAsync();
        fileDownload.Status = FileDownloadStatuses.Paused;
    }

    public async Task CancelDownload(FileDownload fileDownload)
    {
        await fileDownload.CancellationTokenSource.CancelAsync();
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

        if (File.Exists(download.SavePath))
        {
            File.Delete(download.SavePath);
        }
    }

    public void ClearDownloads()
    {
        foreach (var item in Downloads)
        {
            if (item.Status != FileDownloadStatuses.InProgress)
            {
                Downloads.Remove(item);
            }
        }
    }

    public void SaveToDisk(string filePath)
    {
        var json = JsonSerializer.Serialize(Downloads);
        File.WriteAllText(filePath, json);
    }

    public void LoadFromDisk(string filePath)
    {
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            var downloads = JsonSerializer.Deserialize<ObservableCollection<FileDownload>>(json);

            if (downloads != null)
            {
                foreach (var download in downloads)
                {
                    Downloads.Add(download);
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
}