using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

public class IntToRadioButtonSelectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var r = (int)value;
        var p = (string)parameter;
        return r.ToString() == p;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if ((bool)value)
        {
            return System.Convert.ToInt32(parameter);
        }

        return null;
    }
}