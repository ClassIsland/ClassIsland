using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Windows.Documents;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class AppCenterReleaseInfo : ObservableRecipient
{
    private string _version = "";
    private int _id = -1;
    private DateTime _uploadTime = DateTime.MinValue;
    private List<string> _packageHashes = new();
    private string _downloadUrl = "";

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

    [JsonPropertyName("package_hashes")]
    public List<string> PackageHashes
    {
        get => _packageHashes;
        set
        {
            if (Equals(value, _packageHashes)) return;
            _packageHashes = value;
            OnPropertyChanged();
        }
    }

    [JsonPropertyName("download_url")]
    public string DownloadUrl
    {
        get => _downloadUrl;
        set
        {
            if (value == _downloadUrl) return;
            _downloadUrl = value;
            OnPropertyChanged();
        }
    }
}