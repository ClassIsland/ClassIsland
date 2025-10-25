using System.Globalization;

using Avalonia.Data.Converters;

namespace ClassIsland.Core.Converters;

public class TimeSpanDeltaToCanvasPosConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object parameter, CultureInfo culture)
    {
        var v1 = ((TimeSpan)values[0]);
        var v2 = ((TimeSpan)values[1]);
        var s = values[2] as double? ?? 1.0;
        return Math.Max((v2 - v1).Ticks / 1000000000.0 * s, 1.0);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return Array.Empty<object>();
    }
}