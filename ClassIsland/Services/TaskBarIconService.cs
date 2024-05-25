using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using H.NotifyIcon;
using H.NotifyIcon.Core;

using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services;

public class TaskBarIconService : IHostedService
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
        MainTaskBarIcon.MenuActivation = SettingsService.Settings.TaskBarIconClickBehavior == 0 ? 
            PopupActivationMode.LeftOrRightClick : PopupActivationMode.RightClick;
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
        MenuActivation = PopupActivationMode.LeftOrRightClick,
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