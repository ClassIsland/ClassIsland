namespace ClassIsland.Core.Enums.UI;

/// <summary>
/// 图标位图的渲染模式。
/// </summary>
public enum IconBitmapRenderMode
{
    /// <summary>
    /// 在圆角白色背景上渲染亮色主题的彩色图标。
    /// </summary>
    Color,

    /// <summary>
    /// 渲染透明背景的单色图标；默认输出灰度图，可指定自定义颜色。
    /// </summary>
    Monochrome
}
