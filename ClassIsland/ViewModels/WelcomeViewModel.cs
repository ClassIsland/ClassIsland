using System;

using ClassIsland.Models;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class WelcomeViewModel(SettingsService settingsService) : ObservableRecipient
{
    public SettingsService SettingsService { get; } = settingsService;
    [ObservableProperty] private int _slideIndex = 0;
    [ObservableProperty] private bool _isLicenseAgreed = false;
    [ObservableProperty] private string _license = "";
    [ObservableProperty] private bool _isExitConfirmed = false;
    [ObservableProperty] private int _masterTabIndex = 0;
    [ObservableProperty] private int _flipNextCount = 0;
    [ObservableProperty] private bool _isFlipEnd = false;
    [ObservableProperty] private int _flipIndex = 0;
    [ObservableProperty] private bool _createStartupShortcut = true;
    [ObservableProperty] private bool _createStartMenuShortcut = true;
    [ObservableProperty] private bool _createDesktopShortcut = false;
    [ObservableProperty] private int _slideIndexMaster = 0;
    [ObservableProperty] private bool _registerUrlScheme = true;
    [ObservableProperty] private bool _createClassSwapShortcut = false;
    [ObservableProperty] private bool _requiresRestarting = false;
    [ObservableProperty] private DateTime _singleWeekStartTime = DateTime.Now;
    [ObservableProperty] private Type? _currentPage;

}