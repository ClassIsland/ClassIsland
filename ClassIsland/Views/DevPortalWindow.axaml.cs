using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Controls;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Core.Models.UI;
using ClassIsland.Shared;
using ClassIsland.ViewModels;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Views;

public partial class DevPortalWindow : MyWindow
{
    public DevPortalViewModel ViewModel { get; } = IAppHost.GetService<DevPortalViewModel>();
    
    public DevPortalWindow()
    {
        DataContext = this;
        InitializeComponent();
    }

    private void ButtonSendNotification_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.NotificationHostService.ShowNotification(new NotificationRequest()
        {
            MaskContent = NotificationContent.CreateTwoIconsMask(ViewModel.NotificationMaskText, factory: x =>
            {
                // x.Duration = TimeSpan.FromSeconds(15);
            }),
            OverlayContent = NotificationContent.CreateSimpleTextContent(ViewModel.NotificationOverlayText)
        }, new Guid("4B12F124-8585-43C7-AFC5-7BBB7CBE60D6"), Guid.Empty, true);
    }

    private void ButtonRunChain_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.NotificationHostService.ShowChainedNotifications([
            new NotificationRequest()
            {
                MaskContent = NotificationContent.CreateTwoIconsMask("第一条通知")
            },
            new NotificationRequest()
            {
                MaskContent = NotificationContent.CreateTwoIconsMask("第二条通知"),
                OverlayContent = NotificationContent.CreateSimpleTextContent("我是，终将升起的烈阳！")
            }
        ], new Guid("4B12F124-8585-43C7-AFC5-7BBB7CBE60D6"), Guid.Empty);
    }
    
    private void ButtonReset_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.DebugTimeOffsetSeconds = 0;
        ViewModel.IsTargetDateLoaded = ViewModel.IsTargetTimeLoaded = false;
        ViewModel.TargetDate = ViewModel.ExactTimeService.GetCurrentLocalDateTime().Date;
        ViewModel.TargetTime = ViewModel.ExactTimeService.GetCurrentLocalDateTime().TimeOfDay;
        ViewModel.IsTargetDateLoaded = ViewModel.IsTargetTimeLoaded = true;
    }

    private void TargetTime_OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.TargetTime = ViewModel.ExactTimeService.GetCurrentLocalDateTime().TimeOfDay;
        ViewModel.IsTargetTimeLoaded = true;
    }

    private void TimePicker_OnSelectedTimeChanged(object? sender, TimePickerSelectedValueChangedEventArgs e)
    {
        if (!ViewModel.IsTargetDateTimeLoaded) return;

        DateTime now = ViewModel.ExactTimeService.GetCurrentLocalDateTime();
        DateTime tar = new(DateOnly.FromDateTime(now), TimeOnly.FromTimeSpan(e.NewTime ?? TimeSpan.Zero));

        ViewModel.SettingsService.Settings.DebugTimeOffsetSeconds += Math.Round((tar - now).TotalSeconds);
    }

    private void DatePicker_OnSelectedDateChanged(object? sender, SelectionChangedEventArgs selectionChangedEventArgs)
    {
        if (!ViewModel.IsTargetDateTimeLoaded) return;

        DateTime now = ViewModel.ExactTimeService.GetCurrentLocalDateTime().Date;
        DateTime tar = new(DateOnly.FromDateTime(ViewModel.TargetDate), TimeOnly.FromTimeSpan(now.TimeOfDay));

        ViewModel.SettingsService.Settings.DebugTimeOffsetSeconds += Math.Round((tar - now).TotalSeconds);
    }

    private void TargetDate_OnLoaded(object? sender, RoutedEventArgs e)
    {
        ViewModel.TargetDate = ViewModel.ExactTimeService.GetCurrentLocalDateTime().Date;
        ViewModel.IsTargetDateLoaded = true;
    }

    private void ButtonSendToast_OnClick(object? sender, RoutedEventArgs e)
    {
        this.ShowToast(new ToastMessage()
        {
            Title = ViewModel.ToastTitle,
            Message = ViewModel.ToastMessage,
            CanUserClose = ViewModel.ToastCanUserClose,
            ActionContent = ViewModel.ToastHaveActions ? new Button() {Content = "Test"} : null
        });
    }

    private void ButtonPlayOobeAnimation_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.OobeIntroControlContent = new OobeIntroAnimationControl();
    }
}