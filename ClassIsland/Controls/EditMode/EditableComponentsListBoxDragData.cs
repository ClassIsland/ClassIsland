using System.Collections.ObjectModel;
using Avalonia;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Components;

namespace ClassIsland.Controls.EditMode;

public class EditableComponentsListBoxDragData : AvaloniaObject
{
    public static readonly StyledProperty<ComponentSettings?> ComponentSettingsProperty = AvaloniaProperty.Register<EditableComponentsListBoxDragData, ComponentSettings?>(
        nameof(ComponentSettings));

    public ComponentSettings? ComponentSettings
    {
        get => GetValue(ComponentSettingsProperty);
        set => SetValue(ComponentSettingsProperty, value);
    }

    public static readonly StyledProperty<ComponentInfo?> ComponentInfoProperty = AvaloniaProperty.Register<EditableComponentsListBoxDragData, ComponentInfo?>(
        nameof(ComponentInfo));

    public ComponentInfo? ComponentInfo
    {
        get => GetValue(ComponentInfoProperty);
        set => SetValue(ComponentInfoProperty, value);
    }

    public static readonly StyledProperty<ObservableCollection<ComponentSettings>?> SourceListProperty = AvaloniaProperty.Register<EditableComponentsListBoxDragData, ObservableCollection<ComponentSettings>?>(
        nameof(SourceList));

    public ObservableCollection<ComponentSettings>? SourceList
    {
        get => GetValue(SourceListProperty);
        set => SetValue(SourceListProperty, value);
    }
}