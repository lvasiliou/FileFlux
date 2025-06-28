namespace FileFlux.ViewModel;

using System.Globalization;

public class HeightToFontSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double height)
        {
            // Adjust the scale factor as needed
            return height * 0.8;
        }
        return 12.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
