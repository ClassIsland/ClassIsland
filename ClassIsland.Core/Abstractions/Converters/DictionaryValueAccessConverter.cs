using System.Globalization;
using System.Windows.Data;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Shared;

namespace ClassIsland.Core.Abstractions.Converters;

public class DictionaryValueAccessConverter<T> : IMultiValueConverter
{

    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
        {
            return null;
        }
        var dict = values[0] as IDictionary<string, T>;
        var key = values[1] as string;
        if (dict?.TryGetValue(key ?? "", out var o) == true)
        {
            return o;
        }

        return null;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[] { };
    }
}