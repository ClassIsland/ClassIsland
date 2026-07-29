using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using ClassIsland.Models.Profile;
using ClassIsland.ViewModels;

namespace ClassIsland.Views;

public sealed class ClassPlanTreeDropHandler : DropHandlerBase
{
    public const string DataFormat = "ClassIsland.ClassPlanTreeNode";

    private const string DropBeforeClass = "class-plan-drop-before";
    private const string DropAfterClass = "class-plan-drop-after";
    private const string DropIntoGroupClass = "class-plan-drop-into-group";

    private Border? _highlightedTarget;

    public override bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext,
        object? state)
    {
        if (e.Handled)
        {
            ClearHighlight(sender as Border);
            return false;
        }

        var isValid = TryGetDropTarget(sender, e, sourceContext, targetContext, out var dropTarget)
                      && dropTarget.ViewModel.CanMoveClassPlan(
                          dropTarget.SourceNode.Guid,
                          dropTarget.TargetGroupId,
                          dropTarget.TargetClassPlanId,
                          dropTarget.InsertAfter);

        e.Handled = true;
        if (!isValid)
        {
            e.DragEffects = DragDropEffects.None;
            ClearHighlight(sender as Border);
            return false;
        }

        e.DragEffects = DragDropEffects.Move;
        SetHighlight(dropTarget.TargetBorder, dropTarget.HighlightClass);
        return true;
    }

    public override bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext,
        object? state)
    {
        if (e.Handled)
        {
            ClearHighlight();
            return false;
        }

        try
        {
            if (!TryGetDropTarget(sender, e, sourceContext, targetContext, out var dropTarget))
            {
                e.DragEffects = DragDropEffects.None;
                return false;
            }

            e.Handled = true;
            var moved = dropTarget.ViewModel.MoveClassPlan(
                dropTarget.SourceNode.Guid,
                dropTarget.TargetGroupId,
                dropTarget.TargetClassPlanId,
                dropTarget.InsertAfter);
            if (!moved)
            {
                e.DragEffects = DragDropEffects.None;
                return false;
            }

            dropTarget.TargetGroupContainer.IsExpanded = true;
            e.DragEffects = DragDropEffects.Move;
            return true;
        }
        finally
        {
            ClearHighlight();
        }
    }

    public override void Leave(object? sender, RoutedEventArgs e)
    {
        ClearHighlight(sender as Border);
        base.Leave(sender, e);
    }

    public override void Cancel(object? sender, RoutedEventArgs e)
    {
        ClearHighlight(sender as Border);
        base.Cancel(sender, e);
    }

    private static bool TryGetDropTarget(object? sender, DragEventArgs e, object? sourceContext,
        object? targetContext, out ClassPlanDropTarget dropTarget)
    {
        dropTarget = default;

        if (sender is not Border targetBorder
            || sourceContext is not ClassPlansTreeNode { IsGroup: false, ClassPlan: not null } sourceNode
            || targetContext is not ClassPlansTreeNode targetNode
            || targetBorder.FindAncestorOfType<ProfileSettingsWindow>() is not { } window
            || targetBorder.FindAncestorOfType<TreeViewItem>() is not { } targetContainer
            || targetContainer.FindAncestorOfType<TreeView>() is not { } treeView
            || treeView.Name != "ClassPlansTreeView"
            || window.ViewModel is not { } viewModel
            || viewModel.ManagementService.Policy.DisableProfileClassPlanEditing
            || e.DragEffects != DragDropEffects.Move)
        {
            return false;
        }

        var targetPosition = e.GetPosition(targetBorder);
        if (targetPosition.X < 0
            || targetPosition.Y < 0
            || targetPosition.X >= targetBorder.Bounds.Width
            || targetPosition.Y >= targetBorder.Bounds.Height
            || !IsInnermostTarget(treeView, targetBorder, e))
        {
            return false;
        }

        if (targetNode.IsGroup)
        {
            dropTarget = new ClassPlanDropTarget(
                viewModel,
                sourceNode,
                targetNode.Guid,
                null,
                false,
                targetContainer,
                targetBorder,
                DropIntoGroupClass);
            return true;
        }

        if (targetNode.ClassPlan is null || targetNode.Guid == sourceNode.Guid)
        {
            return false;
        }

        var insertAfter = targetPosition.Y >= targetBorder.Bounds.Height / 2;
        var targetGroupContainer = targetContainer.FindAncestorOfType<TreeViewItem>();
        if (targetGroupContainer?.DataContext is not ClassPlansTreeNode { IsGroup: true })
        {
            return false;
        }

        dropTarget = new ClassPlanDropTarget(
            viewModel,
            sourceNode,
            targetNode.ClassPlan.AssociatedGroup,
            targetNode.Guid,
            insertAfter,
            targetGroupContainer,
            targetBorder,
            insertAfter ? DropAfterClass : DropBeforeClass);
        return true;
    }

    private static bool IsInnermostTarget(TreeView treeView, Border targetBorder, DragEventArgs e)
    {
        var targetDepth = targetBorder.GetVisualAncestors().Count();
        return !treeView
            .GetVisualDescendants()
            .OfType<Border>()
            .Where(border => !ReferenceEquals(border, targetBorder)
                             && border.Classes.Contains("ClassPlanTreeNodeRoot")
                             && border.GetVisualAncestors().Count() > targetDepth)
            .Any(border =>
            {
                var position = e.GetPosition(border);
                return position.X >= 0
                       && position.Y >= 0
                       && position.X < border.Bounds.Width
                       && position.Y < border.Bounds.Height;
            });
    }

    private void SetHighlight(Border? target, string highlightClass)
    {
        if (ReferenceEquals(_highlightedTarget, target)
            && target?.Classes.Contains(highlightClass) == true)
        {
            return;
        }

        ClearHighlight();
        _highlightedTarget = target;
        _highlightedTarget?.Classes.Add(highlightClass);
    }

    private void ClearHighlight(Border? target = null)
    {
        if (_highlightedTarget is null
            || target is not null && !ReferenceEquals(_highlightedTarget, target))
        {
            return;
        }

        _highlightedTarget.Classes.Remove(DropBeforeClass);
        _highlightedTarget.Classes.Remove(DropAfterClass);
        _highlightedTarget.Classes.Remove(DropIntoGroupClass);
        _highlightedTarget = null;
    }

    private readonly record struct ClassPlanDropTarget(
        ProfileSettingsViewModel ViewModel,
        ClassPlansTreeNode SourceNode,
        Guid TargetGroupId,
        Guid? TargetClassPlanId,
        bool InsertAfter,
        TreeViewItem TargetGroupContainer,
        Border? TargetBorder,
        string HighlightClass);
}
