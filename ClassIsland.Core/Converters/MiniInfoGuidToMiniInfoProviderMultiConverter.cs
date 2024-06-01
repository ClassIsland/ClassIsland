using System.Globalization;
using System.Windows.Data;
using ClassIsland.Shared;
using ClassIsland.Shared.Interfaces;

namespace ClassIsland.Core.Converters;

public class MiniInfoGuidToMiniInfoProviderMultiConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var guid = (string?)values[0];
        if (guid == null) return null;
        var providers = (ObservableDictionary<string, IMiniInfoProvider>)values[1];
        if (!providers.ContainsKey(guid)) return null;
        return providers[guid];
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[] { };
    }
}