namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 桌面相关服务。
/// </summary>
public interface IDesktopService
{
    /// <summary>
    /// 已启用开机自启。
    /// </summary>
    public bool IsAutoStartEnabled { get; set; }  
    
    /// <summary>
    /// 已注册 Url 协议。
    /// </summary>
    public bool IsUrlSchemeRegistered { get; set; }
}