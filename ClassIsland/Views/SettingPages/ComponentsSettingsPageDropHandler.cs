using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Components;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using FluentAvalonia.Core;

namespace ClassIsland.Views.SettingPages;

public class ComponentsSettingsPageDropHandler(ComponentsSettingsViewModel viewModel) : DropHandlerBase, IDragHandler
{
    public ComponentsSettingsViewModel ViewModel { get; } = viewModel;

    private ObservableCollection<ComponentSettings>? _sourceCollection;
    
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
        if (targetContext is not ObservableCollection<ComponentSettings> components)
        {
            return;
        }
        switch (sourceContext)
        {
            case ComponentInfo info:
                var componentSettings = new ComponentSettings()
                {
                    Id = info.Guid.ToString()
                };
                components.Add(componentSettings);
                ComponentsService.LoadComponentSettings(componentSettings,
                    componentSettings.AssociatedComponentInfo.ComponentType!.BaseType!);
                break;
            case ComponentSettings settings:
                var oldIndex = components.IndexOf(settings);
                if (settings == ViewModel.SelectedRootComponent && components == ViewModel.SelectedComponentContainerChildren)
                {
                    return;
                }
                if (!components.Contains(settings))
                {
                    _sourceCollection?.Remove(settings);
                    components.Add(settings);
                    break;
                }
                // 我们不处理列表内更换位置的拖动操作
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

        var listBox = item.FindAncestorOfType<ListBox>();
        if (listBox?.ItemsSource is ObservableCollection<ComponentSettings> collection)
        {
            _sourceCollection = collection;
        }
    }

    public void AfterDragDrop(object? sender, PointerEventArgs e, object? context)
    {
        _sourceCollection = null;
    }
}