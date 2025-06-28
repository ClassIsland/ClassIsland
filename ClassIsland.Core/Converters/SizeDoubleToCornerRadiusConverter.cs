using System.Globalization;
using Avalonia.Data.Converters;


namespace ClassIsland.Core.Converters;

public class SizeDoubleToCornerRadiusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            double v1 => v1 / 2,
            int v2 => v2 / 2,
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}