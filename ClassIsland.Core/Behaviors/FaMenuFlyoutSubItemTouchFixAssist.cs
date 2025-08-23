using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Behaviors;

/// <summary>
/// 修复 FAMenuFlyoutSubItem 不支持触控的助手类。
/// </summary>
public class FaMenuFlyoutSubItemTouchFixAssist
{
    public static readonly AttachedProperty<bool> IsRepairAppliedProperty =
        AvaloniaProperty.RegisterAttached<FaMenuFlyoutSubItemTouchFixAssist, MenuFlyoutSubItem, bool>("IsRepairApplied");

    public static void SetIsRepairApplied(MenuFlyoutSubItem obj, bool value) => obj.SetValue(IsRepairAppliedProperty, value);
    public static bool GetIsRepairApplied(MenuFlyoutSubItem obj) => obj.GetValue(IsRepairAppliedProperty);

    private static readonly MethodInfo? MenuFlyoutSubItemOpenMethod = typeof(MenuFlyoutSubItem).GetMethod("Open", BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance);
    private static readonly MethodInfo? MenuFlyoutSubItemCloseMethod = typeof(MenuFlyoutSubItem).GetMethod("Close", BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance);

    static FaMenuFlyoutSubItemTouchFixAssist()
    {
        IsRepairAppliedProperty.Changed.AddClassHandler<MenuFlyoutSubItem>(IsRepairAppliedChanged);
    }

    private static void IsRepairAppliedChanged(MenuFlyoutSubItem control, AvaloniaPropertyChangedEventArgs args)
    {
        control.Loaded += ControlOnLoaded;
        
        return;
        
        void ControlOnLoaded(object? sender, RoutedEventArgs e)
        {
            control.Unloaded += ControlOnUnloaded;
            control.PointerReleased += ControlOnPointerReleased;
        }
        
        void ControlOnUnloaded(object? o, RoutedEventArgs routedEventArgs)
        {
            control.Loaded -= ControlOnLoaded;
            control.Unloaded -= ControlOnUnloaded;
            control.PointerReleased -= ControlOnPointerReleased;
        }

        void ControlOnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            var itemsControl = control.FindAncestorOfType<StackPanel>();
            foreach (var other in itemsControl?.Children.OfType<MenuFlyoutSubItem>()
                         .Where(x => x != control)?? [])
            {
                MenuFlyoutSubItemCloseMethod?.Invoke(other, [false]);
            }
            if (control.Focusable && control.IsEffectivelyEnabled)
            {
                MenuFlyoutSubItemOpenMethod?.Invoke(control, [false]);
            }
        }
    }
}