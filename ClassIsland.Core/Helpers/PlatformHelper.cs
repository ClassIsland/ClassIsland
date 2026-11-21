namespace ClassIsland.Core.Helpers;

/// <summary>
/// 集中描述 ClassIsland 当前运行平台的形态。
/// </summary>
public static class PlatformHelper
{
    /// <summary>
    /// 当前是否运行在 Android、iOS 或 iPadOS 移动设备上。
    /// </summary>
    public static bool IsMobile => OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    /// <summary>
    /// 当前是否运行在 iOS 或 iPadOS 上。
    /// </summary>
    public static bool IsAppleMobile => OperatingSystem.IsIOS();
}
