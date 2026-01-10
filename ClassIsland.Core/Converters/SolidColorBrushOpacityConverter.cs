using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;


namespace ClassIsland.Core.Converters;

public class SolidColorBrushOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var v = (SolidColorBrush)value;
        return new SolidColorBrush(v.Color)
        {
            Opacity = double.Parse((string)parameter)
        };
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}