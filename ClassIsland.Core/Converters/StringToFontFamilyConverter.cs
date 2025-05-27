using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;


namespace ClassIsland.Core.Converters;

public class StringToFontFamilyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var v = (string)value;
        return new FontFamily(v);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var v = (FontFamily)value;
        return v.Name;
    }
}