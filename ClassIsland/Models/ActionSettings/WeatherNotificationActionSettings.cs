using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.ActionSettings;

public partial class WeatherNotificationActionSettings : ObservableObject
{
    [ObservableProperty] private int _notificationKind = 0;
}