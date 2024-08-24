namespace ClassIsland.Core.Extensions;

/// <summary>
/// <see cref="IDictionary{TKey,TValue}"/>的扩展方法。
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// 尝试获取指定的值。当值不存在时，设置并返回指定的默认值。
    /// </summary>
    /// <typeparam name="TKey">字典键的类型</typeparam>
    /// <typeparam name="TValue">字典值的类型</typeparam>
    /// <param name="dictionary">字典对象</param>
    /// <param name="key">要尝试获取的键</param>
    /// <param name="defaultValue">如果获取失败，使用的默认值</param>
    /// <returns>如果获取成功，返回获取的值，否则返回默认值。</returns>
    public static TValue GetOrCreateDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
        TKey key, TValue defaultValue)
    {
        if (dictionary.TryGetValue(key, out var value))
        {
            return value;
        }
        dictionary[key] = defaultValue;
        return defaultValue;
    }
}