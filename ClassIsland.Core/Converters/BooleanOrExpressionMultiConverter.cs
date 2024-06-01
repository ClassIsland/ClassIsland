using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

public class BooleanOrExpressionMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.ToList().Exists(i => i as bool? == true);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[] { };
    }
}