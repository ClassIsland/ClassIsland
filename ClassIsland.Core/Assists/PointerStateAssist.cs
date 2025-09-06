using Avalonia;
using Avalonia.Controls;

namespace ClassIsland.Core.Assists;

/// <summary>
/// 光标状态辅助类
/// </summary>
public class PointerStateAssist
{
    public static readonly AttachedProperty<bool> IsTouchModeProperty =
        AvaloniaProperty.RegisterAttached<PointerStateAssist, Control, bool>("IsTouchMode", inherits: true);

    internal static void SetIsTouchMode(Control obj, bool value) => obj.SetValue(IsTouchModeProperty, value);
    public static bool GetIsTouchMode(Control obj) => obj.GetValue(IsTouchModeProperty);
}