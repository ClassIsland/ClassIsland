namespace ClassIsland.Platforms.Abstraction.Enums;

/// <summary>
/// 代表一个窗口拥有的平台特性。
/// </summary>
[Flags]
public enum WindowFeatures
{
    /// <summary>
    /// 无
    /// </summary>
    None = 0,
    /// <summary>
    /// 窗口可以穿透指针
    /// </summary>
    Transparent = 1,
    /// <summary>
    /// 窗口置于屏幕底层
    /// </summary>
    Bottommost = 2,
    /// <summary>
    /// 窗口置于屏幕顶层
    /// </summary>
    Topmost = 4,
    /// <summary>
    /// 隐私窗口，窗口内容只对屏幕可见，对截图工具等不可见
    /// </summary>
    Private = 8,
    /// <summary>
    /// 工具窗口
    /// </summary>
    ToolWindow = 16
}