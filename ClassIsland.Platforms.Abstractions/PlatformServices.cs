using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Platforms.Abstraction.Stubs.Services;

namespace ClassIsland.Platforms.Abstraction;

/// <summary>
/// 用于获取各个平台服务的基础类。
/// </summary>
public static class PlatformServices
{
    /// <summary>
    /// 平台窗口服务
    /// </summary>
    public static IWindowPlatformService WindowPlatformService { get; internal set; } = new WindowPlatformServiceStub();

    /// <summary>
    /// 平台定位服务
    /// </summary>
    public static ILocationService LocationService { get; internal set; } = new LocationServiceStub();
    
    /// <summary>
    /// 平台桌面服务
    /// </summary>
    public static IDesktopService DesktopService { get; internal set; } = new DesktopServiceStub();

    /// <summary>
    /// 当前平台是否支持定位服务
    /// </summary>
    public static bool IsLocationSupported => LocationService is not LocationServiceStub;

    /// <summary>
    /// 系统事件注册服务
    /// </summary>
    public static ISystemEventsService SystemEventsService { get; internal set; } = new SystemEventsServiceStub();

    /// <summary>
    /// 桌面通知服务
    /// </summary>
    public static IDesktopToastService DesktopToastService { get; internal set; } = new DesktopToastServiceStub();
}