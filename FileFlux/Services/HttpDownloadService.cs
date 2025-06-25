namespace FileFlux.Services
{
    using FileFlux.Contracts;
    using FileFlux.Model;

    using System.Diagnostics;
    using System.IO;
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
            download.Status = FileDownloadStatuses.Downloading;

            if (!await _originGuard.IsResourceValidAsync(download))
            {
                download.Status = FileDownloadStatuses.Failed;
                return;
            }
            int bufferSize = download.OptimalBufferSize;
            int chunks = download.OptimalChunks ?? _maxDegreeOfParallelism;


            Directory.CreateDirectory(Path.GetDirectoryName(download.FilePath)!);

            File.Create(download.FilePath).Close(); // Ensure the file exists

            var useParts = download.SupportsResume && chunks > 1;

            if (!useParts)
            {
                using var response = await _httpClient.GetAsync(download.Uri, HttpCompletionOption.ResponseHeadersRead, download.CancellationTokenSource.Token);
                response.EnsureSuccessStatusCode();

                using var contentStream = await response.Content.ReadAsStreamAsync(download.CancellationTokenSource.Token);

                var buffer = new byte[download.OptimalBufferSize];
                int bytesRead;

                using (var fileStream = new FileStream(download.FilePath, FileMode.Append, FileAccess.Write, FileShare.None))
                {

                    while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), download.CancellationTokenSource.Token)) > 0)
                    {

                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), download.CancellationTokenSource.Token);
                        download.BytesDownloaded += bytesRead;


                        double percent = (double)download.BytesDownloaded / download.TotalBytes * 100;
                        download.PercentCompleted = percent;
                    }
                }
            }
            else
            {
                int partCount = chunks;
                long partSize = download.TotalBytes / partCount;
                download.Parts.Clear();

                for (int i = 0; i < partCount; i++)
                {
                    long start = i * partSize;
                    long end = (i == partCount - 1) ? download.TotalBytes - 1 : (start + partSize - 1);
                    download.Parts.Add(new DownloadPart
                    {
                        Index = i,
                        Start = start,
                        End = end,
                        PartFilePath = download.FilePath + $".part{i}"
                    });
                }

                var tasks = download.Parts.Select(p => DownloadPartAsync(download, p)).ToList();
                await Task.WhenAll(tasks);

                download.Status = FileDownloadStatuses.Merging;
                using var output = new FileStream(download.FilePath, FileMode.Create, FileAccess.Write);
                foreach (var part in download.Parts.OrderBy(p => p.Index))
                {
                    using var partStream = new FileStream(part.PartFilePath, FileMode.Open);
                    await partStream.CopyToAsync(output);
                    partStream.Close();
                    File.Delete(part.PartFilePath);
                }
            }

            download.Status = FileDownloadStatuses.Completed;
            download.DateCompleted = DateTimeOffset.UtcNow;
        }

        private async Task DownloadPartAsync(Download download, DownloadPart part)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(part.PartFilePath)!);

            var request = new HttpRequestMessage(HttpMethod.Get, download.Uri);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(part.Start, part.End);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, download.CancellationTokenSource.Token);
            response.EnsureSuccessStatusCode();

            using var contentStream = await response.Content.ReadAsStreamAsync(download.CancellationTokenSource.Token);

            var buffer = new byte[download.OptimalBufferSize];
            int bytesRead;

            using (var fileStream = new FileStream(part.PartFilePath, FileMode.Append, FileAccess.Write, FileShare.None))
            {

                while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), download.CancellationTokenSource.Token)) > 0)
                {

                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), download.CancellationTokenSource.Token);
                    part.DownloadedBytes += bytesRead;

                    long overallDownloaded = download.Parts.Sum(p => p.DownloadedBytes);
                    double percent = (double)overallDownloaded / download.TotalBytes * 100;
                    download.PercentCompleted = percent;
                }
            }

            part.Completed = true;
        }

        public Task PauseDownloadAsync(Download download)
        {
            download.Status = FileDownloadStatuses.Paused;
            download.CancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        public Task CancelDownloadAsync(Download download)
        {
            download.Status = FileDownloadStatuses.Canceled;
            download.CancellationTokenSource.Cancel();

            if (File.Exists(download.FilePath))
                File.Delete(download.FilePath);

            foreach (var part in download.Parts)
            {
                if (File.Exists(part.PartFilePath))
                    File.Delete(part.PartFilePath);
            }

            return Task.CompletedTask;
        }

        public async Task<(int bufferSize, int chunkCount)> BenchmarkDownloadStrategyAsync(string url, long fileSizeBytes)
        {
            int[] bufferSizes = { 8192, 16384, 32768, 65536, 131072 };
            int bestBufferSize = 8192;
            double bestSpeedMbps = 0;

            foreach (int bufferSize in bufferSizes)
            {
                var speed = await MeasureDownloadSpeedAsync(url, bufferSize);
                if (speed > bestSpeedMbps)
                {
                    bestSpeedMbps = speed;
                    bestBufferSize = bufferSize;
                }
            }

            var targetChunkSize = Math.Clamp((long)(bestSpeedMbps * 125_000), 5 * 1024 * 1024, 20 * 1024 * 1024);
            int chunkCount = (int)Math.Clamp(fileSizeBytes / targetChunkSize, 1, Environment.ProcessorCount);

            return (bestBufferSize, chunkCount);
        }

        private async Task<double> MeasureDownloadSpeedAsync(string url, int bufferSize)
        {
            long testBytes = 5_000_000;
            try
            {
                using var client = new HttpClient();
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                var buffer = new byte[bufferSize];
                long totalRead = 0;
                var sw = Stopwatch.StartNew();

                while (totalRead < testBytes)
                {
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read == 0) break;
                    totalRead += read;
                }

                sw.Stop();
                return totalRead > 0 ? (totalRead * 8) / 1_000_000.0 / sw.Elapsed.TotalSeconds : 0;
            }
            catch
            {
                return 0; // fallback if failed
            }
        }
    }
}