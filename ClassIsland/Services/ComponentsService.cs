using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Models.Components;
using ClassIsland.Core.Services.Registry;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Sentry;

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
    public ILogger<ComponentsService> Logger { get; }
    public IManagementService ManagementService { get; }

    private string CurrentConfigName { get; set; } = "Default";

    public ComponentsService(SettingsService settingsService, ILogger<ComponentsService> logger, IManagementService managementService)
    {
        SettingsService = settingsService;
        Logger = logger;
        ManagementService = managementService;
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

    public async Task LoadManagementConfig()
    {
        if (!ManagementService.IsManagementEnabled || ManagementService.Connection == null)
        {
            return;
        }
        
        IsManagementMode = true;
        try
        {
            if (!ManagementService.Manifest.ComponentsSource.IsNewerAndNotNull(ManagementService.Versions
                    .ComponentsVersion))
            {
                return;
            }
            CurrentComponents = await ManagementService.Connection
                .SaveJsonAsync<ComponentSettingsList>(ManagementService.Manifest.ComponentsSource.Value!,
                    Management.ManagementService.ManagementComponentsPath);
            ManagementService.Versions.ComponentsVersion = ManagementService.Manifest.ComponentsSource.Version;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "无法从集控拉取组件配置");
            CurrentComponents =
                ConfigureFileHelper.LoadConfig<ComponentSettingsList>(Management.ManagementService
                    .ManagementComponentsPath);
        }
        LoadConfig();
    
    }

    private void LoadConfig()
    {
        if (!IsManagementMode)
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
        }
        
        CurrentConfigName = SettingsService.Settings.CurrentComponentConfig;
        CurrentComponents.CollectionChanged += (s, e) => ConfigureFileHelper.SaveConfig(CurrentConfigFullPath, CurrentComponents);

        var migrated = false;
        foreach (var i in CurrentComponents)
        {
            if (!ComponentRegistryService.MigrationPairs.TryGetValue(new Guid(i.Id), out var targetGuid))
            {
                if (i.AssociatedComponentInfo.ComponentType != null)
                    LoadComponentSettings(i, i.AssociatedComponentInfo.ComponentType.BaseType!);
                continue;
            }
            
            Logger.LogInformation("迁移组件 {} -> {}", i.Id, targetGuid);
            i.IsMigrated = true;
            i.MigrationSource = new Guid(i.Id);
            i.Id = targetGuid.ToString();
            migrated = true;
        }

        if (migrated)
        {
            SaveConfig();
        }
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

    public bool IsManagementMode { get; set; } = false;


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
        var transaction = SentrySdk.StartTransaction("Get Component Instance", "component.getInstance");
        var sb = Stopwatch.StartNew();
        transaction.SetTag("component", settings.AssociatedComponentInfo.Name);
        transaction.SetTag("component.isSettings", isSettings.ToString());
        transaction.SetTag("component.Id", settings.AssociatedComponentInfo.Guid.ToString());
        try
        {
            var type = isSettings ? settings.AssociatedComponentInfo.SettingsType : settings.AssociatedComponentInfo.ComponentType;
            if (type == null)
            {
                transaction.Finish(SpanStatus.NotFound);
                return null;
            }

            var c = IAppHost.Host?.Services.GetService(type);
            if (c is not ComponentBase component)
            {
                transaction.Finish(SpanStatus.NotFound);
                return null;
            }


            var baseType = type.BaseType;
            var migrated = settings.IsMigrated && !isSettings;
            if (migrated)
            {
                if (baseType?.GetGenericArguments().Length > 0)
                {
                    var settingsType = baseType.GetGenericArguments().First();
                    var componentSettings = Activator.CreateInstance(settingsType);
                    settings.Settings = componentSettings;
                    component.SettingsInternal = componentSettings;
                }
                component.OnMigrated(settings.MigrationSource, settings.Settings);
            } 
            if (baseType?.GetGenericArguments().Length > 0 && !migrated)
            {
                var componentSettings = LoadComponentSettings(settings, baseType);

                component.SettingsInternal = componentSettings;
            }
            transaction.Finish(SpanStatus.Ok);
            sb.Stop();
            if (sb.Elapsed >= TimeSpan.FromMilliseconds(500))
            {
                Logger.LogWarning("组件 {}/{} ({}) 初始化消耗了太长时间，耗时为 {}ms", settings.Id, isSettings ? "settings" : "component", settings.AssociatedComponentInfo.Name, sb.ElapsedMilliseconds);
            }
            return component;
        }
        catch (Exception ex)
        {
            transaction.Finish(ex, SpanStatus.InternalError);
            throw;
        }
    }

    internal static object? LoadComponentSettings(ComponentSettings settings, Type baseType)
    {
        var settingsType = baseType.GetGenericArguments().FirstOrDefault();
        if (settingsType == null)
        {
            return null;
        }
        var componentSettings = settings.Settings ?? Activator.CreateInstance(settingsType);
        if (componentSettings is JsonElement json)
        {
            componentSettings = json.Deserialize(settingsType);
        }
        settings.Settings = componentSettings;
        return componentSettings;
    }
}