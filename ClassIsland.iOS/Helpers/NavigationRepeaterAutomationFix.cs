using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.iOS.Helpers;

internal static class NavigationRepeaterAutomationFix
{
    static NavigationRepeaterAutomationFix()
    {
        // Avalonia sets TemplatedParent before attaching the template to the visual tree.
        StyledElement.TemplatedParentProperty.Changed.AddClassHandler<FAItemsRepeater>((repeater, _) =>
        {
            if (repeater.TemplatedParent is FANavigationView or FANavigationViewItem)
            {
                GetAutomationPeer(repeater) ??= new NavigationRepeaterAutomationPeer(repeater);
            }
        });
    }

    internal static void Initialize()
    {
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_automationPeer")]
    private static extern ref AutomationPeer? GetAutomationPeer(Control control);

    private sealed class NavigationRepeaterAutomationPeer(FAItemsRepeater owner)
        : FAItemsRepeaterAutomationPeer(owner)
    {
        protected override IReadOnlyList<AutomationPeer> GetChildrenCore()
        {
            // FluentAvalonia 3.0.0 assumes Avalonia returns a list even when there are no visual children.
            return Owner.GetVisualChildren().Any() ? base.GetChildrenCore() : [];
        }
    }
}
