using System.ComponentModel;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Services;
using Microsoft.Extensions.Logging;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// iOS 主题色提取服务。
/// </summary>
/// <remarks>
/// iOS 没有向应用公开读取系统壁纸或截取其他应用画面的接口，因此壁纸取色和屏幕取色均不可用，
/// 仅保留共享会话以便界面设置页正常解析并显示不可用状态。
/// </remarks>
internal sealed class IosAccentPickerService : IAccentPickerService
{
    private readonly AccentPickerSession _session;

    public IosAccentPickerService(SettingsService settingsService, ILogger<AccentPickerSession> logger)
    {
        _session = new AccentPickerSession(settingsService, logger, SupportsWallpaper, SupportsScreen,
            (_, _) => Task.FromResult(new AccentPickResult(AccentPickStatus.Unavailable)));
        _session.PropertyChanged += (_, e) => PropertyChanged?.Invoke(this, e);
    }

    public bool SupportsWallpaper => false;
    public bool SupportsScreen => false;
    public bool SupportsPreview => false;
    public bool IsWorking => _session.IsWorking;
    public AccentPickResult? LastResult => _session.LastResult;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Task<AccentPickResult> RefreshAsync(CancellationToken cancellationToken = default) =>
        _session.RefreshAsync(cancellationToken);
}
