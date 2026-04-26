using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Abstractions.Services.Metadata;
using ClassIsland.Core.Services;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class GeneralSettingsViewModel(
    SettingsService settingsService,
    IManagementService managementService,
    IExactTimeService exactTimeService,
    ISplashService splashService,
    IAnnouncementService announcementService) : ObservableRecipient
{
    public SettingsService SettingsService { get; } = settingsService;
    public IManagementService ManagementService { get; } = managementService;
    public IExactTimeService ExactTimeService { get; } = exactTimeService;
    public ISplashService SplashService { get; } = splashService;
    public IAnnouncementService AnnouncementService { get; } = announcementService;
    private bool _isWeekOffsetSettingsOpen = false;
    private bool _isSplashPreviewing = false;

    public bool IsWeekOffsetSettingsOpen
    {
        get => _isWeekOffsetSettingsOpen;
        set
        {
            if (value == _isWeekOffsetSettingsOpen) return;
            _isWeekOffsetSettingsOpen = value;
            OnPropertyChanged();
        }
    }

    public int CriticalSafeModeSelectedIndex
    {
        get
        {
            return SettingsService.Settings.IsCriticalSafeMode
                ? SettingsService.Settings.CriticalSafeModeMethod + 1
                : 0;
        }
        set
        {
            if (value == 0)
            {
                SettingsService.Settings.IsCriticalSafeMode = false;
            }
            else
            {
                SettingsService.Settings.IsCriticalSafeMode = true;
                SettingsService.Settings.CriticalSafeModeMethod = value - 1;
            }
        }
    }

    public bool IsSplashPreviewing
    {
        get => _isSplashPreviewing;
        set
        {
            if (value == _isSplashPreviewing) return;
            _isSplashPreviewing = value;
            OnPropertyChanged();
        }
    }
}