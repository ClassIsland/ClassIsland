using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Behaviors;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Views;

public sealed class ScheduleItemDataGridDropHandler : BaseDataGridDropHandler<KeyValuePair<Guid, ScheduleItem>>
{
    public const string DataFormat = "ClassIsland.ProfileScheduleItem";

    protected override KeyValuePair<Guid, ScheduleItem> MakeCopy(
        ObservableCollection<KeyValuePair<Guid, ScheduleItem>> parentCollection,
        KeyValuePair<Guid, ScheduleItem> item) =>
        throw new NotSupportedException("Schedule item sorting only supports move operations.");

    protected override bool Validate(DataGrid dg, DragEventArgs e, object? sourceContext, object? targetContext,
        bool execute)
    {
        var policy = dg.FindAncestorOfType<ProfileSettingsWindow>()?.ViewModel.ManagementService.Policy;
        if (policy == null
            || policy.DisableProfileEditing
            || policy.DisableProfileClassPlanEditing
            || policy.DisableProfileTimeLayoutEditing
            || policy.DisableProfileSubjectsEditing
            || e.DragEffects != DragDropEffects.Move
            || sourceContext is not KeyValuePair<Guid, ScheduleItem> sourceItem
            || targetContext is not ObservableCollection<KeyValuePair<Guid, ScheduleItem>> items
            || dg.GetVisualAt(e.GetPosition(dg)) is not Control targetControl)
        {
            return false;
        }

        var targetRow = targetControl as DataGridRow
            ?? targetControl.FindAncestorOfType<DataGridRow>()
            ?? targetControl.FindLogicalAncestorOfType<DataGridRow>();
        var targetItemContext = targetRow?.DataContext ?? targetControl.DataContext;
        if (targetItemContext is not KeyValuePair<Guid, ScheduleItem> targetItem
            || sourceItem.Key == targetItem.Key)
        {
            return false;
        }

        return RunDropAction(dg, e, execute, sourceItem, targetItem, items);
    }
}
