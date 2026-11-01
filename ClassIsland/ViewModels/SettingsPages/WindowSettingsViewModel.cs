using System.Collections.ObjectModel;
using Avalonia.Platform;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class WindowSettingsViewModel(SettingsService settingsService) : ObservableRecipient
{
    public SettingsService SettingsService { get; } = settingsService;

    /// <summary>
    /// 系统级悬浮主界面服务。Android 上对应系统悬浮窗，iOS TrollStore 版对应 root HUD 子进程；
    /// 不支持的平台是 IsSupported 为 false 的桩实现。
    /// </summary>
    public IMainWindowOverlayService MainWindowOverlayService => PlatformServices.MainWindowOverlayService;

    private ObservableCollection<Screen> _screens = new();

    public ObservableCollection<Screen> Screens
    {
        get => _screens;
        set
        {
            if (Equals(value, _screens)) return;
            _screens = value;
            OnPropertyChanged();
        }
    }
}