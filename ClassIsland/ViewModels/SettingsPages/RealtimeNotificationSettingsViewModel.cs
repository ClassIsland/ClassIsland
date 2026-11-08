using System;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class RealtimeNotificationSettingsViewModel : ObservableRecipient
{
    private bool _isActivated;
    private INotificationPermissionService? _notificationPermissionService;

    public bool IsNotificationPermissionMissing =>
        _notificationPermissionService?.IsNotificationPermissionGranted == false;

    public void Activate()
    {
        if (_isActivated)
        {
            return;
        }

        _isActivated = true;
        _notificationPermissionService = PlatformServices.DesktopToastService as INotificationPermissionService;
        if (_notificationPermissionService != null)
        {
            _notificationPermissionService.NotificationPermissionChanged += NotificationPermissionOnChanged;
        }
        OnPropertyChanged(nameof(IsNotificationPermissionMissing));
    }

    public void Deactivate()
    {
        if (_notificationPermissionService != null)
        {
            _notificationPermissionService.NotificationPermissionChanged -= NotificationPermissionOnChanged;
        }
        _notificationPermissionService = null;
        _isActivated = false;
    }

    private void NotificationPermissionOnChanged(object? sender, EventArgs e) =>
        OnPropertyChanged(nameof(IsNotificationPermissionMissing));

    [RelayCommand]
    private void OpenNotificationPermissionSettings() =>
        _notificationPermissionService?.OpenNotificationPermissionSettings();
}
