using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;
public class BooleanToVisibilityReConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if ((bool)value)
        {
            return Visibility.Collapsed;
        }
        else
        {
            return Visibility.Visible;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
