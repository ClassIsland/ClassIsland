using System.ComponentModel;
using System.Runtime.Versioning;
using Android.App;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Services;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Android.Services.Platform;

internal sealed class AndroidAccentPickerService : IAccentPickerService
{
    private readonly AccentPickerSession _session;

    public AndroidAccentPickerService(SettingsService settingsService, ILogger<AccentPickerSession> logger)
    {
        _session = new AccentPickerSession(settingsService, logger, SupportsWallpaper, SupportsScreen, PickAsync);
        _session.PropertyChanged += (_, e) => PropertyChanged?.Invoke(this, e);
    }

    [SupportedOSPlatformGuard("android27.0")]
    public bool SupportsWallpaper => OperatingSystem.IsAndroidVersionAtLeast(27);
    public bool SupportsScreen => false;
    public bool SupportsPreview => false;
    public bool IsWorking => _session.IsWorking;
    public AccentPickResult? LastResult => _session.LastResult;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Task<AccentPickResult> RefreshAsync(CancellationToken cancellationToken = default) =>
        _session.RefreshAsync(cancellationToken);

    private Task<AccentPickResult> PickAsync(AccentCaptureTarget target, CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            if (!SupportsWallpaper)
                return new AccentPickResult(AccentPickStatus.Unavailable);

            var wallpaper = WallpaperManager.GetInstance(Application.Instance);
            using var colors = wallpaper?.GetWallpaperColors((int)WallpaperManagerFlags.System);
            if (colors?.PrimaryColor is not { } color)
                return new AccentPickResult(AccentPickStatus.Unavailable);

            var argb = (uint)color.ToArgb();
            return new AccentPickResult(AccentPickStatus.Success, Color.FromUInt32(argb));
        }, cancellationToken);
}
