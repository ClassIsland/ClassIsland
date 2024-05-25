using System;
using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Converters;

public class StringToRadioButtonSelectionConverter : IMultiValueConverter
{
    private string _valueRaw = "";

    public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
    {
        var r = (string)value[0];
        var p = (string)value[1];
        _valueRaw = p;
        return r == p;
    }
    

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        if ((bool)value)
        {
            return new[]
            {
                _valueRaw,
                _valueRaw
            };
        }

        return null;
    }
}