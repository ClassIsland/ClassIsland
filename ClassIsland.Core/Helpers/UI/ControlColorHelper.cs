using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace ClassIsland.Core.Helpers.UI;

/// <summary>
/// 控件颜色相关的辅助方法类
/// </summary>
public static class ControlColorHelper
{
    /// <summary>
    /// 设置一个控件的前景色，并重新计算各个类别的前景色笔刷。
    /// </summary>
    /// <param name="control">目标控件</param>
    /// <param name="color">前景颜色</param>
    /// <param name="enabled">是否启用前景色设置</param>
    public static void SetControlForegroundColor(Control control, Color color, bool enabled)
    {
        if (enabled)
        {
            var brush = new SolidColorBrush(new Color(255, color.R,
                color.G, color.B));
            var brush1 = new SolidColorBrush(new Color(218, color.R,
                color.G, color.B));
            var brushSecondary = new SolidColorBrush(new Color(197, color.R,
                color.G, color.B));
            var brushTertiary = new SolidColorBrush(new Color(135, color.R,
                color.G, color.B));
            var brushDisabled = new SolidColorBrush(new Color(93, color.R,
                color.G, color.B));
            control.SetValue(TemplatedControl.ForegroundProperty, brush);
            control.SetValue(TextElement.ForegroundProperty, brush);
            control.Resources["TextFillColorPrimaryBrush"] = brush;
            control.Resources["TextFillColorInformativeSecondaryBrush"] = brush1;
            control.Resources["TextFillColorSecondaryBrush"] = brushSecondary;
            control.Resources["TextFillColorTertiaryBrush"] = brushTertiary;
            control.Resources["TextFillColorDisabledBrush"] = brushDisabled;
        }
        else
        {
            control.Resources.Remove("TextFillColorPrimaryBrush");
            control.Resources.Remove("TextFillColorInformativeSecondaryBrush");
            control.Resources.Remove("TextFillColorSecondaryBrush");
            control.Resources.Remove("TextFillColorTertiaryBrush");
            control.Resources.Remove("TextFillColorDisabledBrush");
            control.SetValue(TemplatedControl.ForegroundProperty, AvaloniaProperty.UnsetValue);
            control.SetValue(TextElement.ForegroundProperty, AvaloniaProperty.UnsetValue);
        }
    }
}