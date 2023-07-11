using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Converters;

public class KeyValuePairToValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? null : ((KeyValuePair<object, object>)value).Value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}