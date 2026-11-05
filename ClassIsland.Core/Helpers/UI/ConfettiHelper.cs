using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Controls;

namespace ClassIsland.Core.Helpers.UI;

/// <summary>
/// 彩带动画显示辅助类。
/// </summary>
public static class ConfettiHelper
{
    /// <summary>
    /// 在动画等级至少为 1 时，在控件所属的窗口内播放彩带动画。
    /// </summary>
    /// <param name="control">包含在已启用 MyWindow 扩展的窗口中的控件。</param>
    public static void ShowConfetti(this Control control)
    {
        control.RaiseEvent(new RoutedEventArgs(ConfettiAdorner.ShowConfettiEvent));
    }
}
