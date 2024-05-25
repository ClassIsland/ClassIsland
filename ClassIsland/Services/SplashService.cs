using System;

using ClassIsland.Services.Management;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Services;

public class SplashService: ObservableRecipient
{
    private string _splashStatus = "正在启动…";
    private double _currentProgress = 0.0;

    public string SplashStatus
    {
        get => _splashStatus;
        set
        {
            if (value == _splashStatus) return;
            _splashStatus = value;
            OnPropertyChanged();
        }
    }

    public double CurrentProgress
    {
        get => _currentProgress;
        set
        {
            if (value.Equals(_currentProgress)) return;
            _currentProgress = value;
            OnPropertyChanged();
            ProgressChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<double>? ProgressChanged;

    public event EventHandler? SplashEnded;
    public void EndSplash()
    {
        if (!SettingsService.Settings.IsSplashEnabled)
            return;
        SplashEnded?.Invoke(this, EventArgs.Empty);
    }

    private SettingsService SettingsService { get; }

    private static string DefaultText { get; } = "正在启动…";

    public SplashService(SettingsService settingsService, ManagementService managementService)
    {
        SettingsService = settingsService;
        if (managementService.Policy.DisableSplashCustomize)
        {
            SettingsService.Settings.SplashCustomLogoSource = "";
            SettingsService.Settings.SplashCustomText = "";
        }
        ResetSplashText();
    }

    public void ResetSplashText()
    {
        SplashStatus = SettingsService.Settings.SplashCustomText == "" ? DefaultText : SettingsService.Settings.SplashCustomText;
    }       
}