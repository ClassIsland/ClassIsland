using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Components;
using ClassIsland.Core.Models.UI;
using ClassIsland.Shared.Helpers;
using DynamicData;

namespace ClassIsland.Controls.EditMode;

public class EditableComponentsListBoxDropHandler : DropHandlerBase
{
    private static int GetTargetIndex(ListBox listBox, DragEventArgs e, IList<ComponentSettings> items, ListBoxItem? explicitTarget)
    {
        var topLevel = TopLevel.GetTopLevel(explicitTarget);

        if (explicitTarget?.DataContext is ComponentSettings targetFromContainer)
        {
            var containerIndex = items.IndexOf(targetFromContainer);
            if (containerIndex >= 0)
            {
                return containerIndex;
            }
        }

        if (listBox.GetVisualAt(e.GetPosition(listBox)) is Control targetControl
            && targetControl.DataContext is ComponentSettings targetItem)
        {
            var index = items.IndexOf(targetItem);
            if (index >= 0)
            {
                return index;
            }
        }

        return items.Count > 0 ? items.Count - 1 : -1;
    }
    
    private bool ValidateCore(EditableComponentsListBox listBox, DragEventArgs e, object? sourceContext, object? targetContext, bool execute, ListBoxItem? listBoxItem)
    {
        if (sourceContext is not EditableComponentsListBoxDragData data || listBox.ItemsSource is not IList<ComponentSettings> targetList)
        {
            return false;
        }
        
        if (data.ComponentSettings == null && data.ComponentInfo == null)
            return false;
        if (data.ComponentSettings is {} settings1 && listBox.ContainerComponentStack.Contains(settings1))
        {
            return false;
        }
        
        var targetIndex = GetTargetIndex(listBox, e, targetList, listBoxItem);
        var insertIndex = targetIndex >= 0 ? targetIndex + 1 : targetList.Count;

        switch (e.DragEffects)
        {
            case DragDropEffects.Copy when data.ComponentSettings is {} settings:
            {
                if (execute)
                {
                    var clone = ConfigureFileHelper.CopyObject(settings);
                    InsertItem(targetList, clone, insertIndex);
                }
                return true;
            }
            case DragDropEffects.Move when data is { ComponentSettings: {} settings, SourceList: { } sourceItems }:
            {
                if (execute)
                {
                    var sourceIndex = sourceItems.IndexOf(settings);
                    if (sourceIndex < 0)
                    {
                        return false;
                    }
                    
                    if (ReferenceEquals(sourceItems, targetList))
                    {
                        var moveIndex = targetIndex >= 0 ? targetIndex : targetList.Count - 1;
                        if (moveIndex < 0)
                        {
                            moveIndex = 0;
                        }

                        MoveItem(targetList, sourceIndex, moveIndex);
                    }
                    else
                    {
                        MoveItem(sourceItems, targetList, sourceIndex, insertIndex);
                    }
                }
                return true;
            }
            case DragDropEffects.None:
            case DragDropEffects.Link:
            default:
                return false;
        }

        return false;
    }
        
    public override bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        return sender switch
        {
            EditableComponentsListBox listBox => ValidateCore(listBox, e, sourceContext, targetContext, false, null),
            ListBoxItem listBoxItem when listBoxItem.FindAncestorOfType<EditableComponentsListBox>() is { } owner =>
                ValidateCore(owner, e, sourceContext, targetContext, false, listBoxItem),
            _ => false
        };
    }

    public override bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        return sender switch
        {
            EditableComponentsListBox listBox => ValidateCore(listBox, e, sourceContext, targetContext, true, null),
            ListBoxItem listBoxItem when listBoxItem.FindAncestorOfType<EditableComponentsListBox>() is { } owner =>
                ValidateCore(owner, e, sourceContext, targetContext, true, listBoxItem),
            _ => false
        };
    }
}