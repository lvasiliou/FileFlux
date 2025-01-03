using FileFlux.Model;

using System.Globalization;

namespace FileFlux;

public class StatusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var textVal = value.ToString();

        if (value is FileDownloadStatuses)
        {
            switch (value)
            {
                case FileDownloadStatuses.InProgress:
                    textVal = "In Progress";
                    break;
                case FileDownloadStatuses.New:
                    textVal = "New";
                    break;
                case FileDownloadStatuses.Cancelled:
                    textVal = "Cancelled";
                    break;
                case FileDownloadStatuses.Failed:
                    textVal = "Failed";
                    break;
                case FileDownloadStatuses.Completed:
                    textVal = "Finished";
                    break;
                case FileDownloadStatuses.Paused:
                    textVal = "Paused";
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
