using System.Text.Json.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class GitHubReleaseAssetInfo : ObservableRecipient
{
    private string _name = "";
    private string _url = "";

    [JsonPropertyName("name")]
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

    [JsonPropertyName("browser_download_url")]
    public string Url
    {
        get => _url;
        set
        {
            if (value == _url) return;
            _url = value;
            OnPropertyChanged();
        }
    }
}