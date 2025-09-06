using Avalonia;
using Avalonia.Controls;

namespace ClassIsland.Core.Assists;

/// <summary>
/// 主界面样式助手类，用于传递当前上下文的主界面样式信息。
/// </summary>
public class MainWindowStylesAssist
{
    public static readonly AttachedProperty<bool> IsIslandSeperatedProperty =
        AvaloniaProperty.RegisterAttached<MainWindowStylesAssist, Control, bool>("IsIslandSeperated", inherits:true);

    public static void SetIsIslandSeperated(Control obj, bool value) => obj.SetValue(IsIslandSeperatedProperty, value);
    public static bool GetIsIslandSeperated(Control obj) => obj.GetValue(IsIslandSeperatedProperty);

    public static readonly AttachedProperty<double> CornerRadiusProperty =
        AvaloniaProperty.RegisterAttached<MainWindowStylesAssist, Control, double>("CornerRadius", inherits:true);

    public static void SetCornerRadius(Control obj, double value) => obj.SetValue(CornerRadiusProperty, value);
    public static double GetCornerRadius(Control obj) => obj.GetValue(CornerRadiusProperty);

    public static readonly AttachedProperty<double> IslandSpacingProperty =
        AvaloniaProperty.RegisterAttached<MainWindowStylesAssist, Control, double>("IslandSpacing", inherits:true);

    public static void SetIslandSpacing(Control obj, double value) => obj.SetValue(IslandSpacingProperty, value);
    public static double GetIslandSpacing(Control obj) => obj.GetValue(IslandSpacingProperty);

    public static readonly AttachedProperty<double> ComponentsSpacingProperty =
        AvaloniaProperty.RegisterAttached<MainWindowStylesAssist, Control, double>("ComponentsSpacing", inherits:true);

    public static void SetComponentsSpacing(Control obj, double value) => obj.SetValue(ComponentsSpacingProperty, value);
    public static double GetComponentsSpacing(Control obj) => obj.GetValue(ComponentsSpacingProperty);

    public static readonly AttachedProperty<bool> IsProgressAccuracyReducedProperty =
        AvaloniaProperty.RegisterAttached<MainWindowStylesAssist, Control, bool>("IsProgressAccuracyReduced", inherits:true);

    public static void SetIsProgressAccuracyReduced(Control obj, bool value) => obj.SetValue(IsProgressAccuracyReducedProperty, value);
    public static bool GetIsProgressAccuracyReduced(Control obj) => obj.GetValue(IsProgressAccuracyReducedProperty);
}