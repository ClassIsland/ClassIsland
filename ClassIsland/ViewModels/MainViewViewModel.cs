using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class MainViewViewModel(
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

    /// <summary>
    /// 系统级悬浮主界面服务。不支持该能力的平台返回桩实现（<see cref="IMainWindowOverlayService.IsSupported"/> 为 <see langword="false"/>）。
    /// </summary>
    public IMainWindowOverlayService MainWindowOverlayService => PlatformServices.MainWindowOverlayService;
}
