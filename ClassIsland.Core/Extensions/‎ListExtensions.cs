namespace ClassIsland.Core.Extensions;

/// <summary>
/// 为 <see cref="List{T}"/> 提供扩展方法。
/// </summary>
public static class ListExtensions
{
    /// <summary>
    /// 安全获取列表中指定索引处的元素，若索引越界或列表为null则返回默认值。
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <param name="source">目标集合</param>
    /// <param name="index">要获取的索引位置</param>
    /// <param name="defaultValue">索引越界时返回的默认值（可选）</param>
    /// <returns>索引处的元素或默认值</returns>
    /// <example>
    /// var list = new List&lt;int&gt; { 1, 2, 3 };
    /// var value = list.GetValueOrDefault(5, -1); // 返回 -1
    /// </example>
    public static T? GetValueOrDefault<T>(this IReadOnlyList<T>? source, int index, T? defaultValue = default)
    {
        if (source == null) return defaultValue;
        return index >= 0 && index < source.Count
            ? source[index]
            : defaultValue;
    }
}
