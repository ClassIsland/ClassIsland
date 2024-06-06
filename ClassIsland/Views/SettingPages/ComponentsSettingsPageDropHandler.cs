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
        if (dropInfo is not { Data: ComponentInfo sourceItem }) 
            return;
        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
        dropInfo.Effects = DragDropEffects.Copy;
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (dropInfo is not { Data: ComponentInfo sourceItem, TargetCollection: ICollection<ComponentSettings> targetCollection})
            return;
        ComponentsService.CurrentComponents.Add(new ComponentSettings()
        {
            Id = sourceItem.Guid.ToString()
        });
    }
}