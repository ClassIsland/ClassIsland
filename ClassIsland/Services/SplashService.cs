using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Services;

public class SplashService: ObservableRecipient
{
    private string _splashStatus = "正在启动…";

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

    private SettingsService SettingsService { get; }

    private static string DefaultText { get; } = "正在启动…";

    public SplashService(SettingsService settingsService)
    {
        SettingsService = settingsService;
        ResetSplashText();
    }

    public void ResetSplashText()
    {
        SplashStatus = SettingsService.Settings.SplashCustomText == "" ? DefaultText : SettingsService.Settings.SplashCustomText;
    }       
}