using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

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
    

    public static readonly AttachedProperty<bool> IsProgressAccuracyReducedProperty =
        AvaloniaProperty.RegisterAttached<MainWindowStylesAssist, Control, bool>("IsProgressAccuracyReduced", inherits:true);

    public static void SetIsProgressAccuracyReduced(Control obj, bool value) => obj.SetValue(IsProgressAccuracyReducedProperty, value);
    public static bool GetIsProgressAccuracyReduced(Control obj) => obj.GetValue(IsProgressAccuracyReducedProperty);

    public static readonly AttachedProperty<Color> BackgroundCorlorProperty =
        AvaloniaProperty.RegisterAttached<MainWindowStylesAssist, Control, Color>("BackgroundColor", inherits:true);

    public static void SetBackgroundCorlor(Control obj, Color value) => obj.SetValue(BackgroundCorlorProperty, value);
    public static Color GetBackgroundColor(Control obj) => obj.GetValue(BackgroundCorlorProperty);

    public static readonly AttachedProperty<double> BackgroundOpacityProperty =
        AvaloniaProperty.RegisterAttached<MainWindowStylesAssist, Control, double>("BackgroundOpacity", 0.5, inherits: true);

    public static void SetBackgroundOpacity(Control obj, double value) => obj.SetValue(BackgroundOpacityProperty, value);
    public static double GetBackgroundOpacity(Control obj) => obj.GetValue(BackgroundOpacityProperty);

    public static readonly AttachedProperty<bool> IsCustomBackgroundColorEnabledProperty =
        AvaloniaProperty.RegisterAttached<MainWindowStylesAssist, Control, bool>("IsCustomBackgroundColorEnabled", inherits: true);

    public static void SetIsCustomBackgroundColorEnabled(Control obj, bool value) => obj.SetValue(IsCustomBackgroundColorEnabledProperty, value);
    public static bool GetIsCustomBackgroundColorEnabled(Control obj) => obj.GetValue(IsCustomBackgroundColorEnabledProperty);
}