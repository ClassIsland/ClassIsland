using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class AboutSettingsViewModel : ObservableRecipient
{
    private int _appIconClickCount = 0;
    private string _diagnosticInfo = "";
    private bool _isRefreshingContributors;
    private string _license = "";
    private string _sayings = "点击此处可以查看 ClassIsland 用户群里沙雕群友们的发言";
    private ObservableCollection<string> _sayingsCollection = [];
    private bool _isSayingBusy = false;
    private int _clickCount = 0;
    private int _appInfoClickCount = 0;

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

    public string Sayings
    {
        get => _sayings;
        set
        {
            if (value == _sayings) return;
            _sayings = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> SayingsCollection
    {
        get => _sayingsCollection;
        set
        {
            if (Equals(value, _sayingsCollection)) return;
            _sayingsCollection = value;
            OnPropertyChanged();
        }
    }

    public Random Random { get; set; } = new();

    public bool IsSayingBusy
    {
        get => _isSayingBusy;
        set
        {
            if (value == _isSayingBusy) return;
            _isSayingBusy = value;
            OnPropertyChanged();
        }
    }

    public int ClickCount
    {
        get => _clickCount;
        set
        {
            if (value == _clickCount) return;
            _clickCount = value;
            OnPropertyChanged();
        }
    }

    public int AppInfoClickCount
    {
        get => _appInfoClickCount;
        set
        {
            if (value == _appInfoClickCount) return;
            _appInfoClickCount = value;
            OnPropertyChanged();
        }
    }
}