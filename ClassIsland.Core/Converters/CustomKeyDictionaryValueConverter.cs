using System.Globalization;
using Avalonia.Data.Converters;

namespace ClassIsland.Core.Converters;

/// <summary>
/// 自定义字典值访问转换器
/// </summary>
/// <typeparam name="TKey">字典键类型</typeparam>
/// <typeparam name="TValue">字典值类型</typeparam>
public class CustomKeyDictionaryValueConverter<TKey, TValue> : IMultiValueConverter where TKey : notnull
{
    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
        {
            return null;
        }
        
        if (values[0] is not IDictionary<TKey, TValue> dict || values[1] is not TKey key)
        {
            return null;
        }
        if (dict.TryGetValue(key, out var o))
        {
            return o;
        }

        return null;
    }
}