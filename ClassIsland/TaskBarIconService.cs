using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.Extensions.Hosting;

namespace ClassIsland;

public class TaskBarIconService : BackgroundService
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

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => new Task(() =>
    {
    });

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        return base.StartAsync(cancellationToken);
    }
}