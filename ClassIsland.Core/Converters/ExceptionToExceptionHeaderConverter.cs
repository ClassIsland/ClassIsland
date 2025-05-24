using System.Globalization;
using System.Windows.Data;

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

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}