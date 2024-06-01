using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

public class DoubleToPercentStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var v = (double)value;
        return v.ToString("P");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}