using System.Collections.ObjectModel;
using Avalonia;

namespace ClassIsland.Models;

public class ComponentSettingsPageDraggingContext : AvaloniaObject
{
    private Core.Models.Components.ComponentSettings _settings;

    public static readonly DirectProperty<ComponentSettingsPageDraggingContext, Core.Models.Components.ComponentSettings> SettingsProperty = AvaloniaProperty.RegisterDirect<ComponentSettingsPageDraggingContext, Core.Models.Components.ComponentSettings>(
        nameof(Settings), o => o.Settings, (o, v) => o.Settings = v);

    public Core.Models.Components.ComponentSettings Settings
    {
        get => _settings;
        set => SetAndRaise(SettingsProperty, ref _settings, value);
    }

    private ObservableCollection<Core.Models.Components.ComponentSettings> _sourceCollection;

    public static readonly DirectProperty<ComponentSettingsPageDraggingContext, ObservableCollection<Core.Models.Components.ComponentSettings>> SourceCollectionProperty = AvaloniaProperty.RegisterDirect<ComponentSettingsPageDraggingContext, ObservableCollection<Core.Models.Components.ComponentSettings>>(
        nameof(SourceCollection), o => o.SourceCollection, (o, v) => o.SourceCollection = v);

    public ObservableCollection<Core.Models.Components.ComponentSettings> SourceCollection
    {
        get => _sourceCollection;
        set => SetAndRaise(SourceCollectionProperty, ref _sourceCollection, value);
    }
}