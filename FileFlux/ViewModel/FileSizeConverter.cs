using FileFlux.Utilities;

using System.Globalization;

namespace FileFlux
{
    internal class FileSizeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is long sizeInBytes)
            {
                string[] sizeUnits = { Constants.UnitBytes, Constants.UnitKiloBytes, Constants.UnitMegaBytes, Constants.UnitGigaBytes, Constants.UnitTeraBytes };
                int unitIndex = 0;
                double size = sizeInBytes;
                while (size >= 1024 && unitIndex < sizeUnits.Length - 1)
                {
                    unitIndex++; size /= 1024;
                }

                return $"{size:0.##} {sizeUnits[unitIndex]}";
            }
            return value ?? string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value ?? string.Empty;
        }
    }
}
