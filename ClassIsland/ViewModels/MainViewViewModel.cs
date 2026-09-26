using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class MainViewViewModel(
    IManagementService managementService,
    IUriNavigationService uriNavigationService,
    INotificationHostService notificationHostService,
    ILessonsService lessonsService,
    IProfileService profileService) : ObservableRecipient
{
    public IManagementService ManagementService { get; } = managementService;
    public IUriNavigationService UriNavigationService { get; } = uriNavigationService;
    public INotificationHostService NotificationHostService { get; } = notificationHostService;
    public ILessonsService LessonsService { get; } = lessonsService;
    public IProfileService ProfileService { get; } = profileService;
}
