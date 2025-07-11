using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ClassIsland.Converters;

public class StringToFontFamilyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var v = (string)value;
        var fontFamily = FontFamily.Parse(v);
        if (fontFamily.ToString() == MainWindow.DefaultFontFamily.ToString())
        {
            return MainWindow.DefaultFontFamily;
        }
        return fontFamily;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var v = (FontFamily)value;
        if (v.Key != null)
        {
            return v.Key.ToString().Replace("compositefont:", "");
        }
        return v.ToString();
    }
}