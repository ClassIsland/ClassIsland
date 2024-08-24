using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

public class StringConnectConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var a = (string)values[0];
        var b = (string)values[1];
        return a + "\\" + b;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return ((string)value).Split('\\');
    }
}