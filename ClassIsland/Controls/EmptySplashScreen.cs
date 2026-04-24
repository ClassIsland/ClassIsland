using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentAvalonia.UI.Windowing;

namespace ClassIsland.Controls;

public class EmptySplashScreen : IApplicationSplashScreen
{
    private static readonly Lazy<Bitmap> AppIconBitmap = new(() =>
    {
        using var stream = AssetLoader.Open(new Uri("avares://ClassIsland/Assets/AppLogo@384w.png"));
        return new Bitmap(stream);
    });

    static EmptySplashScreen()
    {
        static void DisposeIcon()
        {
            if (AppIconBitmap.IsValueCreated)
            {
                AppIconBitmap.Value.Dispose();
            }
        }

        AppDomain.CurrentDomain.ProcessExit += (_, _) => DisposeIcon();
        AppDomain.CurrentDomain.DomainUnload += (_, _) => DisposeIcon();
    }

    public async Task RunTasks(CancellationToken cancellationToken)
    {
    }

    public string AppName { get; } = "ClassIsland";

    public IImage AppIcon { get; } = AppIconBitmap.Value;
    public object? SplashScreenContent { get; } = null;
    public int MinimumShowTime { get; } = 0;
}
