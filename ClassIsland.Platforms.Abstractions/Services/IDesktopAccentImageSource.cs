using Avalonia;

namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>为桌面主题色服务提供平台图像。返回的图像流由调用方释放。</summary>
public interface IDesktopAccentImageSource
{
    /// <summary>是否支持读取桌面壁纸。</summary>
    bool SupportsWallpaper { get; }

    /// <summary>是否支持屏幕截图。</summary>
    bool SupportsScreen { get; }

    /// <summary>系统报告桌面壁纸发生变更。</summary>
    event Action? WallpaperChanged
    {
        add { }
        remove { }
    }

    /// <summary>获取壁纸。类名及兼容模式选项用于 Windows。</summary>
    ValueTask<Stream?> CaptureWallpaperAsync(string windowClassName, bool compatibilityMode,
        CancellationToken cancellationToken);

    /// <summary>获取指定像素区域的屏幕图像。</summary>
    Stream? CaptureScreen(PixelRect bounds, CancellationToken cancellationToken) => null;
}
