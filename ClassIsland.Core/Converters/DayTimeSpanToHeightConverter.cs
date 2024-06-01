using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

public class DayTimeSpanToHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = (double)value;
        return TimeSpan.FromDays(1).Ticks / 1000000000.0 * s;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}