using Avalonia;

namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>Windows 窗口截图服务。</summary>
public interface IScreenshotService
{
    /// <summary>获取指定窗口的 PNG 截图。窗口无法采集时返回空值。</summary>
    Task<ReadOnlyMemory<byte>?> CaptureWindowAsync(nint hwnd, CancellationToken cancellationToken = default);

    /// <summary>枚举可见窗口及其子窗口，可限定为覆盖至少一台显示器的窗口。</summary>
    IAsyncEnumerable<WindowScreenshot> EnumerateWindowsAsync(bool fullScreenOnly = true,
        CancellationToken cancellationToken = default);
}

/// <summary>可见窗口的资料及可选 PNG 缩略图。</summary>
public sealed record WindowScreenshot(nint Handle, string Title, string ClassName, PixelRect Bounds,
    ReadOnlyMemory<byte> ThumbnailPng);
