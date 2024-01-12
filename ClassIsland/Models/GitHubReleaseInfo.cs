using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class GitHubReleaseInfo : ObservableRecipient
{
    private string _id = "";
    private string _name = "";

    [JsonPropertyName("id")]
    public string Id
    {
        get => _id;
        set
        {
            if (value == _id) return;
            _id = value;
            OnPropertyChanged();
        }
    }

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
}