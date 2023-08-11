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
    public TaskbarIcon TaskBarIcon
    {
        get;
    } = new()
    {
        IconSource = new BitmapImage(new Uri("pack://application:,,,/ClassIsland;component/Assets/AppLogo.ico", UriKind.Absolute)),
        MenuActivation = PopupActivationMode.LeftOrRightClick,
        ToolTipText = "ClassIsland"
    };

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => null;
}