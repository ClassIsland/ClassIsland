using System;
using ClassIsland.Enums.AppUpdating;
using CommunityToolkit.Mvvm.ComponentModel;
using PhainonDistributionCenter.Shared.Models.FileMap;

namespace ClassIsland.Models.AppUpdating;

public partial class DownloadTaskInfo : ObservableObject
{
    [ObservableProperty] private string _fileName = "";
    [ObservableProperty] private long _fileSize = 0;
    [ObservableProperty] private long _downloadedSize = 0;
    [ObservableProperty] private double _downloadSpeed = 0;
    [ObservableProperty] private TimeSpan _timeToComplete = TimeSpan.Zero;
    [ObservableProperty] private DownloadState _state = DownloadState.Pending;
    
    public required string Key { get; init; }

    public static DownloadTaskInfo CreateEmpty() => new DownloadTaskInfo()
    {
        Key = ""
    };
}