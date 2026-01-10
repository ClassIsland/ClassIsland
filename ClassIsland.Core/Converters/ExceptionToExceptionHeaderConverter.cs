using System.Globalization;
using Avalonia.Data.Converters;


namespace ClassIsland.Core.Converters;

public class ExceptionToExceptionHeaderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return "";
        }
        return $"{value.GetType()}: {((Exception)value).Message}";
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}