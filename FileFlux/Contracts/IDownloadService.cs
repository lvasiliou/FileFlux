using FileFlux.Model;

namespace FileFlux.Contracts
{
    public interface IDownloadService
    {
        public Task<Download> GetMetadata(Uri uri);

        public Task StartDownloadAsync(Download fileDownload);

        public Task PauseDownload(Download fileDownload);
        
        public Task CancelDownload(Download fileDownload);
    }
}
