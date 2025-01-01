using FileFlux.Model;

namespace FileFlux.Contracts
{
    public interface IDownloadService
    {
        public Task<FileDownload> GetMetadata(Uri uri);

        public Task StartDownloadAsync(FileDownload fileDownload);

        public void PauseDownload(FileDownload fileDownload);
    }
}
