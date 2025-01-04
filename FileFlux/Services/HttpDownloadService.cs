using FileFlux.Contracts;
using FileFlux.Localization;
using FileFlux.Model;
using FileFlux.Utilities;

using System.Net.Http.Headers;

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
                var request = new HttpRequestMessage(HttpMethod.Head, uri);
                var response = await _httpClient.SendAsync(request);

                response.EnsureSuccessStatusCode();

                var contentDisposition = response.Content.Headers.ContentDisposition;


                bool supportsResume = response.Headers.AcceptRanges != null && response.Headers.AcceptRanges.Contains(Constants.BytesRangeHeader);
                string etag = response.Headers.ETag?.Tag ?? string.Empty;
                string contentType = response.Content.Headers.ContentType?.ToString() ?? string.Empty;
                var filename = contentDisposition?.FileName ?? Path.GetFileName(uri.LocalPath);
                var totalBytes = contentDisposition?.Size ?? response.Content.Headers.ContentLength ?? 0;
                var lastModified = contentDisposition?.ModificationDate?.UtcDateTime ?? response.Content.Headers.LastModified?.UtcDateTime ?? DateTime.UtcNow;
                var created = contentDisposition?.CreationDate?.UtcDateTime ?? DateTime.UtcNow;
                var fileDownload = new FileDownload { Id = Guid.CreateVersion7(), ContentType = contentType, Url = uri, FileName = filename, TotalSize = totalBytes, Status = FileDownloadStatuses.New, LastModified = lastModified, Created = created, SupportsResume = supportsResume, ETag = etag };
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
                var request = new HttpRequestMessage(HttpMethod.Get, fileDownload.Url);
                FileMode fileMode = FileMode.CreateNew;
                long totalRead = 0;

                switch (fileDownload.Status)
                {
                    case FileDownloadStatuses.New:
                        fileMode = FileMode.CreateNew;
                        break;
                    case FileDownloadStatuses.Paused:
                        if (fileDownload.SupportsResume)
                        {
                            fileMode = FileMode.Append;
                            request.Headers.Range = new RangeHeaderValue(fileDownload.TotalDownloaded, null);
                            totalRead = fileDownload.TotalDownloaded;
                        }
                        else
                        {
                            fileMode = FileMode.Truncate;
                        }

                        break;
                }

                fileDownload.Status = FileDownloadStatuses.InProgress;


                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, fileDownload.CancellationTokenSource.Token);

                response.EnsureSuccessStatusCode();

                if (response.Headers.ETag != null && response.Headers.ETag.Tag != fileDownload.ETag)
                {
                    fileDownload.Status = FileDownloadStatuses.Failed;
                    fileDownload.ErrorMessage = App_Resources.ETagMismatchMessage;
                    return;
                }

                using var contentStream = await response.Content.ReadAsStreamAsync();
                if (fileDownload != null && !string.IsNullOrWhiteSpace(fileDownload.SavePath))
                {
                    using (var fileStream = new FileStream(fileDownload.SavePath, fileMode, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];

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
                                switch (fileDownload.Status)
                                {
                                    case FileDownloadStatuses.Paused:
                                        return;
                                    case FileDownloadStatuses.Cancelled:
                                        File.Delete(fileDownload.SavePath);
                                        break;
                                }

                            }
                        }

                    }

                    fileDownload.Status = FileDownloadStatuses.Completed;

                    File.SetCreationTime(fileDownload.SavePath, fileDownload.Created);
                    File.SetLastWriteTime(fileDownload.SavePath, fileDownload.LastModified);
                    File.SetLastAccessTime(fileDownload.SavePath, DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                fileDownload.Status = FileDownloadStatuses.Failed;
                fileDownload.ErrorMessage = ex.Message;
            }
        }

        public async Task PauseDownload(FileDownload fileDownload)
        {
            fileDownload.Status = FileDownloadStatuses.Paused;
            await fileDownload.CancellationTokenSource.CancelAsync();
        }

        public async Task CancelDownload(FileDownload fileDownload)
        {
            await fileDownload.CancellationTokenSource.CancelAsync();
            fileDownload.Status = FileDownloadStatuses.Cancelled;
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
