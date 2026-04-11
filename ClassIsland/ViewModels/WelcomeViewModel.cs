using System;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models;
using ClassIsland.Models.Refreshing;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class WelcomeViewModel(SettingsService settingsService, ITutorialService tutorialService) : ObservableRecipient
{
    public SettingsService SettingsService { get; } = settingsService;
    public ITutorialService TutorialService { get; } = tutorialService;
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
    [ObservableProperty] private bool _canClose = false;
    [ObservableProperty] private bool _isWizardCompleted = false;
    [ObservableProperty] private RefreshingScopes _refreshingScopes = new();
    [ObservableProperty] private bool _isRefreshingInProgress = false;
    [ObservableProperty] private bool _isOnboarding = false;
    [ObservableProperty] private bool _isManuallyRestarted = false;
}