using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using ClassIsland.Controls.EditMode;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Components;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using FluentAvalonia.Core;
using Visual = Avalonia.Visual;

namespace ClassIsland.Views.SettingPages;

public class ComponentsSettingsPageDropHandler(ComponentsSettingsViewModel viewModel) : DropHandlerBase, IDragHandler
{
    public ComponentsSettingsViewModel ViewModel { get; } = viewModel;

    private ObservableCollection<ComponentSettings>? _sourceCollection;

    private ListBox? _sourceListBox;

    private ListBoxItem? _sourceListBoxItem;
    
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

        var half = pos.X > listBox.Bounds.Width / 2;
        return (items.Count > 0 ? (half ? items.Count - 1 : -1) : -1, items.Count > 0);
    }
    
    public override void Enter(object? sender, DragEventArgs e, object? sourceContext, object? targetContext)
    {
        e.DragEffects = sourceContext switch
        {
            ComponentInfo => DragDropEffects.Copy,
            ComponentSettings => DragDropEffects.Move,
            _ => DragDropEffects.None
        };
    }
    

    public override void Drop(object? sender, DragEventArgs e, object? sourceContext, object? targetContext)
    {
        if (targetContext is not ObservableCollection<ComponentSettings> components ||
            sender is not ListBox listBox)
        {
            return;
        }
        var (targetIndex, foundTargetIndex) = GetTargetIndex(listBox, e, components, null);
        var insertIndex = foundTargetIndex ? targetIndex + 1 : components.Count;
        switch (sourceContext)
        {
            case ComponentInfo info:
                var componentSettings = new ComponentSettings()
                {
                    Id = info.Guid.ToString()
                };
                InsertItem(components, componentSettings, insertIndex);
                ComponentsService.LoadComponentSettings(componentSettings,
                    componentSettings.AssociatedComponentInfo.ComponentType!.BaseType!);
                break;
            case EditableComponentsListBoxDragData { ComponentSettings: {} settings, SourceList: {} sourceList }:
                var oldIndex = components.IndexOf(settings);
                if (settings == ViewModel.SelectedRootComponent && components == ViewModel.SelectedComponentContainerChildren)
                {
                    return;
                }
                var sourceIndex = sourceList.IndexOf(settings);
                if (sourceIndex < 0)
                {
                    return ;
                }
                    
                if (ReferenceEquals(sourceList, components))
                {
                    var moveIndex = foundTargetIndex ? targetIndex : components.Count - 1;
                    var newIndex = sourceIndex > moveIndex ? moveIndex + 1 : moveIndex;
                    MoveItem(components, sourceIndex, Math.Clamp(newIndex, 0, components.Count - 1));
                }
                else
                {
                    MoveItem(sourceList, components, sourceIndex, insertIndex);
                }
                break;
        }

    }
    
    public override bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (sourceContext is not ComponentInfo && sourceContext is not ComponentSettings)
            return false;
        if (sourceContext is ComponentSettings settings1 && targetContext is ObservableCollection<ComponentSettings> components
                                                        && settings1 == ViewModel.SelectedRootComponent 
                                                        && Equals(components, ViewModel.SelectedComponentContainerChildren))
        {
            return false;
        }
        return sourceContext is not ComponentSettings settings || settings != ViewModel.SelectedRootComponent
                                                               || !Equals(targetContext, ViewModel.SelectedComponentContainerChildren);
    }

    public void BeforeDragDrop(object? sender, PointerEventArgs e, object? context)
    {
        if (sender is not ListBoxItem item)
        {
            return;
        }

        _sourceListBoxItem = item;
        var listBox = _sourceListBox = item.FindAncestorOfType<ListBox>();
        if (listBox?.ItemsSource is ObservableCollection<ComponentSettings> collection)
        {
            _sourceCollection = collection;
        }
    }

    public void AfterDragDrop(object? sender, PointerEventArgs e, object? context)
    {
        ClearTransform(_sourceListBoxItem);
        foreach (var control in _sourceListBox?.Items
                     .OfType<object>()
                     .Select(x => _sourceListBox.ContainerFromItem(x)) ?? [])
        {
            ClearTransform(control);
        }

        _sourceCollection = null;
        _sourceListBoxItem = null;
        _sourceListBox = null;
    }

    private void ClearTransform(Control? control)
    {
        control?.SetValue(Visual.RenderTransformProperty, new TransformGroup());
    }
}