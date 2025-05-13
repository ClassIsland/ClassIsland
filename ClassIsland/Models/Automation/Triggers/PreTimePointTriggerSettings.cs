using ClassIsland.Core.Attributes;
using ClassIsland.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Models.Automation.Triggers;

public partial class PreTimePointTriggerSettings : ObservableObject
{
    [ObservableProperty] private TimeState _targetState = TimeState.OnClass;

    [ObservableProperty] private double _timeSeconds = 60;
}