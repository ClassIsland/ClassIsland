using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Assists;

public class NativeMenuItemAssist
{
    public static readonly AttachedProperty<IconSource?> IconSourceProperty =
        AvaloniaProperty.RegisterAttached<NativeMenuItemAssist, NativeMenuItem, IconSource?>("IconSource");

    public static void SetIconSource(NativeMenuItem obj, IconSource? value) => obj.SetValue(IconSourceProperty, value);
    public static IconSource? GetIconSource(NativeMenuItem obj) => obj.GetValue(IconSourceProperty);

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

        var ise = new IconSourceElement
        {
            Classes = { "repair-fontsize" }
        };
        ise.Bind(IconSourceElement.IconSourceProperty, nativeMenuItem.GetObservable(IconSourceProperty));
        obj.Icon = ise;
    }
}