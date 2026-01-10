using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;
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

    public static FuncMultiValueConverter<object?, EditableComponentsListBoxDragData?> Create { get; } = new(o =>
    {
        var l = o.ToList();
        if (l.Count < 2 || l[0] is not ComponentSettings settings ||
            l[1] is not ObservableCollection<ComponentSettings> source)
            return null;
        return new EditableComponentsListBoxDragData()
        {
            ComponentSettings = settings,
            SourceList = source
        };
    });
}