using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;


namespace ClassIsland.Core.Converters;

public class SolidColorBrushToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var v = value as SolidColorBrush;
        return Color.FromArgb((byte)(v?.Opacity * 255 ?? 255), v?.Color.R ?? 0, v?.Color.G ?? 0, v?.Color.B ?? 0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}