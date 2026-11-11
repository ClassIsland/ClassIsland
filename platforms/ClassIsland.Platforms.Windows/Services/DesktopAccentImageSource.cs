using Avalonia;
using ClassIsland.Platforms.Abstraction.Services;
using Microsoft.Win32;

namespace ClassIsland.Platform.Windows.Services;

public sealed class DesktopAccentImageSource : IDesktopAccentImageSource
{
    private Action? _wallpaperChanged;

    public bool SupportsWallpaper => true;
    public bool SupportsScreen => true;

    public event Action? WallpaperChanged
    {
        add
        {
            if (_wallpaperChanged == null)
                SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            _wallpaperChanged += value;
        }
        remove
        {
            _wallpaperChanged -= value;
            if (_wallpaperChanged == null)
                SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        }
    }

    public ValueTask<Stream?> CaptureWallpaperAsync(string windowClassName, bool compatibilityMode,
        CancellationToken cancellationToken) =>
        new(Task.Run<Stream?>(() =>
        {
            if (compatibilityMode)
            {
                using var desktop = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
                return desktop?.GetValue("WallPaper") is string { Length: > 0 } path && File.Exists(path)
                    ? File.OpenRead(path)
                    : null;
            }

            var className = string.IsNullOrWhiteSpace(windowClassName) ? "Progman" : windowClassName.Trim();
            var hwnd = ScreenshotService.FindWindowByClass(className, cancellationToken);
            return ScreenshotService.CaptureWindow(hwnd, 640) is { } png
                ? new MemoryStream(png, writable: false)
                : null;
        }, cancellationToken));

    public Stream? CaptureScreen(PixelRect bounds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ScreenshotService.CaptureScreen(bounds) is { } png ? new MemoryStream(png, writable: false) : null;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.Desktop)
            _wallpaperChanged?.Invoke();
    }
}
