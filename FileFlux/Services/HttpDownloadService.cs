namespace FileFlux.Services
{
    using FileFlux.Contracts;
    using FileFlux.Model;

    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Mime;

    public class HttpDownloadService : IDownloadService
    {
        private readonly HttpClient _httpClient;
        private readonly IOriginGuard _originGuard;
        private readonly SettingsService _settingsService;
        private readonly int _maxDegreeOfParallelism;

        public HttpDownloadService(HttpClient httpClient, HttpOriginGuard originGuard, SettingsService settingsService)
        {
            _httpClient = httpClient;
            _originGuard = originGuard;
            _settingsService = settingsService;
            _maxDegreeOfParallelism = this._settingsService.GetMaxConcurrentDownloads();
        }

        public async Task<Download> GetMetadata(Uri uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Head, uri);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var contentDisposition = response.Content.Headers.ContentDisposition;
            var totalBytes = contentDisposition?.Size ?? response.Content.Headers.ContentLength ?? 0;

            var fileName = Path.GetFileName(uri.LocalPath);


            var download = new Download
            {
                Uri = uri,
                FileName = contentDisposition?.FileName ?? Path.GetFileName(uri.LocalPath),
                TotalBytes = contentDisposition?.Size ?? response.Content.Headers.ContentLength ?? 0,
                ContentType = response.Content.Headers.ContentType?.MediaType,
                SupportsResume = response.Headers.AcceptRanges.Contains("bytes"),
                ETag = response.Headers.ETag?.Tag,
                LastModified = response.Content.Headers.LastModified,
                DateCreated = contentDisposition?.CreationDate?.UtcDateTime ?? DateTime.UtcNow,
                DateAdded = DateTimeOffset.UtcNow,
                Status = FileDownloadStatuses.GetMetadata
            };

            return download;
        }

        public async Task StartDownloadAsync(Download download)
        {
            var cts = download.CancellationTokenSource;
            var token = cts.Token;
            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                sw.Start();

                download.Status = FileDownloadStatuses.Downloading;

                if (!await _originGuard.IsResourceValidAsync(download))
                {
                    download.Status = FileDownloadStatuses.Failed;
                    return;
                }
                int bufferSize = download.OptimalBufferSize;
                int chunks = download.OptimalChunks ?? _maxDegreeOfParallelism;


                Directory.CreateDirectory(Path.GetDirectoryName(download.FilePath)!);

                var useParts = download.SupportsResume && chunks > 1;

                if (!useParts)
                {
                    var canResume = download.SupportsResume && download.BytesDownloaded > 0 && File.Exists(download.FilePath);

                    if (canResume)
                    {
                        // Reconcile in-memory progress with bytes actually flushed to disk.
                        download.BytesDownloaded = new FileInfo(download.FilePath).Length;
                    }
                    else
                    {
                        File.Create(download.FilePath).Close();
                        download.BytesDownloaded = 0;
                    }

                    var request = new HttpRequestMessage(HttpMethod.Get, download.Uri);
                    if (canResume && download.BytesDownloaded < download.TotalBytes)
                    {
                        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(download.BytesDownloaded, download.TotalBytes - 1);
                    }

                    using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                    response.EnsureSuccessStatusCode();

                    if (request.Headers.Range != null && response.StatusCode != HttpStatusCode.PartialContent)
                    {
                        // Server ignored Range header — discard partial data and write the full body from byte 0.
                        File.Create(download.FilePath).Close();
                        download.BytesDownloaded = 0;
                    }

                    using var contentStream = await response.Content.ReadAsStreamAsync(token);

                    var buffer = new byte[download.OptimalBufferSize];
                    int bytesRead;

                    using (var fileStream = new FileStream(download.FilePath, FileMode.Append, FileAccess.Write, FileShare.None))
                    {

                        while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), token)) > 0)
                        {

                            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                            download.BytesDownloaded += bytesRead;


                            double percent = (double)download.BytesDownloaded / download.TotalBytes * 100;
                            download.PercentCompleted = percent;
                        }
                    }
                }
                else
                {
                    if (download.Parts.Count == 0)
                    {
                        int partCount = chunks;
                        long partSize = download.TotalBytes / partCount;

                        for (int i = 0; i < partCount; i++)
                        {
                            long start = i * partSize;
                            long end = (i == partCount - 1) ? download.TotalBytes - 1 : (start + partSize - 1);
                            var partFilePath = download.FilePath + $".part{i}";

                            // Discard stray part files from a previous aborted attempt.
                            if (File.Exists(partFilePath))
                            {
                                File.Delete(partFilePath);
                            }

                            download.Parts.Add(new DownloadPart
                            {
                                Index = i,
                                Start = start,
                                End = end,
                                PartFilePath = partFilePath
                            });
                        }
                    }

                    await RunPartsAdaptivelyAsync(download, token);

                    download.Status = FileDownloadStatuses.Merging;
                    using (var output = new FileStream(download.FilePath, FileMode.Create, FileAccess.Write))
                    {
                        foreach (var part in download.Parts.OrderBy(p => p.Index))
                        {
                            using (var partStream = new FileStream(part.PartFilePath, FileMode.Open, FileAccess.Read))
                            {
                                await partStream.CopyToAsync(output);
                            }
                            File.Delete(part.PartFilePath);
                        }
                    }
                }

                download.Status = FileDownloadStatuses.Completed;
                download.DateCompleted = DateTimeOffset.UtcNow;
                sw.Stop();
                Debug.WriteLine($"Download completed in {sw.ElapsedMilliseconds} ms");
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                // Status was set by PauseDownloadAsync/CancelDownloadAsync — leave it.
                // Pause keeps part files for resume; Cancel discards them.
                if (download.Status == FileDownloadStatuses.Canceled)
                {
                    DeleteArtifacts(download);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Download failed: {ex.Message}");
                download.Status = FileDownloadStatuses.Failed;
                download.ErrorMessage = ex.Message;
                DeleteArtifacts(download);
            }
        }

        private static void DeleteArtifacts(Download download)
        {
            foreach (var part in download.Parts)
            {
                try { if (File.Exists(part.PartFilePath)) File.Delete(part.PartFilePath); } catch { }
            }
            download.Parts.Clear();
            download.BytesDownloaded = 0;
            download.PercentCompleted = 0;

            try { if (File.Exists(download.FilePath)) File.Delete(download.FilePath); } catch { }
        }

        private async Task DownloadPartAsync(Download download, DownloadPart part, CancellationToken token)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(part.PartFilePath)!);

            // Reconcile in-memory progress with bytes actually flushed to disk.
            part.DownloadedBytes = File.Exists(part.PartFilePath) ? new FileInfo(part.PartFilePath).Length : 0;

            long partLength = part.End - part.Start + 1;
            if (part.DownloadedBytes >= partLength)
            {
                part.Completed = true;
                return;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, download.Uri);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(part.Start + part.DownloadedBytes, part.End);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();

            if (response.StatusCode != HttpStatusCode.PartialContent)
            {
                throw new InvalidOperationException($"Expected 206 Partial Content for part {part.Index}, got {(int)response.StatusCode}.");
            }

            using var contentStream = await response.Content.ReadAsStreamAsync(token);

            var buffer = new byte[download.OptimalBufferSize];
            int bytesRead;

            using (var fileStream = new FileStream(part.PartFilePath, FileMode.Append, FileAccess.Write, FileShare.None))
            {

                while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), token)) > 0)
                {

                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                    part.DownloadedBytes += bytesRead;

                    long overallDownloaded = download.Parts.Sum(p => p.DownloadedBytes);
                    double percent = (double)overallDownloaded / download.TotalBytes * 100;
                    download.PercentCompleted = percent;
                }
            }

            part.Completed = true;
        }

        // Starts with one connection and adds another every ~2s as long as per-connection
        // throughput keeps scaling. Stops ramping when it doesn't, but keeps existing
        // connections fed from the remaining planned parts.
        private async Task RunPartsAdaptivelyAsync(Download download, CancellationToken token)
        {
            const int rampIntervalMs = 2000;
            const double scalingThreshold = 0.85; // per-connection rate must hold ≥85% of the previous tick

            var pending = new Queue<DownloadPart>(download.Parts.Where(p => !p.Completed));
            var active = new List<Task>();
            int targetConcurrency = 1;
            double prevPerConnMbps = 0;
            bool saturated = false;

            void FillToTarget()
            {
                while (active.Count < targetConcurrency && pending.Count > 0)
                {
                    active.Add(DownloadPartAsync(download, pending.Dequeue(), token));
                }
            }

            FillToTarget();

            while (active.Count > 0)
            {
                long beforeBytes = download.Parts.Sum(p => p.DownloadedBytes);
                var rampTick = Task.Delay(rampIntervalMs, token);
                var completed = await Task.WhenAny(active.Concat(new[] { rampTick }).ToArray());

                if (completed == rampTick)
                {
                    long afterBytes = download.Parts.Sum(p => p.DownloadedBytes);
                    double totalMbps = (afterBytes - beforeBytes) * 8.0 / 1_000_000.0 / (rampIntervalMs / 1000.0);
                    double perConnMbps = totalMbps / active.Count;

                    if (!saturated && pending.Count > 0)
                    {
                        if (prevPerConnMbps > 0 && perConnMbps < prevPerConnMbps * scalingThreshold)
                        {
                            saturated = true; // adding the last connection didn't pay off
                        }
                        else
                        {
                            prevPerConnMbps = perConnMbps;
                            targetConcurrency = Math.Min(targetConcurrency + 1, download.Parts.Count);
                        }
                    }
                }
                else
                {
                    active.Remove(completed);
                    await completed; // surface any exception
                }

                FillToTarget();
            }
        }

        public Task PauseDownloadAsync(Download download)
        {
            download.Status = FileDownloadStatuses.Paused;
            download.CancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        public Task CancelDownloadAsync(Download download)
        {
            // If a download task is in-flight, its catch block will run DeleteArtifacts
            // once the FileStreams have safely disposed. For non-running downloads
            // (paused, failed, completed, pending) there's no catch to fire, so we
            // clean up directly here.
            var hadActiveTask = download.Status == FileDownloadStatuses.Downloading
                                || download.Status == FileDownloadStatuses.Merging;

            download.Status = FileDownloadStatuses.Canceled;
            download.CancellationTokenSource.Cancel();

            if (!hadActiveTask)
            {
                DeleteArtifacts(download);
            }

            return Task.CompletedTask;
        }

        public Task<(int bufferSize, int chunkCount)> BenchmarkDownloadStrategyAsync(string url, long fileSizeBytes)
        {
            int bufferSize = GetOptimalBufferSize(fileSizeBytes);

            // Aim for ~10 MB per chunk; cap by the user's MaxConcurrentDownloads setting.
            const long targetChunkSize = 10L * 1024 * 1024;
            int chunkCount = (int)Math.Clamp(fileSizeBytes / targetChunkSize, 1L, _maxDegreeOfParallelism);

            return Task.FromResult((bufferSize, chunkCount));
        }

        private static int GetOptimalBufferSize(long fileSizeBytes)
        {
            if (fileSizeBytes < 1 * 1024 * 1024) return 8192;         // < 1MB
            if (fileSizeBytes < 10 * 1024 * 1024) return 16384;       // 1–10MB
            if (fileSizeBytes < 50 * 1024 * 1024) return 32768;       // 10–50MB
            return 65536;                                             // > 50MB
        }
    }
}