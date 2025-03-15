using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Actions;

public partial class WeatherNotificationActionSettings : ObservableObject
{
    [ObservableProperty] private int _notificationKind = 0;
}