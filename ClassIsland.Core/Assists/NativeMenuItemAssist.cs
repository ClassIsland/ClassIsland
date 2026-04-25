using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Assists;

public class NativeMenuItemAssist
{
    public static readonly AttachedProperty<FAIconSource?> IconSourceProperty =
        AvaloniaProperty.RegisterAttached<NativeMenuItemAssist, NativeMenuItem, FAIconSource?>("FAIconSource");

    public static void SetIconSource(NativeMenuItem obj, FAIconSource? value) => obj.SetValue(IconSourceProperty, value);
    public static FAIconSource? GetIconSource(NativeMenuItem obj) => obj.GetValue(IconSourceProperty);

    public static readonly AttachedProperty<bool> OverrideIconProperty =
        AvaloniaProperty.RegisterAttached<NativeMenuItemAssist, MenuItem, bool>("OverrideIcon");

    public static void SetOverrideIcon(MenuItem obj, bool value) => obj.SetValue(OverrideIconProperty, value);

    public static bool GetOverrideIcon(MenuItem obj) => obj.GetValue(OverrideIconProperty);

    static NativeMenuItemAssist()
    {
        OverrideIconProperty.Changed.AddClassHandler<MenuItem>(OnOverrideIconChanged);
    }

    private static void OnOverrideIconChanged(MenuItem obj, AvaloniaPropertyChangedEventArgs arg2)
    {
        if (!GetOverrideIcon(obj) || obj.DataContext is not NativeMenuItem nativeMenuItem || nativeMenuItem.Icon != null)
        {
            return;
        }

        if (GetIconSource(nativeMenuItem) == null)
        {
            return;
        }

        var ise = new FAIconSourceElement
        {
            Classes = { "repair-fontsize" }
        };
        ise.Bind(FAIconSourceElement.IconSourceProperty, nativeMenuItem.GetObservable(IconSourceProperty));
        obj.Icon = ise;
    }
}