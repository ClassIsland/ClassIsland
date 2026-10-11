using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ClassIsland.Models;
using ClassIsland.Models.LiveActivities;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class LiveActivitySettingsViewModel(SettingsService settingsService) : ObservableObject
{
    private Settings? _subscribedSettings;

    public SettingsService SettingsService { get; } = settingsService;

    public ObservableCollection<LiveActivityComponentSettings> Components =>
        SettingsService.Settings.LiveActivityComponents;

    public IReadOnlyList<LiveActivityComponentOption> AvailableKinds =>
        LiveActivityComponentSettings.AvailableKinds;

    public bool CanAddComponent => Components.Count < LiveActivityComponentSettings.MaximumComponents;

    public void Activate()
    {
        if (_subscribedSettings != null)
        {
            return;
        }

        SettingsService.PropertyChanged += SettingsServiceOnPropertyChanged;
        SubscribeToSettings();
    }

    public void Deactivate()
    {
        SettingsService.PropertyChanged -= SettingsServiceOnPropertyChanged;
        if (_subscribedSettings != null)
        {
            _subscribedSettings.PropertyChanged -= SettingsOnPropertyChanged;
            _subscribedSettings = null;
        }
    }

    private void SettingsServiceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsService.Settings))
        {
            SubscribeToSettings();
        }
    }

    private void SubscribeToSettings()
    {
        if (_subscribedSettings != null)
        {
            _subscribedSettings.PropertyChanged -= SettingsOnPropertyChanged;
        }

        _subscribedSettings = SettingsService.Settings;
        _subscribedSettings.PropertyChanged += SettingsOnPropertyChanged;
        NotifyComponentsChanged();
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Settings.LiveActivityComponents) or
            nameof(Settings.IsLiveActivityLayoutCustomized))
        {
            NotifyComponentsChanged();
            SettingsService.SaveSettings("灵动岛展开态组件已更新");
        }
    }

    private void NotifyComponentsChanged()
    {
        OnPropertyChanged(nameof(Components));
        OnPropertyChanged(nameof(CanAddComponent));
        AddComponentCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanAddComponent))]
    private void AddComponent(LiveActivityComponentKind kind)
    {
        if (CanAddComponent && Enum.IsDefined(kind))
        {
            Components.Add(new LiveActivityComponentSettings { Kind = kind });
        }
    }

    [RelayCommand]
    private void RemoveComponent(LiveActivityComponentSettings component) => Components.Remove(component);

    [RelayCommand]
    private void MoveUp(LiveActivityComponentSettings component) => Move(component, -1);

    [RelayCommand]
    private void MoveDown(LiveActivityComponentSettings component) => Move(component, 1);

    private void Move(LiveActivityComponentSettings component, int offset)
    {
        var index = Components.IndexOf(component);
        var target = index + offset;
        if (index >= 0 && target >= 0 && target < Components.Count)
        {
            Components.Move(index, target);
        }
    }

    [RelayCommand]
    private void RestoreDefaults() =>
        SettingsService.Settings.LiveActivityComponents = LiveActivityComponentSettings.CreateDefault();
}
