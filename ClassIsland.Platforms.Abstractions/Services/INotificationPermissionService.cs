namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 提供系统通知权限检测与授权设置入口。
/// </summary>
public interface INotificationPermissionService
{
    /// <summary>
    /// 系统是否允许此应用发送通知。
    /// </summary>
    bool IsNotificationPermissionGranted { get; }

    /// <summary>
    /// 通知权限可能发生变化时触发。
    /// </summary>
    event EventHandler? NotificationPermissionChanged;

    /// <summary>
    /// 打开此应用的系统通知设置。
    /// </summary>
    void OpenNotificationPermissionSettings();
}
