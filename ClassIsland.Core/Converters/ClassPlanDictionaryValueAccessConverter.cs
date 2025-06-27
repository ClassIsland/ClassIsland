using System.Globalization;
using Avalonia.Data.Converters;
using ClassIsland.Shared;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Core.Converters;

public class ClassPlanDictionaryValueAccessConverter : IValueConverter
{
    public ObservableOrderedDictionary<string, TimeLayout> SourceDictionary
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