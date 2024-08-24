using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

public class TimeLongToPercentMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var a = (long)values[0];
        var b = (long)values[1];
        return $"{(a*1.0)/(b*1.0):P0}";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[] { };
    }
}