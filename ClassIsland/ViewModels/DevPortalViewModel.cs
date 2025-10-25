using System;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class DevPortalViewModel(
    INotificationHostService notificationHostService,
    SettingsService settingsService,
    IExactTimeService exactTimeService,
    IWeatherService weatherService) : ObservableObject
{
    public INotificationHostService NotificationHostService { get; } = notificationHostService;
    public SettingsService SettingsService { get; } = settingsService;
    public IExactTimeService ExactTimeService { get; } = exactTimeService;
    public IWeatherService WeatherService { get; } = weatherService;

    [ObservableProperty] private string _notificationMaskText = "";
    
    [ObservableProperty] private string _notificationOverlayText = "";

    [ObservableProperty] private DateTime _targetDate = DateTime.Now;
    
    [ObservableProperty] private TimeSpan _targetTime = TimeSpan.Zero;

    [ObservableProperty] private bool _isTargetDateLoaded = false;

    [ObservableProperty] private bool _isTargetTimeLoaded = false;

    [ObservableProperty] private string _toastTitle = "";
    [ObservableProperty] private string _toastMessage = "";
    [ObservableProperty] private bool _toastHaveActions = false;
    [ObservableProperty] private bool _toastCanUserClose = true;
    [ObservableProperty] private object? _oobeIntroControlContent = new Border();
    [ObservableProperty] private string _markdownText =
        """
        # Welcome to ClassIsland!
        
        
        """;

    public bool IsTargetDateTimeLoaded => IsTargetDateLoaded && IsTargetTimeLoaded;
}