using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;


namespace ClassIsland.Core.Converters;

public class SizeDoubleToCornerRadiusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            double v1 => new CornerRadius(v1 / 2.0), 
            int v2 => new CornerRadius(v2 / 2.0),
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}