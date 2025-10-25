using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace ClassIsland.Core.Helpers.UI;

/// <summary>
/// <see cref="Flyout"/> 扩展操作静态类
/// </summary>
public static class FlyoutHelper
{
    /// <summary>
    /// 关闭父级上最近的 <see cref="Flyout"/>。
    /// </summary>
    /// <param name="control">目标控件</param>
    public static void CloseAncestorFlyout(object? o)
    {
        if (o is not Visual visual)
        {
            return;
        }
        
        var presenter = visual
            .GetVisualAncestors()
            .OfType<FlyoutPresenter>()
            .FirstOrDefault();
        if (presenter?.Parent is Popup flyout)
        {
            flyout.IsOpen = false;
        }
    }
}