using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ClassIsland.Core.Converters;

public class WidthDoubleToRectConverter : IMultiValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var v = (double)value;
        return new RectangleGeometry(new Rect(new Point(0, 0), new Size(v, 40.0)));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var w = Math.Max(0, values[0] as double? ?? 0);
        var gw = Math.Max(0, values[1] as double? ?? 0);
        var gh = Math.Max(0, values[2] as double? ?? 0);
        var l = values[3] as int? ?? 0;
        var rX = values[4] as double? ?? 0;
        var rY = values[5] as double? ?? 0; 

        var px = 0.0;
        if (double.IsNaN(gw))
        {
            px = 0.0;
        } else switch (l)
        {
            case 0: case 3:
                px = 0.0;
                break;
            case 1: case 4:
                px = (gw - w) / 2;
                break;
            case 2: case 5:
                px = gw - w;
                break;
        }

        //Debug.WriteLine(w);
        return new RectangleGeometry(new Rect(new Point(px, 0), new Size(w, gh)), rX, rY);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[] { };
    }
}