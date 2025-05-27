using System.Globalization;
using Avalonia.Data.Converters;

namespace ClassIsland.Core.Converters;

public class BooleanOrExpressionMultiConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        return values.ToList().Exists(i => i as bool? == true);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return Array.Empty<object>();
    }
}