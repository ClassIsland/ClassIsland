using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Assists;

/// <summary>
/// 修复 FluentAvalonia 空列表的无障碍子节点读取。
/// </summary>
public class FaItemsRepeaterAutomationAssist
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<FaItemsRepeaterAutomationAssist, FAItemsRepeater, bool>("IsEnabled");

    public static bool GetIsEnabled(FAItemsRepeater repeater) => repeater.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(FAItemsRepeater repeater, bool value) => repeater.SetValue(IsEnabledProperty, value);

    static FaItemsRepeaterAutomationAssist()
    {
        IsEnabledProperty.Changed.AddClassHandler<FAItemsRepeater>((repeater, args) =>
        {
            ref var peer = ref GetAutomationPeer(repeater);
            if (args.GetNewValue<bool>())
            {
                // 在平台首次查询前缓存修复后的 peer，也覆盖第三方控件模板内的 repeater。
                if (peer == null || peer.GetType() == typeof(FAItemsRepeaterAutomationPeer))
                {
                    peer = new EmptySafeItemsRepeaterAutomationPeer(repeater);
                }
            }
            else if (peer is EmptySafeItemsRepeaterAutomationPeer)
            {
                peer = null;
            }
        });
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_automationPeer")]
    private static extern ref AutomationPeer? GetAutomationPeer(Control control);

    private sealed class EmptySafeItemsRepeaterAutomationPeer(FAItemsRepeater owner)
        : FAItemsRepeaterAutomationPeer(owner)
    {
        protected override IReadOnlyList<AutomationPeer> GetChildrenCore()
        {
            // Avalonia 12 允许无 visual children 时返回 null；FA 3.0.0 随后直接读取 Count。
            // 保留非空列表的上游虚拟化和无障碍行为。
            return Owner.GetVisualChildren().Any() ? base.GetChildrenCore() : [];
        }
    }
}
