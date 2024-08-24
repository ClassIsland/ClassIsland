using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Components;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Services;

using ComponentSettingsList = ObservableCollection<ComponentSettings>;

public class ComponentsService : ObservableRecipient, IComponentsService
{
    public static readonly string ComponentSettingsPath = Path.Combine(App.AppConfigPath, "Islands/");

    private ComponentSettingsList _currentComponents = new();
    private IReadOnlyList<string> _componentConfigs = new List<string>();

    public static ComponentSettingsList DefaultComponents { get; } = new()
    {
        new ComponentSettings
        {
            Id = "DF3F8295-21F6-482E-BADA-FA0E5F14BB66"
        },
        new ComponentSettings
        {
            Id = "1DB2017D-E374-4BC6-9D57-0B4ADF03A6B8"
        }
    };

    private string SelectedConfigFullPath =>
        Path.GetFullPath(Path.Combine(ComponentSettingsPath, SettingsService.Settings.CurrentComponentConfig + ".json"));

    private string CurrentConfigFullPath =>
        Path.GetFullPath(Path.Combine(ComponentSettingsPath, CurrentConfigName + ".json"));

    private SettingsService SettingsService { get; }

    private string CurrentConfigName { get; set; } = "Default";

    public ComponentsService(SettingsService settingsService)
    {
        SettingsService = settingsService;
        SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;

        if (!Directory.Exists(ComponentSettingsPath))
        {
            Directory.CreateDirectory(ComponentSettingsPath);
        }

        RefreshConfigs();
        LoadConfig();
        RefreshConfigs();
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SettingsService.Settings.CurrentComponentConfig))
            return;
        SaveConfig();
        LoadConfig();
    }

    private void LoadConfig()
    {
        if (!File.Exists(SelectedConfigFullPath))
        {
            CurrentComponents = ConfigureFileHelper.CopyObject(DefaultComponents);
            SaveConfig();
        }
        else
        {
            CurrentComponents = ConfigureFileHelper.LoadConfig<ComponentSettingsList>(SelectedConfigFullPath);
        }
        CurrentConfigName = SettingsService.Settings.CurrentComponentConfig;
        CurrentComponents.CollectionChanged += (s, e) => ConfigureFileHelper.SaveConfig(CurrentConfigFullPath, CurrentComponents);
    }

    public void SaveConfig()
    {
        ConfigureFileHelper.SaveConfig(CurrentConfigFullPath, CurrentComponents);
    }

    public IReadOnlyList<string> ComponentConfigs
    {
        get => _componentConfigs;
        set
        {
            if (Equals(value, _componentConfigs)) return;
            _componentConfigs = value;
            OnPropertyChanged();
        }
    }

    public void RefreshConfigs()
    {
        ComponentConfigs = Directory.GetFiles(ComponentSettingsPath, "*.json").Select(Path.GetFileNameWithoutExtension).SkipWhile(x => x is null).ToList()!;
    }


    public ComponentSettingsList CurrentComponents
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