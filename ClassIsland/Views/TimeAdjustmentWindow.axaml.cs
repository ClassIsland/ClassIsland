using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Helpers;
using ClassIsland.Models.AttachedSettings;
using ClassIsland.Services;
using ClassIsland.Services.NotificationProviders;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels;

namespace ClassIsland.Views;

/// <summary>
/// TimeAdjustmentWindow.xaml 的交互逻辑
/// </summary>
public partial class TimeAdjustmentWindow : MyWindow
{
    public IExactTimeService ExactTimeService { get; }
    public ILessonsService LessonsService { get; }
    public SettingsService SettingsService { get; }
    public INotificationHostService NotificationHostService { get; }
    public TimeAdjustmentViewModel ViewModel { get; } = new();

    private static readonly Guid ClassNotificationProviderGuid = new Guid("08F0D9C3-C770-4093-A3D0-02F3D90C24BC");

    public TimeAdjustmentWindow(IExactTimeService exactTimeService, 
        ILessonsService lessonsService, 
        SettingsService settingsService,
        INotificationHostService notificationHostService)
    {
        ExactTimeService = exactTimeService;
        LessonsService = lessonsService;
        SettingsService = settingsService;
        NotificationHostService = notificationHostService;

        LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
        InitializeComponent();
        SetTargetTime(false);
    }

    private void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
        ViewModel.CurrentTime = ExactTimeService.GetCurrentLocalDateTime();
        
    }

    private void TimeAdjustmentWindow_OnClosed(object? sender, EventArgs e)
    {
        LessonsService.PostMainTimerTicked -= LessonsServiceOnPostMainTimerTicked;
    }

    private void ButtonApplyAdjustment_OnClick(object sender, RoutedEventArgs e)
    {
        var now = ExactTimeService.GetCurrentLocalDateTime();
        var offset = ViewModel.TargetTime - now;
        SettingsService.Settings.TimeOffsetSeconds = Math.Round(SettingsService.Settings.TimeOffsetSeconds + offset.TotalSeconds , 2);
    }

    private void SetTargetTime(bool force)
    {
        if (LessonsService.CurrentClassPlan?.TimeLayout is not { } timeLayout)
        {
            return;
        }

        var provider =
            NotificationHostService.NotificationProviders.FirstOrDefault(x =>
                x.ProviderGuid == ClassNotificationProviderGuid);
        if (provider?.ProviderInstance is not ClassNotificationProvider classNotificationProvider)
        {
            return;
        }

        var now = ExactTimeService.GetCurrentLocalDateTime();

        switch (LessonsService.CurrentState)
        {
            // 上课中
            case TimeState.OnClass when LessonsService.NextBreakingTimeLayoutItem == TimeLayoutItem.Empty:
                ViewModel.TargetTime = new DateTime(DateOnly.FromDateTime(now), TimeOnly.FromTimeSpan(LessonsService.CurrentTimeLayoutItem.EndTime));
                return;
            case TimeState.OnClass:
                ViewModel.TargetTime = new DateTime(DateOnly.FromDateTime(now), TimeOnly.FromTimeSpan(LessonsService.NextBreakingTimeLayoutItem.StartTime));
                return;
            // 课间休息
            case TimeState.Breaking when LessonsService.NextClassTimeLayoutItem == TimeLayoutItem.Empty:
                ViewModel.TargetTime = new DateTime(DateOnly.FromDateTime(now), TimeOnly.FromTimeSpan(LessonsService.CurrentTimeLayoutItem.EndTime));
                return;
            case TimeState.Breaking:
            {
                var nextPrepOnClassDeltaSeconds = classNotificationProvider.GetSettingsDeltaTime();
                var onClassTime = new DateTime(DateOnly.FromDateTime(now), TimeOnly.FromTimeSpan(
                    LessonsService.NextClassTimeLayoutItem.StartTime));
                var prepOnClassTime = onClassTime - TimeSpanHelper.FromSecondsSafe(nextPrepOnClassDeltaSeconds);
                if (now <= prepOnClassTime)
                {
                    ViewModel.TargetTime = prepOnClassTime;
                    return;
                }
                ViewModel.TargetTime = onClassTime;
                return;
                }
            case TimeState.None when LessonsService.NextClassTimeLayoutItem != TimeLayoutItem.Empty:
                ViewModel.TargetTime = new DateTime(DateOnly.FromDateTime(now), TimeOnly.FromTimeSpan(
                    LessonsService.NextClassTimeLayoutItem.StartTime));
                return;
            case TimeState.None when LessonsService.NextBreakingTimeLayoutItem != TimeLayoutItem.Empty:
                ViewModel.TargetTime = new DateTime(DateOnly.FromDateTime(now), TimeOnly.FromTimeSpan(
                    LessonsService.NextBreakingTimeLayoutItem.StartTime));
                return;
            case TimeState.PrepareOnClass:  // 弃用
            case TimeState.AfterSchool:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (force)
        {
            return;
        }
        AdjustToNextMinute();
    }

    private void ButtonAdjustToNextEvent_OnClick(object sender, RoutedEventArgs e)
    {
        SetTargetTime(true);
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsClockOpen = true;
    }

    private void ButtonAdjustToNextMinute_OnClick(object sender, RoutedEventArgs e)
    {
        AdjustToNextMinute();
    }

    private void AdjustToNextMinute()
    {
        var now = ExactTimeService.GetCurrentLocalDateTime();
        var nextMinute = now.AddMinutes(1);
        ViewModel.TargetTime = new DateTime(nextMinute.Year, nextMinute.Month, nextMinute.Day, nextMinute.Hour,
            nextMinute.Minute, 0);
    }
}
