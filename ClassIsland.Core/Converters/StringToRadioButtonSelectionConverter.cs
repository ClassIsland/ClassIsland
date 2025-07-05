using System.Globalization;

using Avalonia.Data.Converters;

namespace ClassIsland.Core.Converters;

public class StringToRadioButtonSelectionConverter : IMultiValueConverter
{
    private string _valueRaw = "";

    public object Convert(IList<object?> value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value[0] is not string r || value[1] is not string p)
        {
            return false;
        }
        _valueRaw = p;
        return r == p;
    }
}