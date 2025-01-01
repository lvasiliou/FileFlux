using System.Globalization;

namespace FileFlux
{
    internal class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is long sizeInBytes)
            {
                string[] sizeUnits = { "B", "KB", "MB", "GB", "TB" };
                int unitIndex = 0;
                double size = sizeInBytes;
                while (size >= 1024 && unitIndex < sizeUnits.Length - 1)
                {
                    unitIndex++; size /= 1024;
                }

                return $"{size:0.##} {sizeUnits[unitIndex]}";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
