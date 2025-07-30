using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class TaskBarIconService(ILogger<TaskBarIconService> logger) : IHostedService, ITaskBarIconService
{
    public ILogger<TaskBarIconService> Logger { get; } = logger;

    public TrayIcon MainTaskBarIcon
    {
        get;
    } = new()
    {
        Icon = new WindowIcon(OperatingSystem.IsMacOS() ? "../Resources/Assets/AppLogo.png" : "Assets/AppLogo.png"),
        ToolTipText = "ClassIsland"
    };

    public void ShowNotification(string title, string content, Action? clickedCallback = null)
    {
        
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