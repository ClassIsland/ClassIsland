using System.Globalization;

using Avalonia.Data.Converters;

namespace ClassIsland.Core.Converters;

public class StringToRadioButtonSelectionConverter : IMultiValueConverter
{
    public object Convert(IList<object?> value, Type targetType, object parameter, CultureInfo culture)
    {
        var reverse = Equals(parameter, true);
        if (value[0] is string r1 && value[1] is string p1)
        {
            return reverse ? r1 != p1 : r1 == p1;
        }
        if (value[0] is Guid r2 && value[1] is Guid p2)
        {
            return reverse ? r2 != p2 : r2 == p2;
        }

        return false;
    }
}