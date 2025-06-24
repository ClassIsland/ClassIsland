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
}