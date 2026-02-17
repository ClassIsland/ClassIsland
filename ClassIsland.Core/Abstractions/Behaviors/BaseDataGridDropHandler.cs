// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;

namespace ClassIsland.Core.Abstractions.Behaviors;

/// <summary>
/// Provides common drag-and-drop logic for <see cref="DataGrid"/> row manipulations.
/// </summary>
/// <typeparam name="T">The item type contained by the target <see cref="DataGrid"/>.</typeparam>
public abstract class BaseDataGridDropHandler<T> : DropHandlerBase
    where T : class
{
    private const string RowDraggingUpStyleClass = "DraggingUp";
    private const string RowDraggingDownStyleClass = "DraggingDown";

    /// <summary>
    /// Creates a copy of the source item for copy operations.
    /// </summary>
    /// <param name="parentCollection">The collection owning the original item.</param>
    /// <param name="item">The item to clone.</param>
    protected abstract T MakeCopy(ObservableCollection<T> parentCollection, T item);

    /// <summary>
    /// Validates a pending drag operation and optionally executes it.
    /// </summary>
    /// <param name="dg">The target data grid.</param>
    /// <param name="e">The drag event data.</param>
    /// <param name="sourceContext">The source context.</param>
    /// <param name="targetContext">The target context.</param>
    /// <param name="execute">When true, the handler should execute the drop logic.</param>
    protected abstract bool Validate(DataGrid dg, DragEventArgs e, object? sourceContext, object? targetContext, bool execute);

    /// <inheritdoc />
    public override bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (e.Source is Control c && sender is DataGrid dg)
        {
            var valid = Validate(dg, e, sourceContext, targetContext, false);
            if (valid)
            {
                var row = FindDataGridRowFromChildView(c);
                if (row is not null)
                {
                    var isDirectionUp = e.GetPosition(row).Y < row.Bounds.Height / 2;
                    ApplyDraggingStyleToRow(row, isDirectionUp);
                }
                ClearDraggingStyleFromAllRows(sender, exceptThis: row);
            }
            return valid;
        }
        ClearDraggingStyleFromAllRows(sender);
        return false;
    }

    /// <inheritdoc />
    public override bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        ClearDraggingStyleFromAllRows(sender);
        if (e.Source is Control && sender is DataGrid dg)
        {
            return Validate(dg, e, sourceContext, targetContext, true);
        }
        return false;
    }

    /// <inheritdoc />
    public override void Cancel(object? sender, RoutedEventArgs e)
    {
        base.Cancel(sender, e);
        // Clear adorner borders when the pointer leaves the DataGrid.
        ClearDraggingStyleFromAllRows(sender);
    }

    /// <summary>
    /// Executes a drop action against the provided <paramref name="items"/> collection.
    /// </summary>
    /// <param name="dg">The owning data grid.</param>
    /// <param name="e">The drag event arguments.</param>
    /// <param name="execute">True to perform the operation; false to validate only.</param>
    /// <param name="sourceItem">The item being dragged.</param>
    /// <param name="targetItem">The item currently under the pointer.</param>
    /// <param name="items">The backing collection.</param>
    /// <returns><c>true</c> when the operation is valid; otherwise <c>false</c>.</returns>
    protected bool RunDropAction(DataGrid dg, DragEventArgs e, bool execute, T sourceItem, T targetItem, ObservableCollection<T> items)
    {
        var sourceIndex = items.IndexOf(sourceItem);
        var targetIndex = items.IndexOf(targetItem);

        if (sourceIndex < 0 || targetIndex < 0)
        {
            return false;
        }

        var insertIndex = targetIndex;

        if (e.Source is Control c)
        {
            var row = FindDataGridRowFromChildView(c);
            if (row is not null && e.GetPosition(row).Y > row.Bounds.Height / 2)
            {
                insertIndex = targetIndex + 1;
            }
        }

        var adjustedTargetIndex = insertIndex;
        if (adjustedTargetIndex > sourceIndex)
        {
            adjustedTargetIndex--;
        }

        switch (e.DragEffects)
        {
            case DragDropEffects.Copy:
                {
                    if (execute)
                    {
                        var clone = MakeCopy(items, sourceItem);
                        InsertItem(items, clone, insertIndex);
                        dg.SelectedIndex = insertIndex;
                    }
                    return true;
                }
            case DragDropEffects.Move:
                {
                    if (execute)
                    {
                        MoveItem(items, sourceIndex, adjustedTargetIndex);
                        dg.SelectedIndex = adjustedTargetIndex;
                    }
                    return true;
                }
            case DragDropEffects.Link:
                {
                    if (execute)
                    {
                        SwapItem(items, sourceIndex, adjustedTargetIndex);
                        dg.SelectedIndex = adjustedTargetIndex;
                    }
                    return true;
                }
            default:
                return false;
        }
    }

    private static DataGridRow? FindDataGridRowFromChildView(StyledElement sourceChild)
    {
        return sourceChild.FindLogicalAncestorOfType<DataGridRow>();
    }

    private static DataGridRowsPresenter? GetRowsPresenter(Visual v)
    {
        foreach (var cv in v.GetVisualChildren())
        {
            if (cv is DataGridRowsPresenter dgrp)
                return dgrp;
            else if (GetRowsPresenter(cv) is DataGridRowsPresenter dgrp2)
                return dgrp2;
        }
        return null;
    }

    /// <summary>
    /// Removes drag styling from all materialized rows except the provided instance.
    /// </summary>
    private static void ClearDraggingStyleFromAllRows(object? sender, DataGridRow? exceptThis = null)
    {
        if (sender is DataGrid dg)
        {
            var presenter = GetRowsPresenter(dg);
            if (presenter is null)
                return;

            foreach (var r in presenter.Children)
            {
                if (r == exceptThis)
                    continue;

                r?.Classes?.Remove(RowDraggingUpStyleClass);
                r?.Classes?.Remove(RowDraggingDownStyleClass);
            }
        }
    }

    /// <summary>
    /// Applies the drag styling for the given direction to the specified row.
    /// </summary>
    private static void ApplyDraggingStyleToRow(DataGridRow row, bool isDirectionUp)
    {
        if (isDirectionUp)
        {
            row.Classes.Remove(RowDraggingDownStyleClass);
            row.Classes.Add(RowDraggingUpStyleClass);
        }
        else
        {
            row.Classes.Remove(RowDraggingUpStyleClass);
            row.Classes.Add(RowDraggingDownStyleClass);
        }
    }
}
