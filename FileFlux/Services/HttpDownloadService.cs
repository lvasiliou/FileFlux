using FileFlux.Contracts;
using FileFlux.Localization;
using FileFlux.Model;

namespace FileFlux.Services
{
    public class HttpDownloadService : IDownloadService
    {
        private readonly HttpClient _httpClient;
        public HttpDownloadService(HttpClient httpClient)
        {
            this._httpClient = httpClient;
            this._httpClient.DefaultRequestHeaders.Add("User-Agent", BuildUserAgent());
        }

        public async Task<FileDownload> GetMetadata(Uri uri)
        {
            try
            {
                var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

                response.EnsureSuccessStatusCode();

                var contentDisposition = response.Content.Headers.ContentDisposition;

                string contentType = response.Content.Headers.ContentType?.ToString() ?? string.Empty;
                var filename = contentDisposition?.FileName ?? Path.GetFileName(uri.LocalPath);
                var totalBytes = contentDisposition?.Size ?? response.Content.Headers.ContentLength ?? 0;
                var lastModified = contentDisposition?.ModificationDate?.UtcDateTime ?? response.Content.Headers.LastModified?.UtcDateTime ?? DateTime.UtcNow;
                var created = contentDisposition?.CreationDate?.UtcDateTime ?? DateTime.UtcNow;
                var fileDownload = new FileDownload { Id = Guid.CreateVersion7(), ContentType = contentType, Url = uri, FileName = filename, TotalSize = totalBytes, Status = FileDownloadStatuses.New, LastModified = lastModified, Created = created };
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
                fileDownload.Status = FileDownloadStatuses.InProgress;
                var response = await _httpClient.GetAsync(fileDownload.Url, HttpCompletionOption.ResponseHeadersRead, fileDownload.CancellationTokenSource.Token);

                response.EnsureSuccessStatusCode();


                using var contentStream = await response.Content.ReadAsStreamAsync();
                var fileMode = File.Exists(fileDownload.SavePath) ? FileMode.Truncate : FileMode.Create;
                using var fileStream = new FileStream(fileDownload.SavePath, fileMode, FileAccess.ReadWrite, FileShare.None);


                var buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalRead += bytesRead;

                    fileDownload.TotalDownloaded = totalRead;
                    fileDownload.PercentCompleted = (double)totalRead / fileDownload.TotalSize * 100;
                    if (fileDownload.CancellationTokenSource.IsCancellationRequested)
                    {
                        fileStream.Close();
                        File.Delete(fileDownload.SavePath);
                        throw new TaskCanceledException(App_Resources.CancellationExceptionMessage);
                    }
                }

                fileDownload.Status = FileDownloadStatuses.Completed;

                File.SetCreationTime(fileDownload.SavePath, fileDownload.Created);
                File.SetLastWriteTime(fileDownload.SavePath, fileDownload.LastModified);
                File.SetLastAccessTime(fileDownload.SavePath, DateTime.Now);
            }
            catch (Exception ex)
            {
                fileDownload.Status = FileDownloadStatuses.Failed;
                fileDownload.ErrorMessage = ex.Message;
            }
        }

        public void PauseDownload(FileDownload fileDownload)
        {
            fileDownload.CancellationTokenSource.Cancel();
            fileDownload.Status = FileDownloadStatuses.Paused;
        }

        private static string BuildUserAgent()
        {
            string userAgent = $"FileFlux/{AppInfo.Version} ({DeviceInfo.Manufacturer}; {DeviceInfo.Model}; {DeviceInfo.Platform} {DeviceInfo.VersionString})";
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                userAgent += " Mobile";
            }
            else if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                userAgent += " iPhone";
            }
            else if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                userAgent += " Windows";
            }
            else if (DeviceInfo.Platform == DevicePlatform.MacCatalyst)
            {
                userAgent += " Mac";
            }
            else if (DeviceInfo.Platform == DevicePlatform.Tizen)
            {
                userAgent += " Tizen";
            }
            return userAgent;
        }
    }
}
