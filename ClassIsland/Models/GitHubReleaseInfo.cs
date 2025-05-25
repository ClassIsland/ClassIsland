using System.Collections.Generic;
using System.Text.Json.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class GitHubReleaseInfo : ObservableRecipient
{
    private string _id = "";
    private string _name = "";
    private bool _isPreRelease = false;
    private List<GitHubReleaseAssetInfo> _assets = new();
    private string _body = "";

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

    [JsonPropertyName("prerelease")]
    public bool IsPreRelease
    {
        get => _isPreRelease;
        set
        {
            if (value == _isPreRelease) return;
            _isPreRelease = value;
            OnPropertyChanged();
        }
    }

    [JsonPropertyName("body")]
    public string Body
    {
        get => _body;
        set
        {
            if (value == _body) return;
            _body = value;
            OnPropertyChanged();
        }
    }

    [JsonPropertyName("assets")]
    public List<GitHubReleaseAssetInfo> Assets
    {
        get => _assets;
        set
        {
            if (Equals(value, _assets)) return;
            _assets = value;
            OnPropertyChanged();
        }
    }
}