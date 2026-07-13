namespace ClassIsland.Core.Helpers;

public static class TextDebugConfig
{
    // 默认值对应 SimpleRichText.cs 第 78-80 行的硬编码
    public static double Heit { get; set; } = 22;
    public static double MTop { get; set; } = 0;
    public static double MBot { get; set; } = 0;
    public static double PTop { get; set; } = 0;
    public static double PBot { get; set; } = 0;

    /// <summary>
    /// 值变化时触发，通知所有 SimpleRichText 实例重建
    /// </summary>
    public static event Action? ValuesChanged;

    public static void NotifyChanged() => ValuesChanged?.Invoke();
}