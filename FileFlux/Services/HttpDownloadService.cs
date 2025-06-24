using FileFlux.Contracts;
using FileFlux.Model;
using FileFlux.Utilities;

using System.Net.Http.Headers;

namespace FileFlux.Services
{
    public class HttpDownloadService : IDownloadService
    {
        private readonly HttpClient _httpClient;
        private readonly SettingsService _settingsService;

        public HttpDownloadService(HttpClient httpClient, SettingsService settingsService)
        {
            this._httpClient = httpClient;
            this._settingsService = settingsService;
            this._httpClient.DefaultRequestHeaders.Add("User-Agent", BuildUserAgent());
        }

        public async Task<Download> GetMetadata(Uri uri)
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
                var fileDownload = new Download { Id = Guid.CreateVersion7(), ContentType = contentType, Url = uri, FileName = filename, TotalSize = totalBytes, Status = FileDownloadStatuses.New, LastModified = lastModified, Created = created, SupportsResume = supportsResume, ETag = etag };
                return fileDownload;
            }
            catch (Exception ex)
            {
                return await Task.FromException<Download>(ex);
            }
        }

        public async Task StartDownloadAsync(Download fileDownload)
        {
            int multipartCount = this.DetermineMaxDownloadParts(fileDownload);

            if (fileDownload.SupportsResume && multipartCount > 1)
            {
                if (fileDownload.Parts == null || !fileDownload.Parts.Any())
                {
                    InitializeDownloadParts(fileDownload, multipartCount);
                }

                await ResumeMultiPartDownloadAsync(fileDownload);
            }
            else
            {
                await StartSingleFileDownloadAsync(fileDownload);
            }
        }

        private void InitializeDownloadParts(Download fileDownload, int partsCount)
        {
            long totalSize = fileDownload.TotalSize;
            long partSize = totalSize / partsCount;
            FileInfo info = new FileInfo(fileDownload.SavePath);

            if (info.Exists == false)
            {
                File.Create(fileDownload.SavePath, 8192, FileOptions.None).Dispose(); // Ensure the file exists before writing parts.
            }

            if (fileDownload.Parts == null)
            {
                fileDownload.Parts = new List<DownloadPart>();
            }

            for (int i = 0; i < partsCount; i++)
            {
                long start = i * partSize;
                long end = (i == partsCount - 1) ? totalSize - 1 : start + partSize - 1;
                string partFilePath = fileDownload.SavePath + $".part{i}";
                // Check if part file exists to support resume.
                long alreadyDownloaded = File.Exists(partFilePath) ? new FileInfo(partFilePath).Length : 0;

                // Only add the part if it hasn't been added yet.
                if (!fileDownload.Parts.Any(p => p.PartNumber == i))
                {
                    fileDownload.Parts.Add(new DownloadPart
                    {
                        PartNumber = i,
                        StartByte = start,
                        EndByte = end,
                        DownloadedBytes = alreadyDownloaded,
                        FilePath = partFilePath,
                        Completed = (alreadyDownloaded >= (end - start + 1))
                    });
                }
            }
        }

        public async Task ResumeMultiPartDownloadAsync(Download fileDownload)
        {
            if (fileDownload.Status == FileDownloadStatuses.New)
            {
                fileDownload.Status = FileDownloadStatuses.InProgress;
            }

            // If the download was paused, create a new cancellation token.
            if (fileDownload.Status == FileDownloadStatuses.Paused)
            {
                if (fileDownload.CancellationTokenSource.IsCancellationRequested)
                {
                    fileDownload.CancellationTokenSource = new CancellationTokenSource();
                }

                fileDownload.Status = FileDownloadStatuses.InProgress;
            }

            var tasks = fileDownload.Parts
                .Where(p => !p.Completed)
                .Select(part => DownloadPartAsync(fileDownload, part))
                .ToList();

            await Task.WhenAll(tasks);

            // Report overall progress.
            long overallDownloaded = fileDownload.Parts.Sum(p => p.DownloadedBytes);
            fileDownload.TotalDownloaded = overallDownloaded;
            fileDownload.PercentCompleted = (double)overallDownloaded / fileDownload.TotalSize * 100;

            // When all parts are complete, merge them.
            if (fileDownload.Parts.All(p => p.Completed))
            {
                MergeParts(fileDownload);
                fileDownload.Status = FileDownloadStatuses.Completed;
            }
        }

        private async Task DownloadPartAsync(Download fileDownload, DownloadPart part)
        {
            if (part.Completed)
                return;

            // Calculate resume point.
            long rangeStart = part.StartByte + part.DownloadedBytes;
            var request = new HttpRequestMessage(HttpMethod.Get, fileDownload.Url)
            {
                Headers = { Range = new RangeHeaderValue(rangeStart, part.EndByte) }
            };

            try
            {
                var response = await _httpClient.SendAsync(request,
                    HttpCompletionOption.ResponseHeadersRead,
                    fileDownload.CancellationTokenSource.Token);
                response.EnsureSuccessStatusCode();

                using var contentStream = await response.Content.ReadAsStreamAsync();

                using (var fileStream = new FileStream(part.FilePath, FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    var buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), fileDownload.CancellationTokenSource.Token)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), fileDownload.CancellationTokenSource.Token);
                        part.DownloadedBytes += bytesRead;

                        // Aggregate progress from all parts.
                        long overallDownloaded = fileDownload.Parts.Sum(p => p.DownloadedBytes);
                        double percent = (double)overallDownloaded / fileDownload.TotalSize * 100;
                        fileDownload.PercentCompleted = percent;

                        if (fileDownload.CancellationTokenSource.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                }
                if (part.DownloadedBytes >= (part.EndByte - part.StartByte + 1))
                {
                    part.Completed = true;
                }
            }
            catch (OperationCanceledException)
            {
                // Download was paused or cancelled.
            }
            catch (Exception ex)
            {
                fileDownload.Status = FileDownloadStatuses.Failed;
                fileDownload.ErrorMessage = ex.Message;
            }
        }

        private async Task StartSingleFileDownloadAsync(Download fileDownload)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, fileDownload.Url);
                FileMode fileMode = FileMode.Create;
                long totalRead = 0;

                // If resuming (download already started) use append mode.
                if (fileDownload.Status == FileDownloadStatuses.Paused)
                {
                    fileMode = FileMode.Append;
                    totalRead = fileDownload.TotalDownloaded;
                }

                fileDownload.Status = FileDownloadStatuses.InProgress;
                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, fileDownload.CancellationTokenSource.Token);
                response.EnsureSuccessStatusCode();

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(fileDownload.SavePath, fileMode, FileAccess.Write, FileShare.None);
                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), fileDownload.CancellationTokenSource.Token)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), fileDownload.CancellationTokenSource.Token);
                    totalRead += bytesRead;
                    fileDownload.TotalDownloaded = totalRead;
                    double percent = (double)totalRead / fileDownload.TotalSize * 100;
                    fileDownload.PercentCompleted = percent;

                    if (fileDownload.CancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }
                }
                fileDownload.Status = FileDownloadStatuses.Completed;

                File.SetCreationTime(fileDownload.SavePath, fileDownload.Created);
                File.SetLastWriteTime(fileDownload.SavePath, fileDownload.LastModified);
                File.SetLastAccessTime(fileDownload.SavePath, DateTime.Now);
            }
            catch (OperationCanceledException)
            {
                // The download was paused or cancelled.
            }
            catch (Exception ex)
            {
                fileDownload.Status = FileDownloadStatuses.Failed;
                fileDownload.ErrorMessage = ex.Message;
            }
        }
        private void MergeParts(Download fileDownload)
        {
            using (var destination = new FileStream(fileDownload.SavePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                foreach (var part in fileDownload.Parts.OrderBy(p => p.PartNumber))
                {
                    using (var source = new FileStream(part.FilePath, FileMode.Open, FileAccess.Read))
                    {
                        source.CopyTo(destination);
                    }
                }
            }

            foreach (var part in fileDownload.Parts)
            {
                if (File.Exists(part.FilePath))
                    File.Delete(part.FilePath);
            }
        }

        public int DetermineMaxDownloadParts(Download fileDownload)
        {
            int defaultMaxParts = this._settingsService.GetMaxConcurrentDownloads();

            if (!fileDownload.SupportsResume)
                return 1;

            if (fileDownload.TotalSize < 10 * 1024 * 1024) // 10 MB threshold.
                return 1;

            int estimatedParts = (int)(fileDownload.TotalSize / (50 * 1024 * 1024)) + 1;

            int partsCount = Math.Min(defaultMaxParts, Math.Max(2, estimatedParts));
            return partsCount;
        }

        public async Task PauseDownload(Download fileDownload)
        {
            fileDownload.Status = FileDownloadStatuses.Paused;
            await fileDownload.CancellationTokenSource.CancelAsync();

        }

        public async Task CancelDownload(Download fileDownload)
        {
            await fileDownload.CancellationTokenSource.CancelAsync();
            fileDownload.Status = FileDownloadStatuses.Cancelled;
        }

        private async Task<string> GetEtag(Download download)
        {
            var request = new HttpRequestMessage(HttpMethod.Head, download.Url);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return response.Headers.ETag?.Tag ?? string.Empty;
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
