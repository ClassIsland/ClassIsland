using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Converters;

public class DictionaryKeyFinderMultiConverter<TValue> : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        // values[0]: TValue                        value
        // values[1]: IDictionary<Guid, Subject>    dict
        if (values.Count < 2)
        {
            return Guid.Empty;
        }

        if (values[0] is not TValue value || values[1] is not IDictionary<Guid, TValue> dict)
        {
            return Guid.Empty;
        }

        return dict.FirstOrDefault(x => Equals(x.Value, value)).Key;
    }
}