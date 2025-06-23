using ClassIsland.Core.Abstractions.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class DevPortalViewModel(INotificationHostService notificationHostService) : ObservableObject
{
    public INotificationHostService NotificationHostService { get; } = notificationHostService;

    [ObservableProperty] private string _notificationMaskText = "";
    
    [ObservableProperty] private string _notificationOverlayText = "";
}