using System.ComponentModel;
using Avalonia;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Helpers;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Services;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Desktop.Services.Platform;

internal sealed class DesktopAccentPickerService : IAccentPickerService
{
    private readonly IDesktopAccentImageSource _imageSource;
    private readonly AccentPickerSession _session;

    public DesktopAccentPickerService(IDesktopAccentImageSource imageSource, SettingsService settingsService,
        ILogger<AccentPickerSession> logger)
    {
        _imageSource = imageSource;
        _session = new AccentPickerSession(settingsService, logger, SupportsWallpaper, SupportsScreen, PickAsync);
        _session.PropertyChanged += (_, e) => PropertyChanged?.Invoke(this, e);
        _imageSource.WallpaperChanged += _session.OnWallpaperChanged;
        AppBase.Current.AppStopping += OnAppStopping;
    }

    public bool SupportsWallpaper => _imageSource.SupportsWallpaper;
    public bool SupportsScreen => _imageSource.SupportsScreen;
    public bool SupportsPreview => SupportsWallpaper || SupportsScreen;
    public bool IsWorking => _session.IsWorking;
    public AccentPickResult? LastResult => _session.LastResult;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Task<AccentPickResult> RefreshAsync(CancellationToken cancellationToken = default) =>
        _session.RefreshAsync(cancellationToken);

    private async Task<AccentPickResult> PickAsync(AccentCaptureTarget target, CancellationToken cancellationToken)
    {
        Stream? image;
        if (target.Source == 3)
        {
            var root = AppBase.Current;
            var screens = root.PhonyRootWindow?.Screens;
            var fallback = screens != null && target.MonitorIndex >= 0 && target.MonitorIndex < screens.ScreenCount
                ? screens.All[target.MonitorIndex]
                : screens?.Primary;
            var bounds = ((root.MainWindow as MainWindow)?.GetSelectedScreenSafe() ?? fallback)?.Bounds;
            if (bounds is not PixelRect area)
                return new AccentPickResult(AccentPickStatus.Unavailable);
            image = await Task.Run(() => _imageSource.CaptureScreen(area, cancellationToken), cancellationToken);
        }
        else
        {
            image = await _imageSource.CaptureWallpaperAsync(target.WindowClassName,
                target.CompatibilityMode, cancellationToken);
        }

        using (image)
        {
            if (image == null)
                return new AccentPickResult(AccentPickStatus.Failed);

            return await Task.Run(() => AccentColorHelper.Extract(image, cancellationToken), cancellationToken);
        }
    }

    private void OnAppStopping(object? sender, EventArgs e)
    {
        AppBase.Current.AppStopping -= OnAppStopping;
        _imageSource.WallpaperChanged -= _session.OnWallpaperChanged;
    }
}
