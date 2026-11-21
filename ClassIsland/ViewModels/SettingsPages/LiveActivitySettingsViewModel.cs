using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Icons;
using ClassIsland.Models;
using ClassIsland.Models.LiveActivities;
using ClassIsland.Platforms.Abstraction.Models.LiveActivities;
using ClassIsland.Services;
using ClassIsland.Services.LiveActivities;
using ClassIsland.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class LiveActivitySettingsViewModel(
    SettingsService settingsService,
    IWeatherService? weatherService = null) : ObservableObject
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

    public ObservableCollection<LiveActivityPreviewRow> PreviewRows { get; } = [];

    public bool CanAddComponent => Components.Count < LiveActivityComponentSettings.MaximumComponents;

    public string PreviewTitle => SettingsService.Settings.CurrentClassRealtimeNotificationShowSubjectName
        ? "数学"
        : "正在上课";

    public string PreviewRemainingTime => SettingsService.Settings.CurrentClassRealtimeNotificationShowRemainingTime
        ? "32:18"
        : string.Empty;

    public bool PreviewShowPhase => SettingsService.Settings.CurrentClassRealtimeNotificationShowSubjectName;

    public bool PreviewShowRemainingTime => SettingsService.Settings.CurrentClassRealtimeNotificationShowRemainingTime;

    public string PreviewLocation => SettingsService.Settings.CurrentClassRealtimeNotificationShowLocation ? "A301" : "";

    public string PreviewTeacherName => SettingsService.Settings.CurrentClassRealtimeNotificationShowTeacherName ? "张老师" : "";

    public string PreviewTimeRange => SettingsService.Settings.CurrentClassRealtimeNotificationShowTimeRange ? "14:10–14:55" : "";

    public bool PreviewHasMetadata => PreviewLocation.Length > 0 || PreviewTeacherName.Length > 0 || PreviewTimeRange.Length > 0;

    public bool PreviewHasCustomLayout => SettingsService.Settings.IsLiveActivityLayoutCustomized;

    public bool PreviewHasCustomRows => PreviewRows.Count > 0;

    public double PreviewProgressValue => 762.0 / 2700 * 100;

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
        else if (e.PropertyName == nameof(Settings.LastWeatherInfo) ||
                 e.PropertyName?.Contains("RealtimeNotification", StringComparison.Ordinal) == true)
        {
            RefreshPreview();
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
        OnPropertyChanged(nameof(PreviewTitle));
        OnPropertyChanged(nameof(PreviewRemainingTime));
        OnPropertyChanged(nameof(PreviewShowPhase));
        OnPropertyChanged(nameof(PreviewShowRemainingTime));
        OnPropertyChanged(nameof(PreviewLocation));
        OnPropertyChanged(nameof(PreviewTeacherName));
        OnPropertyChanged(nameof(PreviewTimeRange));
        OnPropertyChanged(nameof(PreviewHasMetadata));
        OnPropertyChanged(nameof(PreviewHasCustomLayout));
        PreviewItems.Clear();
        PreviewRows.Clear();
        var settings = SettingsService.Settings;
        var countdownNow = DateTime.Now;
        var now = countdownNow.Date.AddHours(14).AddMinutes(22).AddSeconds(42);
        var snapshot = new LessonsLiveActivitySnapshot(
            TimeState.OnClass, "preview", "上课 · 数学", "", "14:10–14:55", "数",
            ProgressMax: 2700, Progress: 762, RemainingText: "32:18",
            StartTime: new DateTimeOffset(now.Date.AddHours(14).AddMinutes(10)),
            EndTime: new DateTimeOffset(now.Date.AddHours(14).AddMinutes(55)))
        {
            ActivityDetails = new LessonLiveActivityDetails("数学", "A301", "14:10–14:55", "")
            {
                TeacherName = "张老师"
            }
        };
        var components = LiveActivityComponentSnapshotFactory.Create(
            settings, snapshot, now,
            weatherService?.GetWeatherTextByCode(settings.LastWeatherInfo.Current.Weather) ?? "",
            new LessonLiveActivityDetails("物理", "A302", "15:10–15:55", ""),
            LessonLiveActivitySettingsPolicy.GetDisplayOptions(settings, isUpcoming: false));
        if (components == null)
        {
            OnPropertyChanged(nameof(PreviewHasCustomRows));
            return;
        }

        var rows = new List<List<LiveActivityPreviewItem>>();
        foreach (var component in components)
        {
            var item = new LiveActivityPreviewItem(component.Kind, component.Text, component.SecondaryText,
                component.Kind == "separator")
            {
                Glyph = GetPreviewGlyph(component.SymbolName),
                ExtraText = component.ExtraText,
                IsSchedule = component.Kind == "schedule",
                IsCountdown = component.Kind == "countdown",
                ShowProgress = component.Kind == "schedule" && component.ShowProgress,
                ValueText = component.Kind == "countdown" ? GetCountdownPreview(component.TargetTime, countdownNow) : ""
            };
            if (component.Kind == "clock")
            {
                item = item with { Text = now.ToString("H:mm:ss", CultureInfo.InvariantCulture) };
            }

            if (!IsFullWidth(item) && rows.LastOrDefault() is { Count: 1 } previous && !IsFullWidth(previous[0]))
            {
                previous.Add(item);
            }
            else
            {
                rows.Add([item]);
            }
        }

        // 与原生展开态相同：优先保留前面的行，超过系统高度预算后不再显示后续组件。
        var usedHeight = 0;
        foreach (var row in rows)
        {
            var height = row.Max(x => x.IsSeparator ? 14 : x.IsSchedule ? 18 + (x.ShowProgress ? 7 : 0) : 16);
            var nextHeight = usedHeight + (usedHeight > 0 ? 4 : 0) + height;
            if (nextHeight > 86)
            {
                break;
            }

            usedHeight = nextHeight;
            PreviewRows.Add(new LiveActivityPreviewRow(row));
            foreach (var item in row)
            {
                PreviewItems.Add(item);
            }
        }
        OnPropertyChanged(nameof(PreviewHasCustomRows));
    }

    private static bool IsFullWidth(LiveActivityPreviewItem item) => item.IsSchedule || item.Label == "text";

    private static string GetCountdownPreview(double? targetTime, DateTime now)
    {
        if (targetTime == null)
        {
            return "32:18";
        }

        var remaining = DateTimeOffset.FromUnixTimeSeconds((long)targetTime.Value) - new DateTimeOffset(now);
        return remaining <= TimeSpan.Zero
            ? "已到日期"
            : $"{(int)remaining.TotalHours}:{remaining.Minutes:00}:{remaining.Seconds:00}";
    }

    private static string GetPreviewGlyph(string symbolName) => symbolName switch
    {
        "book.closed.fill" => FluentIcons.BookFilled,
        "calendar" => FluentIcons.CalendarRegular,
        "clock" => FluentIcons.ClockRegular,
        "hourglass" => FluentIcons.HourglassRegular,
        "text.alignleft" => FluentIcons.TextAlignLeftRegular,
        "sun.max.fill" => FluentIcons.WeatherSunnyRegular,
        "cloud.sun.fill" => FluentIcons.WeatherPartlyCloudyDayRegular,
        "cloud.snow.fill" => FluentIcons.WeatherSnowRegular,
        "cloud.bolt.rain.fill" => FluentIcons.WeatherThunderstormRegular,
        "cloud.rain.fill" => FluentIcons.WeatherRainRegular,
        "cloud.fog.fill" => FluentIcons.WeatherFogRegular,
        "cloud.fill" or "cloud" => FluentIcons.WeatherCloudyRegular,
        _ => ""
    };

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
    bool IsSeparator)
{
    public string Glyph { get; init; } = "";
    public string ExtraText { get; init; } = "";
    public string ValueText { get; init; } = "";
    public bool IsCountdown { get; init; }
    public bool IsSchedule { get; init; }
    public bool ShowProgress { get; init; }
}

public sealed record LiveActivityPreviewRow(IReadOnlyList<LiveActivityPreviewItem> Items)
{
    public int Columns => Items.Count;
}
