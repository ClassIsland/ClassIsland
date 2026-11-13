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

public class TaskBarIconService : IHostedService, ITaskBarIconService
{
    public TaskBarIconService(ILogger<TaskBarIconService> logger)
    {
        Logger = logger;
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
        {
            MainTaskBarIcon.Icon = new WindowIcon(OperatingSystem.IsMacOS()
                ? "../Resources/Assets/AppLogo_Monochrome.png"
                : "Assets/AppLogo.png");

            // Avalonia 12.1.4 会在注册时绑定菜单，macOS 需等 PostInit 设置最终菜单再注册。
            if (!OperatingSystem.IsMacOS())
            {
                var icons = TrayIcon.GetIcons(AppBase.Current) ?? new TrayIcons();
                icons.Add(MainTaskBarIcon);
                TrayIcon.SetIcons(AppBase.Current, icons);
            }
        }
        AppBase.Current.AppStopping += CurrentOnAppStopping;
    }

    private void CurrentOnAppStopping(object? sender, EventArgs e)
    {
        MainTaskBarIcon.IsVisible = false;
    }

    public ILogger<TaskBarIconService> Logger { get; }

    public TrayIcon MainTaskBarIcon
    {
        get;
    } = new()
    {
        ToolTipText = "ClassIsland"
    };

    public IList<NativeMenuItemBase> MoreOptionsMenuItems => MoreOptionsMenu!.Items;
    
    public NativeMenu? MoreOptionsMenu { get; set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        return;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        return;
    }
}
