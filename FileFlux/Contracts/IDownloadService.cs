using FileFlux.Model;

namespace FileFlux.Contracts
{
    public interface IDownloadService
    {
        public Task<Download> GetMetadata(Uri uri);

        public Task StartDownloadAsync(Download fileDownload);

        public Task PauseDownloadAsync(Download fileDownload);
        
        public Task CancelDownloadAsync(Download fileDownload);

        public Task<(int bufferSize, int chunkCount)> BenchmarkDownloadStrategyAsync(string url, long testBytes);
    }
}
