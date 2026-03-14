using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Components;
using ClassIsland.Core.Models.UI;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using DynamicData;

namespace ClassIsland.Controls.EditMode;

public class EditableComponentsListBoxDropHandler : DropHandlerBase
{
    private static (int index, bool found) GetTargetIndex(ListBox listBox, DragEventArgs e, IList<ComponentSettings> items, ListBoxItem? explicitTarget)
    {
        var pos = e.GetPosition(listBox);
        if (listBox.GetVisualAt(pos) is Control targetControl
            && targetControl.FindAncestorOfType<ListBoxItem>() is {} listBoxItem
            && listBoxItem.DataContext is ComponentSettings targetItem)
        {
            var rPos = e.GetPosition(listBoxItem);
            // Console.WriteLine($"Pos {rPos.X} of width {listBoxItem.Bounds.Width}");
            var index = items.IndexOf(targetItem);
            if (index >= 0)
            {
                return (rPos.X <= listBoxItem.Bounds.Width / 2 ? index - 1 : index, true);
            }
        }

        return (items.Count > 0 ? items.Count - 1 : -1, items.Count > 0);
    }
    
    private bool ValidateCore(EditableComponentsListBox listBox, DragEventArgs e, object? sourceContext, object? targetContext, bool execute, ListBoxItem? listBoxItem)
    {
        e.Handled = true;
        if (sourceContext is ComponentInfo info)
        {
            return ValidateCoreComponentInfo(listBox, e, info, targetContext, execute, listBoxItem);
        }
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
        
        var (targetIndex, foundTargetIndex) = GetTargetIndex(listBox, e, targetList, listBoxItem);
        var insertIndex = foundTargetIndex ? targetIndex + 1 : targetList.Count;

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
                        var moveIndex = foundTargetIndex ? targetIndex : targetList.Count - 1;
                        var newIndex = sourceIndex > moveIndex ? moveIndex + 1 : moveIndex;
                        Console.WriteLine($"ti={targetIndex}, ni={newIndex}");
                        MoveItem(targetList, sourceIndex, Math.Clamp(newIndex, 0, targetList.Count - 1));
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
        
    private bool ValidateCoreComponentInfo(EditableComponentsListBox listBox, DragEventArgs e, ComponentInfo data, object? targetContext, bool execute, ListBoxItem? listBoxItem)
    {
        if (listBox.ItemsSource is not IList<ComponentSettings> targetList)
        {
            return false;
        }
        
        var (targetIndex, foundTargetIndex) = GetTargetIndex(listBox, e, targetList, listBoxItem);
        var insertIndex = foundTargetIndex ? targetIndex + 1 : targetList.Count;

        if (execute)
        {
            var componentSettings = new ComponentSettings()
            {
                Id = data.Guid.ToString()
            };
            ComponentsService.LoadComponentSettings(componentSettings,
                componentSettings.AssociatedComponentInfo.ComponentType!.BaseType!);
            InsertItem(targetList, componentSettings, insertIndex);
            if (data.IsComponentContainer)
            {
                IAppHost.GetService<ITutorialService>().BeginNotCompletedTutorials(
                    "classisland.getStarted.componentsEditing/containerComponent");
            }
        }
        return true;

    }
    
    public override bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (e.Handled)
        {
            return false;
        }
        return sender switch
        {
            EditableComponentsListBox listBox => ValidateCore(listBox, e, sourceContext, targetContext, false, null),
            // ListBoxItem listBoxItem when listBoxItem.FindAncestorOfType<EditableComponentsListBox>() is { } owner =>
            //     ValidateCore(owner, e, sourceContext, targetContext, false, listBoxItem),
            _ => false
        };
    }

    public override bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (e.Handled)
        {
            return false;
        }
        return sender switch
        {
            EditableComponentsListBox listBox => ValidateCore(listBox, e, sourceContext, targetContext, true, null),
            // ListBoxItem listBoxItem when listBoxItem.FindAncestorOfType<EditableComponentsListBox>() is { } owner =>
            //     ValidateCore(owner, e, sourceContext, targetContext, true, listBoxItem),
            _ => false
        };
    }
}