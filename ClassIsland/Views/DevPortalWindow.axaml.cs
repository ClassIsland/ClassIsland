using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Shared;
using ClassIsland.ViewModels;

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
}