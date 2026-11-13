using AppKit;
using Avalonia.Threading;
using ClassIsland.Platforms.Abstraction.Services;
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
        });
        return path == null ? null : await Task.Run(() => DecodeWallpaper(path), cancellationToken);
    }

    private static Stream? DecodeWallpaper(string path)
    {
        using var pool = new NSAutoreleasePool();
        using var url = NSUrl.FromFilename(path);
        using var source = CGImageSource.FromUrl(url);
        if (source == null || source.ImageCount == 0)
            return null;
        using var image = source.CreateThumbnail((int)source.GetPrimaryImageIndex(), new CGImageThumbnailOptions
        {
            MaxPixelSize = 640,
            CreateThumbnailFromImageAlways = true,
            CreateThumbnailWithTransform = true
        });
        if (image == null)
            return null;
        using var bitmap = new NSBitmapImageRep(image);
        using var png = bitmap.RepresentationUsingTypeProperties(NSBitmapImageFileType.Png);
        return png == null ? null : new MemoryStream(png.ToArray(), writable: false);
    }
}
