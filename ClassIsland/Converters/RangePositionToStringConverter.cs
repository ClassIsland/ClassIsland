using System;
using System.Globalization;
using System.Windows.Data;

using unvell.ReoGrid;

namespace ClassIsland.Converters;

public class RangePositionToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var v = (RangePosition)value;
        return v == RangePosition.Empty ? "" : v.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}