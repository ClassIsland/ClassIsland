using System.Globalization;
using System.Windows.Data;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Core.Converters;

public class SubjectsDictionaryValueAccessConverter : IValueConverter
{
    public ObservableDictionary<string, Subject> SourceDictionary
    {
        get;
        set;
    } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var k = (string)value;
        try
        {
            return SourceDictionary[k].Name;
        }
        catch
        {
            return k;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}