using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Components;
using GongSolutions.Wpf.DragDrop;

namespace ClassIsland.Views.SettingPages;

public class ComponentsSettingsPageDropHandler : IDropTarget
{
    private IComponentsService ComponentsService { get; } = App.GetService<IComponentsService>();


    public void DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is not ComponentInfo && dropInfo.Data is not ComponentSettings) 
            return;
        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
        dropInfo.Effects = dropInfo.Data switch
        {
            ComponentInfo info => DragDropEffects.Copy,
            ComponentSettings settings => DragDropEffects.Move,
            _ => DragDropEffects.None
        };
    }

    public void Drop(IDropInfo dropInfo)
    {
        var components = ComponentsService.CurrentComponents;
        switch (dropInfo.Data)
        {
            case ComponentInfo info:
                components.Insert(dropInfo.InsertIndex, new ComponentSettings()
                {
                    Id = info.Guid.ToString()
                });
                break;
            case ComponentSettings settings:
                var oldIndex = components.IndexOf(settings);
                var newIndex = oldIndex < dropInfo.UnfilteredInsertIndex ? dropInfo.UnfilteredInsertIndex - 1 : dropInfo.UnfilteredInsertIndex;
                var finalIndex = newIndex >= components.Count ? components.Count - 1 : newIndex;
                if (oldIndex != finalIndex)
                {
                    components.Move(oldIndex, finalIndex);
                }
                break;
        }
    }
}