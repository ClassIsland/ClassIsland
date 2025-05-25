using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using ClassIsland.Core.Abstractions.Services;
using H.NotifyIcon;
using H.NotifyIcon.Core;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class TaskBarIconService : IHostedService, ITaskBarIconService
{
    public ILogger<TaskBarIconService> Logger { get; }

    public TaskbarIcon MainTaskBarIcon
    {
        get;
    } = new()
    {
        IconSource = new GeneratedIconSource()
        {
            BackgroundSource = new BitmapImage(new Uri("pack://application:,,,/ClassIsland;component/Assets/AppLogo.png", UriKind.Absolute)),
        },
        MenuActivation = PopupActivationMode.RightClick,
        ToolTipText = "ClassIsland"
    };

    private Action? CurrentNotificationCallback { get; set; }

    private Queue<Action> NotificationQueue { get; set; } = new();

    private bool IsProcessingNotifications { get; set; } = false;

    private void ProcessNotification()
    {
        MainTaskBarIcon.TrayBalloonTipClosed -= MainTaskBarIconOnTrayBalloonTipClosed;
        MainTaskBarIcon.TrayBalloonTipClosed += MainTaskBarIconOnTrayBalloonTipClosed;

        if (NotificationQueue.Count > 0)
        {
            var notificationAction = NotificationQueue.Dequeue();
            try
            {
                notificationAction();
                return;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "无法显示气泡通知");
            }
        }
        
        CurrentNotificationCallback = null;
        IsProcessingNotifications = false;
    }

    private void MainTaskBarIconOnTrayBalloonTipClosed(object sender, RoutedEventArgs e)
    {
        ProcessNotification();
    }

    public void ShowNotification(string title, string content, NotificationIcon icon, Action? clickedCallback = null)
    {
        NotificationQueue.Enqueue(() =>
        {
            CurrentNotificationCallback = clickedCallback;
            MainTaskBarIcon.ShowNotification(title, content, icon);
        });
        if (!IsProcessingNotifications)
        {
            ProcessNotification();
        }
    }

    public TaskBarIconService(ILogger<TaskBarIconService> logger)
    {
        Logger = logger;
        MainTaskBarIcon.TrayBalloonTipClicked += MainTaskBarIconOnTrayBalloonTipClicked;
    }

    private void MainTaskBarIconOnTrayBalloonTipClicked(object sender, RoutedEventArgs e)
    {
        CurrentNotificationCallback?.Invoke();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        return;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        return;
    }
}