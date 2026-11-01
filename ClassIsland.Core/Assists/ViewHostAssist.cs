using Avalonia;
using ClassIsland.Core.Abstractions.Controls;

namespace ClassIsland.Core.Assists;

/// <summary>
/// 页面在视图宿主抽屉中的显示配置。
/// </summary>
public class ViewHostAssist
{
    /// <summary>
    /// 抽屉的最大宽度，默认不限制。
    /// </summary>
    public static readonly AttachedProperty<double> DrawerMaxWidthProperty =
        AvaloniaProperty.RegisterAttached<ViewHostAssist, ViewBase, double>("DrawerMaxWidth", double.PositiveInfinity,
            validate: value => value >= 0);

    /// <inheritdoc cref="DrawerMaxWidthProperty"/>
    public static double GetDrawerMaxWidth(ViewBase view) => view.GetValue(DrawerMaxWidthProperty);

    /// <inheritdoc cref="DrawerMaxWidthProperty"/>
    public static void SetDrawerMaxWidth(ViewBase view, double value) => view.SetValue(DrawerMaxWidthProperty, value);

    /// <summary>
    /// 抽屉展开高度占宿主高度的比例。
    /// </summary>
    public static readonly AttachedProperty<double> DrawerContentHeightRatioProperty =
        AvaloniaProperty.RegisterAttached<ViewHostAssist, ViewBase, double>("DrawerContentHeightRatio", 0.5);

    /// <inheritdoc cref="DrawerContentHeightRatioProperty"/>
    public static double GetDrawerContentHeightRatio(ViewBase view) => view.GetValue(DrawerContentHeightRatioProperty);

    /// <inheritdoc cref="DrawerContentHeightRatioProperty"/>
    public static void SetDrawerContentHeightRatio(ViewBase view, double value) => view.SetValue(DrawerContentHeightRatioProperty, value);

    /// <summary>
    /// 抽屉展开后的绝对高度，设置有效高度后优先于高度比例。
    /// </summary>
    public static readonly AttachedProperty<double?> DrawerContentHeightProperty =
        AvaloniaProperty.RegisterAttached<ViewHostAssist, ViewBase, double?>("DrawerContentHeight");

    /// <inheritdoc cref="DrawerContentHeightProperty"/>
    public static double? GetDrawerContentHeight(ViewBase view) => view.GetValue(DrawerContentHeightProperty);

    /// <inheritdoc cref="DrawerContentHeightProperty"/>
    public static void SetDrawerContentHeight(ViewBase view, double? value) => view.SetValue(DrawerContentHeightProperty, value);

    /// <summary>
    /// 是否允许收起抽屉。
    /// </summary>
    public static readonly AttachedProperty<bool> DrawerAllowCollapseProperty =
        AvaloniaProperty.RegisterAttached<ViewHostAssist, ViewBase, bool>("DrawerAllowCollapse");

    /// <inheritdoc cref="DrawerAllowCollapseProperty"/>
    public static bool GetDrawerAllowCollapse(ViewBase view) => view.GetValue(DrawerAllowCollapseProperty);

    /// <inheritdoc cref="DrawerAllowCollapseProperty"/>
    public static void SetDrawerAllowCollapse(ViewBase view, bool value) => view.SetValue(DrawerAllowCollapseProperty, value);

    /// <summary>
    /// 是否显示抽屉遮罩。
    /// </summary>
    public static readonly AttachedProperty<bool> DrawerSmokeBackgroundProperty =
        AvaloniaProperty.RegisterAttached<ViewHostAssist, ViewBase, bool>("DrawerSmokeBackground", true);

    /// <inheritdoc cref="DrawerSmokeBackgroundProperty"/>
    public static bool GetDrawerSmokeBackground(ViewBase view) => view.GetValue(DrawerSmokeBackgroundProperty);

    /// <inheritdoc cref="DrawerSmokeBackgroundProperty"/>
    public static void SetDrawerSmokeBackground(ViewBase view, bool value) => view.SetValue(DrawerSmokeBackgroundProperty, value);

    /// <summary>
    /// 是否将抽屉高度展开到整个可用区域，宽度保持不变。
    /// </summary>
    public static readonly AttachedProperty<bool> DrawerIsFullScreenProperty =
        AvaloniaProperty.RegisterAttached<ViewHostAssist, ViewBase, bool>("DrawerIsFullScreen");

    /// <inheritdoc cref="DrawerIsFullScreenProperty"/>
    public static bool GetDrawerIsFullScreen(ViewBase view) => view.GetValue(DrawerIsFullScreenProperty);

    /// <inheritdoc cref="DrawerIsFullScreenProperty"/>
    public static void SetDrawerIsFullScreen(ViewBase view, bool value) => view.SetValue(DrawerIsFullScreenProperty, value);
}
