using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

using ClassIsland.Core;
using ClassIsland.Core.Models.Profile;

namespace ClassIsland.Converters;

public class ClassPlanDictionaryValueAccessConverter : IValueConverter
{
    public ObservableDictionary<string, TimeLayout> SourceDictionary
    {
        get;
        set;
    } = new();

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var k = (string)value;
        return k == "" ? null : new KeyValuePair<string, TimeLayout>(k, SourceDictionary[k]);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var k = (KeyValuePair<string, TimeLayout>?)value ?? new KeyValuePair<string, TimeLayout>();
        return k.Key;
    }
}