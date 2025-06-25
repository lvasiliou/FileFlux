namespace FileFlux.Model;
public enum FileDownloadStatuses
{
    Pending,
    Downloading,
    Completed,
    Canceled,
    Paused,
    Failed,
    Verifying,
    GetMetadata,
    Measuring,
    Merging
}