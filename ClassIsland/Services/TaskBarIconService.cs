using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class TaskBarIconService : IHostedService, ITaskBarIconService
{
    public ILogger<TaskBarIconService> Logger { get; }

    public TrayIcon MainTaskBarIcon
    {
        get;
    } = new()
    {
        Icon = new WindowIcon("pack://application:,,,/ClassIsland;component/Assets/AppLogo.png"),
        ToolTipText = "ClassIsland"
    };

    private Action? CurrentNotificationCallback { get; set; }

    private Queue<Action> NotificationQueue { get; set; } = new();

    private bool IsProcessingNotifications { get; set; } = false;

    private void ProcessNotification()
    {
        
    }

    

    public void ShowNotification(string title, string content, Action? clickedCallback = null)
    {
        // todo: 实现通知显示
    }

    public TaskBarIconService(ILogger<TaskBarIconService> logger)
    {
        Logger = logger;
        
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