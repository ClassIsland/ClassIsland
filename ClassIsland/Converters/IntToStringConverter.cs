using System;
using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Converters;

public class IntToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var r = System.Convert.ToString(value);
        return r ?? "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (targetType == typeof(int))
        {
            return int.Parse((string)value);
        }

        if (targetType == typeof(double))
        {
            return double.Parse((string)value);
        }

        if (targetType == typeof(float))
        {
            return float.Parse((string)value);
        }

        return 0;
    }
}