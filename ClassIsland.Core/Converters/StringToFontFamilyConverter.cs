using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

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
        return v.Source;
    }
}