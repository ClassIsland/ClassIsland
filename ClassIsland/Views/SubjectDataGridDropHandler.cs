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

public sealed class SubjectDataGridDropHandler : BaseDataGridDropHandler<KeyValuePair<Guid, Subject>>
{
    public const string DataFormat = "ClassIsland.ProfileSubject";

    protected override KeyValuePair<Guid, Subject> MakeCopy(
        ObservableCollection<KeyValuePair<Guid, Subject>> parentCollection,
        KeyValuePair<Guid, Subject> item) =>
        throw new NotSupportedException("Subject sorting only supports move operations.");

    protected override bool Validate(DataGrid dg, DragEventArgs e, object? sourceContext, object? targetContext,
        bool execute)
    {
        if (e.DragEffects != DragDropEffects.Move
            || sourceContext is not KeyValuePair<Guid, Subject> sourceItem
            || targetContext is not ObservableCollection<KeyValuePair<Guid, Subject>> items
            || dg.GetVisualAt(e.GetPosition(dg)) is not Control targetControl)
        {
            return false;
        }

        var targetRow = targetControl as DataGridRow
            ?? targetControl.FindAncestorOfType<DataGridRow>()
            ?? targetControl.FindLogicalAncestorOfType<DataGridRow>();
        var targetItemContext = targetRow?.DataContext ?? targetControl.DataContext;
        if (targetItemContext is not KeyValuePair<Guid, Subject> targetItem
            || sourceItem.Key == targetItem.Key)
        {
            return false;
        }

        return RunDropAction(dg, e, execute, sourceItem, targetItem, items);
    }
}
