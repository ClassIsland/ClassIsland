using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using ClassIsland.Models;

namespace ClassIsland.Converters;

public class DictionaryValueAccessConverter : IValueConverter
{
    public ObservableDictionary<string, Subject> SourceDictionary
    {
        get;
        set;
    } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var k = (string)value;
        return SourceDictionary[k].Name;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}