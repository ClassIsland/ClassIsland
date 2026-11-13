using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class InterfaceSettingsViewModel(SettingsService settingsService, IAccentPickerService accentPicker,
    IScreenshotService? screenshotService = null) : ObservableRecipient
{
    [ObservableProperty] private bool _isSelectingBackgroundImage;
    [ObservableProperty] private string _backgroundImageError = "";

    [ObservableProperty] private string? _accentError;

    public SettingsService SettingsService { get; } = settingsService;
    public IAccentPickerService AccentPicker { get; } = accentPicker;
    public IScreenshotService? ScreenshotService { get; } = screenshotService;
    public bool CanBrowseWallpaperWindows => ScreenshotService != null;

    public async Task RefreshAccentAsync(CancellationToken cancellationToken = default)
    {
        var settings = SettingsService.Settings;
        var source = settings.ColorSource;
        var windowClass = settings.WallpaperClassName;
        var compatibility = settings.IsFallbackModeEnabled;
        var monitor = settings.WindowDockingMonitorIndex;
        AccentError = null;
        var result = await AccentPicker.RefreshAsync(cancellationToken);
        if (settings != SettingsService.Settings || source != settings.ColorSource ||
            windowClass != settings.WallpaperClassName || compatibility != settings.IsFallbackModeEnabled ||
            monitor != settings.WindowDockingMonitorIndex)
            return;

        AccentError = result.Status switch
        {
            AccentPickStatus.Failed when source == 3 => "无法从屏幕提取主题色。请确认所选显示器已连接，再试一次。",
            AccentPickStatus.Failed when CanBrowseWallpaperWindows && !compatibility =>
                "无法从壁纸窗口提取主题色。请选择其他窗口，或开启兼容模式后重试。",
            AccentPickStatus.Failed => "无法从当前壁纸提取主题色。请更换壁纸后重试。",
            AccentPickStatus.Unavailable => "暂时无法获取主题色，请稍后重试。",
            _ => null
        };
    }
}
