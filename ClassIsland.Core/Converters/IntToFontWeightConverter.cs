using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

public class IntToFontWeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return FontWeight.FromOpenTypeWeight((int)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ((FontWeight)value).ToOpenTypeWeight();
    }
}