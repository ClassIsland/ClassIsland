using System.Collections.ObjectModel;
using System.Windows;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Components;
using ClassIsland.Services;
using GongSolutions.Wpf.DragDrop;

namespace ClassIsland.Views.SettingPages;

public class ComponentsSettingsPageDropHandler : DependencyObject, IDropTarget
{
    public static readonly DependencyProperty ComponentsProperty = DependencyProperty.Register(
        nameof(Components), typeof(ObservableCollection<ComponentSettings>), typeof(ComponentsSettingsPageDropHandler), new PropertyMetadata(default(ObservableCollection<ComponentSettings>)));

    public ObservableCollection<ComponentSettings> Components
    {
        get { return (ObservableCollection<ComponentSettings>)GetValue(ComponentsProperty); }
        set { SetValue(ComponentsProperty, value); }
    }

    public static readonly DependencyProperty CurrentSelectedContainerComponentSettingsProperty = DependencyProperty.Register(
        nameof(CurrentSelectedContainerComponentSettings), typeof(ComponentSettings), typeof(ComponentsSettingsPageDropHandler), new PropertyMetadata(default(ComponentSettings)));

    public ComponentSettings? CurrentSelectedContainerComponentSettings
    {
        get { return (ComponentSettings?)GetValue(CurrentSelectedContainerComponentSettingsProperty); }
        set { SetValue(CurrentSelectedContainerComponentSettingsProperty, value); }
    }

    //private IComponentsService ComponentsService { get; } = App.GetService<IComponentsService>();


    public void DragOver(IDropInfo dropInfo)
    {
        // TODO: 如果拖入的组件是当前组件的父组件，要拒绝拖入到子容器中。
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
        var components = Components;
        switch (dropInfo.Data)
        {
            case ComponentInfo info:
                var componentSettings = new ComponentSettings()
                {
                    Id = info.Guid.ToString()
                };
                components.Insert(dropInfo.InsertIndex, componentSettings);
                ComponentsService.LoadComponentSettings(componentSettings,
                    componentSettings.AssociatedComponentInfo.ComponentType!.BaseType!);
                break;
            case ComponentSettings settings:
                var oldIndex = components.IndexOf(settings);
                var newIndex = oldIndex < dropInfo.UnfilteredInsertIndex ? dropInfo.UnfilteredInsertIndex - 1 : dropInfo.UnfilteredInsertIndex;
                var finalIndex = newIndex >= components.Count ? components.Count - 1 : newIndex;
                if (!components.Contains(settings))
                {
                    var source = dropInfo.DragInfo.SourceCollection as ObservableCollection<ComponentSettings>;
                    source?.Remove(settings);
                    components.Insert(newIndex + 1, settings);
                    break;
                }
                if (oldIndex != finalIndex)
                {
                    components.Move(oldIndex, finalIndex);
                }
                break;
        }
    }
}