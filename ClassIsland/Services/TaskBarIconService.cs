using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ClassIsland.Core.Abstractions.Services;
using H.NotifyIcon;
using H.NotifyIcon.Core;

using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services;

public class TaskBarIconService : IHostedService, ITaskBarIconService
{
    private SettingsService SettingsService { get; }

    public TaskBarIconService(SettingsService settingsService)
    {
        SettingsService = settingsService;
        SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
        UpdateMenuAction();
    }

    private void UpdateMenuAction()
    {
        // 由于 ClassIsland 现在会自行处理左键抬起事件来显示菜单，
        // 所以只保留默认的右键打开菜单方式。
        MainTaskBarIcon.MenuActivation = PopupActivationMode.RightClick;
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateMenuAction();
    }

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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        return;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        return;
    }
}