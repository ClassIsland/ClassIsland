using System;
using System.Text.Json.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class AppCenterReleaseInfoMin : ObservableRecipient
{
    private string _version = "";
    private int _id = -1;
    private DateTime _uploadTime = DateTime.MinValue;
    private bool _isLatest = false;

    [JsonPropertyName("version")]
    public string Version
    {
        get => _version;
        set
        {
            if (value == _version) return;
            _version = value;
            OnPropertyChanged();
        }
    }

    [JsonPropertyName("id")]
    public int Id
    {
        get => _id;
        set
        {
            if (value == _id) return;
            _id = value;
            OnPropertyChanged();
        }
    }

    [JsonPropertyName("uploaded_at")]
    public DateTime UploadTime
    {
        get => _uploadTime;
        set
        {
            if (value.Equals(_uploadTime)) return;
            _uploadTime = value;
            OnPropertyChanged();
        }
    }
}