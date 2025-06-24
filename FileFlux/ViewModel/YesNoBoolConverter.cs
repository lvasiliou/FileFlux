
using FileFlux.Localization;

using System.Globalization;

namespace FileFlux
{
    public class YesNoBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? App_Resources.YesString : App_Resources.NoString;
            }

            return value?.ToString() ?? string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (string.Compare(value?.ToString(), App_Resources.YesString, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }
            else if (string.Compare(value?.ToString(), App_Resources.NoString, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return false;
            }

            return default(bool);
        }
    }
}
