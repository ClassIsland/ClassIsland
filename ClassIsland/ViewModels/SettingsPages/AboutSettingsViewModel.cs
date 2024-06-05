using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class AboutSettingsViewModel : ObservableRecipient
{
    private int _appIconClickCount = 0;
    private string _diagnosticInfo = "";
    private bool _isRefreshingContributors;
    private string _license = "";

    public int AppIconClickCount
    {
        get => _appIconClickCount;
        set
        {
            if (value == _appIconClickCount) return;
            _appIconClickCount = value;
            OnPropertyChanged();
        }
    }

    public string DiagnosticInfo
    {
        get => _diagnosticInfo;
        set
        {
            if (value == _diagnosticInfo) return;
            _diagnosticInfo = value;
            OnPropertyChanged();
        }
    }

    public bool IsRefreshingContributors
    {
        get => _isRefreshingContributors;
        set
        {
            if (value == _isRefreshingContributors) return;
            _isRefreshingContributors = value;
            OnPropertyChanged();
        }
    }

    public string License
    {
        get => _license;
        set
        {
            if (value == _license) return;
            _license = value;
            OnPropertyChanged();
        }
    }
}