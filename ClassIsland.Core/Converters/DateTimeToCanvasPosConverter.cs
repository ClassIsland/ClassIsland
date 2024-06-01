using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

public class DateTimeToCanvasPosConverter : IMultiValueConverter
{

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var v = (DateTime)values[0];
        var s = values[1] as double? ?? 1.0;
        return v.TimeOfDay.Ticks / 1000000000.0 * s;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[] { };
    }
}