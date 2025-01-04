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
                case FileDownloadStatuses.InProgress:
                    textVal = App_Resources.DownloadStatusInProgress;
                    break;
                case FileDownloadStatuses.New:
                    textVal = App_Resources.DownloadStatusNew;
                    break;
                case FileDownloadStatuses.Cancelled:
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
            }
        }

        return textVal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}
