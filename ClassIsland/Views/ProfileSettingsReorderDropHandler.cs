using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactions.DragAndDrop;
using ClassIsland.Models.Profile;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels;

namespace ClassIsland.Views;

public class ProfileSettingsReorderDropHandler(ProfileSettingsViewModel viewModel) : DropHandlerBase
{
    public override bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (sender is not Control { IsEffectivelyEnabled: true }
            || viewModel.ManagementService.Policy.DisableProfileEditing
            || e.DragEffects != DragDropEffects.Move)
            return false;

        return (sourceContext, targetContext) switch
        {
            (ClassPlansTreeNode { IsGroup: false, ClassPlan: { } source } sourceNode,
                ClassPlansTreeNode { IsGroup: false, ClassPlan: { } target } targetNode) =>
                !viewModel.ManagementService.Policy.DisableProfileClassPlanEditing
                && sourceNode.Guid != targetNode.Guid
                && source.AssociatedGroup == target.AssociatedGroup
                && viewModel.ClassPlans.List.Contains(new KeyValuePair<Guid, ClassPlan>(sourceNode.Guid, source))
                && viewModel.ClassPlans.List.Contains(new KeyValuePair<Guid, ClassPlan>(targetNode.Guid, target)),
            (KeyValuePair<Guid, TimeLayout> source, KeyValuePair<Guid, TimeLayout> target) =>
                !viewModel.ManagementService.Policy.DisableProfileTimeLayoutEditing
                && source.Key != target.Key
                && viewModel.TimeLayouts.List.Contains(source)
                && viewModel.TimeLayouts.List.Contains(target),
            _ => false
        };
    }

    public override void Over(object? sender, DragEventArgs e, object? sourceContext, object? targetContext)
    {
        var valid = Validate(sender, e, sourceContext, targetContext, null);
        e.DragEffects = valid ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
        if (sender is not Control control)
            return;

        var before = e.GetPosition(control).Y < control.Bounds.Height / 2;
        control.Classes.Set("insert-before", valid && before);
        control.Classes.Set("insert-after", valid && !before);
    }

    public override bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (!Validate(sender, e, sourceContext, targetContext, state) || sender is not Control control)
            return false;

        var before = e.GetPosition(control).Y < control.Bounds.Height / 2;
        switch ((sourceContext, targetContext))
        {
            case (ClassPlansTreeNode source, ClassPlansTreeNode target):
            {
                var plans = viewModel.ClassPlans.List;
                var slots = plans.Select((pair, index) => (pair, index))
                    .Where(item => item.pair.Value.AssociatedGroup == source.ClassPlan!.AssociatedGroup)
                    .ToList();
                var group = slots.Select(item => item.pair).ToList();
                var sourceIndex = group.FindIndex(pair => pair.Key == source.Guid);
                var targetIndex = group.FindIndex(pair => pair.Key == target.Guid);
                var moved = group[sourceIndex];
                group.RemoveAt(sourceIndex);
                group.Insert(GetMoveIndex(sourceIndex, targetIndex, before), moved);

                // 保留其它课表群的位置，避免交错存储的课表在重载后改变群的显示顺序。
                var order = plans.ToArray();
                for (var index = 0; index < slots.Count; index++)
                    order[slots[index].index] = group[index];
                for (var index = 0; index < order.Length; index++)
                {
                    var currentIndex = plans.IndexOf(order[index]);
                    if (currentIndex != index)
                        plans.Move(currentIndex, index);
                }
                viewModel.SelectClassPlanByGuid(source.Guid);
                break;
            }
            case (KeyValuePair<Guid, TimeLayout> source, KeyValuePair<Guid, TimeLayout> target):
            {
                var layouts = viewModel.TimeLayouts.List;
                var sourceIndex = layouts.IndexOf(source);
                var targetIndex = layouts.IndexOf(target);
                layouts.Move(sourceIndex, GetMoveIndex(sourceIndex, targetIndex, before));
                viewModel.SelectedTimeLayout = source.Value;
                break;
            }
            default:
                return false;
        }

        viewModel.ProfileService.SaveProfile();
        return true;
    }

    private static int GetMoveIndex(int sourceIndex, int targetIndex, bool before)
    {
        var insertIndex = targetIndex + (before ? 0 : 1);
        return sourceIndex < insertIndex ? insertIndex - 1 : insertIndex;
    }

    public override void Cancel(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control control)
            return;
        control.Classes.Remove("insert-before");
        control.Classes.Remove("insert-after");
    }
}
