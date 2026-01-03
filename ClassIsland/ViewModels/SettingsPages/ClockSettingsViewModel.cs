using System;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class ClockSettingsViewModel(
    SettingsService settingsService,
    IExactTimeService exactTimeService,
    ILessonsService lessonsService) : ObservableObject
{
    public SettingsService SettingsService { get; } = settingsService;
    public IExactTimeService ExactTimeService { get; } = exactTimeService;
    public ILessonsService LessonsService { get; } = lessonsService;

    [ObservableProperty] private DateTime _currentTime = DateTime.Now;
}