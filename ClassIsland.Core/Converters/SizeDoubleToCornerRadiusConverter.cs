using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;


namespace ClassIsland.Core.Converters;

public class SizeDoubleToCornerRadiusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var p = double.TryParse(parameter as string ?? "", out var r1) ? r1 : 2.0;
        return value switch
        {
            double v1 => new CornerRadius(v1 / p), 
            int v2 => new CornerRadius(v2 / p),
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}