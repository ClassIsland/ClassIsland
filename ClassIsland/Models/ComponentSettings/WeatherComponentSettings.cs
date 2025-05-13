using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.ComponentSettings;

public class WeatherComponentSettings : ObservableRecipient
{
    private bool _showAlerts = true;

    public bool ShowAlerts
    {
        get => _showAlerts;
        set
        {
            if (value == _showAlerts) return;
            _showAlerts = value;
            OnPropertyChanged();
        }
    }

    private int _alertsTitleShowMode = 1;
    private bool _showRainTime = true;
    private bool _showMainWeatherInfo = true;
    private int _mainWeatherInfoKind = 0;

    public int AlertsTitleShowMode
    {
        get => _alertsTitleShowMode;
        set
        {
            if (value == _alertsTitleShowMode) return;
            _alertsTitleShowMode = value;
            OnPropertyChanged();
        }
    }

    public bool ShowRainTime
    {
        get => _showRainTime;
        set
        {
            if (value == _showRainTime) return;
            _showRainTime = value;
            OnPropertyChanged();
        }
    }

    public bool ShowMainWeatherInfo
    {
        get => _showMainWeatherInfo;
        set
        {
            if (value == _showMainWeatherInfo) return;
            _showMainWeatherInfo = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 0 - 当前天气+气温 <br/>
    /// 1 - 湿度 <br/>
    /// 2 - 风力 <br/>
    /// 3 - AQI <br/>
    /// 4 - 气压 <br/>
    /// 5 - 体感温度 <br/>
    /// </summary>
    public int MainWeatherInfoKind
    {
        get => _mainWeatherInfoKind;
        set
        {
            if (value == _mainWeatherInfoKind) return;
            _mainWeatherInfoKind = value;
            OnPropertyChanged();
        }
    }
}