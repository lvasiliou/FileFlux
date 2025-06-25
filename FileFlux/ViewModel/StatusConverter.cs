using FileFlux.Localization;
using FileFlux.Model;

using System.Globalization;

namespace FileFlux;

public class StatusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var textVal = value?.ToString() ?? string.Empty;

        if (value is FileDownloadStatuses)
        {
            switch (value)
            {
                case FileDownloadStatuses.Downloading:
                    textVal = App_Resources.DownloadStatusInProgress;
                    break;
                case FileDownloadStatuses.Pending:
                    textVal = App_Resources.DownloadStatusNew;
                    break;
                case FileDownloadStatuses.Canceled:
                    textVal = App_Resources.DownloadStatusCancelled;
                    break;
                case FileDownloadStatuses.Failed:
                    textVal = App_Resources.DownloadStatusFailed;
                    break;
                case FileDownloadStatuses.Completed:
                    textVal = App_Resources.DownloadStatusCompleted;
                    break;
                case FileDownloadStatuses.Paused:
                    textVal = App_Resources.DownloadStatusPaused;
                    break;
                case FileDownloadStatuses.GetMetadata:
                    textVal = App_Resources.DownloadStatusGetMetadata;
                    break;
                case FileDownloadStatuses.Measuring:
                    textVal = App_Resources.DownloadStatusMeasuring;
                    break;
            }
        }

        return textVal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}
