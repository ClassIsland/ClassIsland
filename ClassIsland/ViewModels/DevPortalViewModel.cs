using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Helpers;
using ClassIsland.Models;
using ClassIsland.Models.AttachedSettings;
using ClassIsland.Services;
using ClassIsland.Services.NotificationProviders;
using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class DevPortalViewModel(
    INotificationHostService notificationHostService,
    SettingsService settingsService,
    IExactTimeService exactTimeService,
    IWeatherService weatherService,
    ITutorialService tutorialService) : ObservableObject
{
    public INotificationHostService NotificationHostService { get; } = notificationHostService;
    public SettingsService SettingsService { get; } = settingsService;
    public IExactTimeService ExactTimeService { get; } = exactTimeService;
    public IWeatherService WeatherService { get; } = weatherService;
    public ITutorialService TutorialService { get; } = tutorialService;

    internal DateTime? GetPreparingNotificationTargetTime(ILessonsService lessonsService, IProfileService profileService)
    {
        var now = ExactTimeService.GetCurrentLocalDateTime();
        var classPlan = lessonsService.CurrentClassPlan;
        var provider = NotificationHostService.NotificationProviders
            .Select(x => x.ProviderInstance)
            .OfType<ClassNotificationProvider>()
            .FirstOrDefault();
        if (!SettingsService.Settings.IsNotificationEnabled || !lessonsService.IsClassPlanEnabled ||
            classPlan?.TimeLayout == null || provider == null)
        {
            return null;
        }

        var validItems = classPlan.ValidTimeLayoutItems;
        List<DateTime> targets = [];
        foreach (var item in validItems.Where(x => x.TimeType == 0))
        {
            var classInfo = classPlan.Classes.FirstOrDefault(x => x.CurrentTimeLayoutItem == item);
            var subject = classInfo != null && profileService.Profile.Subjects.TryGetValue(classInfo.SubjectId, out var value)
                ? value
                : Subject.Fallback;
            var attachedSettings = IAttachedSettingsHostService.GetAttachedSettingsByPriority<ClassNotificationAttachedSettings>(
                provider.ProviderGuid, subject, item, classPlan, classPlan.TimeLayout);
            IClassNotificationSettings settings = attachedSettings ?? (IClassNotificationSettings)provider.Settings;
            var deltaSeconds = attachedSettings?.ClassPreparingDeltaTime ?? (subject.IsOutDoor
                ? provider.Settings.OutDoorClassPreparingDeltaTime
                : provider.Settings.InDoorClassPreparingDeltaTime);
            if (!settings.IsClassOnPreparingNotificationEnabled || deltaSeconds <= 0)
            {
                continue;
            }

            var windowStart = item.StartTime - TimeSpanHelper.FromSecondsSafe(deltaSeconds);
            windowStart = windowStart < TimeSpan.Zero ? TimeSpan.Zero : windowStart;
            // 提醒可能要等上一节课结束才能触发；结束时间本身仍算上课中。
            var candidates = validItems.Where(x => x.TimeType == 0)
                .Select(x => x.EndTime + TimeSpan.FromMilliseconds(1))
                .Append(windowStart)
                .Where(x => x >= windowStart && x < item.StartTime)
                .OrderBy(x => x);
            foreach (var candidate in candidates)
            {
                var currentItem = validItems.FirstOrDefault(x => x.TimeType is 0 or 1 &&
                    x.StartTime <= candidate && x.EndTime >= candidate);
                var nextClass = validItems.FirstOrDefault(x => x.TimeType == 0 && x.EndTime >= candidate);
                if (currentItem?.TimeType == 0 || nextClass != item)
                {
                    continue;
                }

                // 先跳到窗口前，让计时器清除上一条提醒的状态，再自然触发新的提醒。
                var targetTime = candidate - TimeSpan.FromSeconds(1);
                targets.Add(now.Date + (targetTime < TimeSpan.Zero ? TimeSpan.Zero : targetTime));
                break;
            }
        }

        var orderedTargets = targets.OrderBy(x => x).ToList();
        return orderedTargets.Where(x => x >= now).Select(x => (DateTime?)x).FirstOrDefault()
               ?? orderedTargets.Select(x => (DateTime?)x).FirstOrDefault();
    }

    [ObservableProperty] private string _notificationMaskText = "";
    
    [ObservableProperty] private string _notificationOverlayText = "";

    [ObservableProperty] private DateTime _targetDate = DateTime.Now;
    
    [ObservableProperty] private TimeSpan _targetTime = TimeSpan.Zero;

    [ObservableProperty] private bool _isTargetDateLoaded = false;

    [ObservableProperty] private bool _isTargetTimeLoaded = false;

    [ObservableProperty] private string _toastTitle = "";
    [ObservableProperty] private string _toastMessage = "";
    [ObservableProperty] private bool _toastHaveActions = false;
    [ObservableProperty] private bool _toastCanUserClose = true;
    [ObservableProperty] private object? _oobeIntroControlContent = new Border();
    [ObservableProperty] private string _markdownText =
        """
        # Welcome to ClassIsland!
        
        
        """;

    [ObservableProperty] private ISplashProvider? _splashProvider;

    public bool IsTargetDateTimeLoaded => IsTargetDateLoaded && IsTargetTimeLoaded;

    [ObservableProperty] private string _styleSelector = "Control";

    [ObservableProperty] private string? _iconExpression = "";
}
