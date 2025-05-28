using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Converters;

public class SubjectDictionaryKeyFinderMultiConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        // values[0]: Subject                       subject
        // values[1]: IDictionary<string, Subject>  dict
        if (values.Count < 2)
        {
            return "";
        }

        if (values[0] is not Subject subject || values[1] is not IDictionary<string, Subject> dict)
        {
            return "";
        }

        return dict.FirstOrDefault(x => x.Value == subject).Key ?? "";
    }
}