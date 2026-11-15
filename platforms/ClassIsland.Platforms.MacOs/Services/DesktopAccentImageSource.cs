using AppKit;
using Avalonia.Threading;
using ClassIsland.Platforms.Abstraction.Services;
using CoreGraphics;
using Foundation;
using ImageIO;

namespace ClassIsland.Platforms.MacOs.Services;

public sealed class DesktopAccentImageSource : IDesktopAccentImageSource
{
    public bool SupportsWallpaper => true;
    public bool SupportsScreen => false;

    public async ValueTask<Stream?> CaptureWallpaperAsync(string windowClassName, bool compatibilityMode,
        CancellationToken cancellationToken)
    {
        var path = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (NSScreen.Screens.FirstOrDefault() is not { } screen)
                return null;
            using var url = NSWorkspace.SharedWorkspace.DesktopImageUrl(screen);
            return url?.IsFileUrl == true ? url.Path : null;
        }, DispatcherPriority.Default, cancellationToken);
        if (path == null)
            return null;

        using var image = await Task.Run(() => CreateThumbnail(path), cancellationToken);
        if (image == null)
            return null;

        // NSBitmapImageRep 是 AppKit 类型，只能在主线程上创建和使用。
        using var png = await Dispatcher.UIThread.InvokeAsync(() => EncodePng(image),
            DispatcherPriority.Default, cancellationToken);
        return png == null ? null : new MemoryStream(png.ToArray(), writable: false);
    }

    private static CGImage? CreateThumbnail(string path)
    {
        using var pool = new NSAutoreleasePool();
        using var url = NSUrl.FromFilename(path);
        using var source = CGImageSource.FromUrl(url);
        if (source == null || source.ImageCount == 0)
            return null;
        return source.CreateThumbnail((int)source.GetPrimaryImageIndex(), new CGImageThumbnailOptions
        {
            MaxPixelSize = 640,
            CreateThumbnailFromImageAlways = true,
            CreateThumbnailWithTransform = true
        });
    }

    private static NSData? EncodePng(CGImage image)
    {
        using var bitmap = new NSBitmapImageRep(image);
        return bitmap.RepresentationUsingTypeProperties(NSBitmapImageFileType.Png);
    }
}
