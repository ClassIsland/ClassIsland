using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;


namespace ClassIsland.Core.Converters;

public class StringColorHexToSolidBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string text && Color.TryParse(text, out var color)
            ? new SolidColorBrush(color)
            : Brushes.Transparent;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

