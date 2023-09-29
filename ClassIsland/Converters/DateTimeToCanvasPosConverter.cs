using System;
using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Converters;

public class DateTimeToCanvasPosConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var v = (DateTime)value;
        return v.TimeOfDay.Ticks / 1000000000.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}