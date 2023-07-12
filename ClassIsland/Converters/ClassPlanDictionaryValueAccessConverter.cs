using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using ClassIsland.Models;

namespace ClassIsland.Converters;

public class ClassPlanDictionaryValueAccessConverter : IValueConverter
{
    public ObservableDictionary<string, TimeLayout> SourceDictionary
    {
        get;
        set;
    } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var k = (string)value;
        return new KeyValuePair<string, TimeLayout>(k, SourceDictionary[k]);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var k = (KeyValuePair<string, TimeLayout>)value;
        return k.Key;
    }
}