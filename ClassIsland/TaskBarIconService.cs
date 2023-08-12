using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.Extensions.Hosting;

namespace ClassIsland;

public class TaskBarIconService : IHostedService
{
    public TaskbarIcon MainTaskBarIcon
    {
        get;
    } = new()
    {
        IconSource = new BitmapImage(new Uri("pack://application:,,,/ClassIsland;component/Assets/AppLogo.ico", UriKind.Absolute)),
        MenuActivation = PopupActivationMode.LeftOrRightClick,
        ToolTipText = "ClassIsland"
    };

    public TaskbarIcon UpdateNotificationIcon
    {
        get;
    } = new()
    {
        IconSource = new BitmapImage(new Uri("pack://application:,,,/ClassIsland;component/Assets/AppLogo.ico", UriKind.Absolute)),
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