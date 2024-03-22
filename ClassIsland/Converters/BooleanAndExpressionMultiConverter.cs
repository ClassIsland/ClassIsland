using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace ClassIsland.Converters;

public class BooleanAndExpressionMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.ToList().All(i => i as bool? == true);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[] { };
    }
}