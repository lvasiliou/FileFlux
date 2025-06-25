
using FileFlux.Contracts;
using FileFlux.Model;

namespace FileFlux.Services
{
    internal class FtpDownloadService : IDownloadService
    {
        public Task CancelDownloadAsync(Download fileDownload)
        {
            throw new NotImplementedException();
        }

        public Task<Download> GetMetadata(Uri uri)
        {
            throw new NotImplementedException();
        }

        public Task PauseDownloadAsync(Download fileDownload)
        {
            throw new NotImplementedException();
        }

        public Task StartDownloadAsync(Download fileDownload)
        {
            throw new NotImplementedException();
        }

        public Task<(int bufferSize, int chunkCount)> BenchmarkDownloadStrategyAsync(string url, long testBytes)
        {
            throw new NotImplementedException();
        }
    }
}
