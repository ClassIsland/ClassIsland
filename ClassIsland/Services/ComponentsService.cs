using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Components;
using ClassIsland.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Services;

public class ComponentsService : ObservableRecipient, IComponentsService
{
    private ObservableCollection<ComponentSettings> _currentComponents = new();

    public ObservableCollection<ComponentSettings> CurrentComponents
    {
        get => _currentComponents;
        set
        {
            if (Equals(value, _currentComponents)) return;
            _currentComponents = value;
            OnPropertyChanged();
        }
    }

    public ComponentBase? GetComponent(ComponentSettings settings, bool isSettings)
    {
        var type = isSettings ? settings.AssociatedComponentInfo.SettingsType : settings.AssociatedComponentInfo.ComponentType;
        if (type == null)
        {
            return null;
        }

        var c = IAppHost.Host?.Services.GetService(type);
        if (c is not ComponentBase component)
        {
            return null;
        }

        var baseType = type.BaseType;
        if (baseType?.GetGenericArguments().Length > 0)
        {
            var settingsType = baseType.GetGenericArguments().First();
            var componentSettings = settings.Settings ?? Activator.CreateInstance(settingsType);
            if (componentSettings is JsonElement json)
            {
                componentSettings = json.Deserialize(settingsType);
            }
            settings.Settings = componentSettings;

            component.SettingsInternal = componentSettings;
        }
        return component;
    }
}