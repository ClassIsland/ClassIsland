using System.Collections.ObjectModel;
using Avalonia.Input;
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
                
                if (!components.Contains(settings))
                {
                    foreach (var line in ViewModel.ComponentsService.CurrentComponents.Lines)
                    {
                        if (line.Children.Remove(settings))
                        {
                            break;
                        }
                    }
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
        return sourceContext is not ComponentSettings settings || settings != ViewModel.SelectedRootComponent
                                                               || !Equals(targetContext, ViewModel.SelectedComponentContainerChildren);
    }

    public void BeforeDragDrop(object? sender, PointerEventArgs e, object? context)
    {
    }

    public void AfterDragDrop(object? sender, PointerEventArgs e, object? context)
    {
    }
}