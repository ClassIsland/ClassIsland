using System.Runtime.Versioning;

namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 电源操作服务，用于自动化行动
/// </summary>
public interface IPowerOptionsService
{
    /// <summary>
    /// 执行关机操作
    /// </summary>
    public void Shutdown();

    /// <summary>
    /// 执行重启操作
    /// </summary>
    public void Reboot();

    /// <summary>
    /// 执行注销（登出当前用户）操作
    /// </summary>
    [UnsupportedOSPlatform("linux")]
    public void Logout();

    /// <summary>
    /// 执行休眠操作 (Hibernation)
    /// <remarks>在macOS平台上，此操作与<see cref="Sleep"/>等同</remarks>
    /// </summary>
    public void Hibernate();

    /// <summary>
    /// 执行睡眠操作 (Sleep/Suspend)
    /// <remarks>在macOS平台上，此操作与<see cref="Hibernate"/>等同</remarks>
    /// </summary>
    public void Sleep();
}