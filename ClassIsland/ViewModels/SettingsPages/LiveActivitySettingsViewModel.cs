using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ClassIsland.Models;
using ClassIsland.Models.LiveActivities;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class LiveActivitySettingsViewModel(SettingsService settingsService) : ObservableObject
{
    private Settings? _subscribedSettings;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsComponentSelected))]
    private LiveActivityComponentSettings? _selectedComponent;

    public SettingsService SettingsService { get; } = settingsService;

    public ObservableCollection<LiveActivityComponentSettings> Components =>
        SettingsService.Settings.LiveActivityComponents;

    public IReadOnlyList<LiveActivityComponentOption> AvailableKinds =>
        LiveActivityComponentSettings.AvailableKinds;

    public bool IsComponentSelected => SelectedComponent != null;

    public ObservableCollection<LiveActivityPreviewItem> PreviewItems { get; } = [];

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
        RefreshPreview();
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
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        PreviewItems.Clear();
        if (!SettingsService.Settings.IsLiveActivityLayoutCustomized)
        {
            PreviewItems.Add(new LiveActivityPreviewItem("预览", "开启自定义展开态后显示组件", "", false));
            return;
        }

        foreach (var component in Components.Where(x => x.IsEnabled).Take(LiveActivityComponentSettings.MaximumComponents))
        {
            var item = component.Kind switch
            {
                LiveActivityComponentKind.Schedule => new LiveActivityPreviewItem(
                    "课程表", "高等数学 · A301", component.ShowNextLesson ? "下一节：物理 15:10" : "14:10–14:55", false),
                LiveActivityComponentKind.Date => new LiveActivityPreviewItem("日期", DateTime.Now.ToString("M月d日 ddd"), "", false),
                LiveActivityComponentKind.Clock => new LiveActivityPreviewItem("时钟", DateTime.Now.ToString("HH:mm"), "", false),
                LiveActivityComponentKind.Weather => new LiveActivityPreviewItem("天气简报", "☀ 23°C", "多云", false),
                LiveActivityComponentKind.Countdown => new LiveActivityPreviewItem(
                    "倒计时", component.UseCustomCountdown ? component.CountdownName : "课程剩余", "32:18", false),
                LiveActivityComponentKind.Text => new LiveActivityPreviewItem("文本", string.IsNullOrWhiteSpace(component.Text) ? "短文本预览" : component.Text, "", false),
                LiveActivityComponentKind.Separator => new LiveActivityPreviewItem("", "", "", true),
                _ => null
            };
            if (item != null)
            {
                PreviewItems.Add(item);
            }
        }
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
    private void RemoveComponent(LiveActivityComponentSettings component)
    {
        if (ReferenceEquals(SelectedComponent, component))
        {
            SelectedComponent = null;
        }
        Components.Remove(component);
    }

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

public sealed record LiveActivityPreviewItem(
    string Label,
    string Text,
    string SecondaryText,
    bool IsSeparator);
