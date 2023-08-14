using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Hosting;

namespace ClassIsland;

public class WallpaperPickingService : IHostedService
{
    private static readonly string DesktopWindowClassName = "Progman";

    public static void ColorToHSV(System.Windows.Media.Color color, out double hue, out double saturation, out double value)
    {
        int max = Math.Max(color.R, Math.Max(color.G, color.B));
        int min = Math.Min(color.R, Math.Min(color.G, color.B));

        hue = 0;
        saturation = (max == 0) ? 0 : 1d - (1d * min / max);
        value = max / 255d;
    }

    public static Bitmap GetScreenShot()
    {
        var win = NativeWindowHelper.FindWindow(DesktopWindowClassName, null);
        if (win == IntPtr.Zero)
        {
            return new Bitmap(1920, 1080);
        }

        return WindowCaptureHelper.GetShotCutImage(win);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return new Task(() => {});
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return new Task(() => { });
    }
}