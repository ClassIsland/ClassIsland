using System.Globalization;

using Avalonia.Data.Converters;

namespace ClassIsland.Core.Converters;

public class DateTimeToCanvasPosConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object parameter, CultureInfo culture)
    {
        var v = (DateTime)values[0];
        var s = values[1] as double? ?? 1.0;
        return v.TimeOfDay.Ticks / 1000000000.0 * s;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return Array.Empty<object>();
    }
}