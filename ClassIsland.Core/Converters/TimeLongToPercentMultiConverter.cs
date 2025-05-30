using System.Globalization;

using Avalonia.Data.Converters;

namespace ClassIsland.Core.Converters;

public class TimeLongToPercentMultiConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object parameter, CultureInfo culture)
    {
        var a = values[0] as long?;
        var b = values[1] as long?;
        return $"{(a*1.0)/(b*1.0):P0}";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return Array.Empty<object>();
    }
}