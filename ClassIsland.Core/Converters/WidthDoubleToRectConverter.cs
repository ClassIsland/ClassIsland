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
        var w = (double)values[0];
        var gw = (double)values[1];
        var gh = (double)values[2];
        var l = (int)values[3];

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

        //Debug.WriteLine(gw);
        return new RectangleGeometry(new Rect(new Point(px, 0), new Size(w, gh)));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[] { };
    }
}