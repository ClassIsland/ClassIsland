using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Updating;

public class DownloadMirror : ObservableRecipient
{
    private string _name = "";
    private List<string> _speedTestUrls = [];
    private SpeedTestResult _speedTestResult = new();

    public string Name
    {
        get => _name;
        set
        {
            if (value == _name) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    public List<string> SpeedTestUrls
    {
        get => _speedTestUrls;
        set
        {
            if (Equals(value, _speedTestUrls)) return;
            _speedTestUrls = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public SpeedTestResult SpeedTestResult
    {
        get => _speedTestResult;
        set
        {
            if (Equals(value, _speedTestResult)) return;
            _speedTestResult = value;
            OnPropertyChanged();
        }
    }
}