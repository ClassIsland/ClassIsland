using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
namespace ClassIsland.Converters;

public class ColorStringToAvaloniaColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Color.TryParse(value.ToString(), out var color) ? color : BindingOperations.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value is Color color ? color : default).ToString();
    }
}