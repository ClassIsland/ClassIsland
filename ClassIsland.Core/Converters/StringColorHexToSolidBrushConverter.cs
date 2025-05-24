using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ClassIsland.Core.Converters;

public class StringColorHexToSolidBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        byte r = 0, g = 0, b = 0;
        if (value is string && ((string)value).Length == 7 && ((string)value)[0] == '#')
        {
            var code = (string)value;
            var rh = "" + code[1] + code[2];
            var gh = "" + code[3] + code[4];
            var bh = "" + code[5] + code[6];
            r = System.Convert.ToByte(rh, 16);
            g = System.Convert.ToByte(gh, 16);
            b = System.Convert.ToByte(bh, 16);
        }

        return new SolidColorBrush(Color.FromRgb(r, g, b));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

