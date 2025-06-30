namespace ClassIsland.Core.Extensions;

/// <summary>
/// 提供将 int 转换为中文数字表示的扩展方法。
/// </summary>
public static class IntToChineseExtensions
{
    /// <summary>
    /// 将 int 转换为中文数字表示。
    /// 支持范围：0 到 10。
    /// </summary>
    public static string ToChinese(this int num) => num switch
    {
        0 => "零", 1 => "一", 2 => "二", 3 => "三", 4 => "四", 5 => "五",
        6 => "六", 7 => "七", 8 => "八", 9 => "九", 10 => "十", _ => num.ToString()
    };
}