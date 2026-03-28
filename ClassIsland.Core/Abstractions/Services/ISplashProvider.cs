namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 代表一个启动界面提供方。
/// </summary>
public interface ISplashProvider
{
    /// <summary>
    /// 开始显示启动界面。
    /// </summary>
    Task StartSplash();

    /// <summary>
    /// 停止显示启动界面。
    /// </summary>
    Task EndSplash();
}