using System.Collections;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace ClassIsland.Core.Converters;

/// <summary>
/// 自定义字典值访问转换器
/// </summary>
public class CustomKeyDictionaryValueConverter : AvaloniaObject
{
    private IDictionary? _dictionary;
    
    public static readonly DirectProperty<CustomKeyDictionaryValueConverter, IDictionary?>
        DictionaryProperty =
            AvaloniaProperty.RegisterDirect<CustomKeyDictionaryValueConverter, IDictionary?>(
                nameof(Dictionary), o => o.Dictionary, (o, v) => o.Dictionary = v);
    
    public IDictionary? Dictionary
    {
        get => _dictionary;
        set => SetAndRaise(DictionaryProperty, ref _dictionary, value);
    }
    
    internal CustomKeyDictionaryValueConverter() {}
}

/// <summary>
/// 自定义字典值访问转换器
/// </summary>
/// <typeparam name="TKey">字典键类型</typeparam>
/// <typeparam name="TValue">字典值类型</typeparam>
public class CustomKeyDictionaryValueConverter<TKey, TValue> : CustomKeyDictionaryValueConverter, IMultiValueConverter, IValueConverter where TKey : notnull
{
    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
        {
            return null;
        }

        if (values[1] is not TKey key)
        {
            return null;
        }
        if (Dictionary is IDictionary<TKey, TValue> dictionary)
        {
            dictionary.TryGetValue(key, out var o1);
            return o1;
        }
        if (values[0] is not IDictionary<TKey, TValue> dict)
        {
            return null;
        }
        if (dict.TryGetValue(key, out var o))
        {
            return o;
        }

        return null;
    }
    
    object? IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TKey key)
        {
            return null;
        }

        if (Dictionary is not IDictionary<TKey, TValue> dictionary) 
            return null;
        dictionary.TryGetValue(key, out var o1);
        return o1;

    }

    object? IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}