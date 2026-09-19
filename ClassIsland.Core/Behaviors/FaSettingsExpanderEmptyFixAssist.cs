using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Behaviors;

/// <summary>
/// 修复没有折叠项的 SettingsExpander 被作为卡片使用时，单击仍会展开极短空白的问题的助手类。
/// </summary>
public class FaSettingsExpanderEmptyFixAssist
{
    public static readonly AttachedProperty<bool> IsRepairAppliedProperty =
        AvaloniaProperty.RegisterAttached<FaSettingsExpanderEmptyFixAssist, SettingsExpander, bool>("IsRepairApplied");

    public static void SetIsRepairApplied(SettingsExpander obj, bool value) => obj.SetValue(IsRepairAppliedProperty, value);
    public static bool GetIsRepairApplied(SettingsExpander obj) => obj.GetValue(IsRepairAppliedProperty);

    static FaSettingsExpanderEmptyFixAssist()
    {
        IsRepairAppliedProperty.Changed.AddClassHandler<SettingsExpander>(IsRepairAppliedChanged);
    }

    private static void IsRepairAppliedChanged(SettingsExpander control, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is true)
        {
            control.AddHandler(Expander.ExpandingEvent, ExpanderOnExpanding, RoutingStrategies.Bubble);
        }
        else
        {
            control.RemoveHandler(Expander.ExpandingEvent, ExpanderOnExpanding);
        }
    }

    private static void ExpanderOnExpanding(object? sender, CancelRoutedEventArgs e)
    {
        if (sender is SettingsExpander { ItemCount: 0 })
        {
            e.Cancel = true;
        }
    }
}
