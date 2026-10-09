using UserNotifications;

namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// 让本地通知在 Avalonia 移动界面位于前台时也能正常显示。
/// </summary>
internal sealed class IosNotificationCenterDelegate : UNUserNotificationCenterDelegate
{
    public override void WillPresentNotification(
        UNUserNotificationCenter center,
        UNNotification notification,
        Action<UNNotificationPresentationOptions> completionHandler)
    {
        var presentationOptions = OperatingSystem.IsIOSVersionAtLeast(14)
            ? UNNotificationPresentationOptions.Banner |
              UNNotificationPresentationOptions.List
            : UNNotificationPresentationOptions.Alert;
        if (notification.Request.Content.Sound != null)
        {
            presentationOptions |= UNNotificationPresentationOptions.Sound;
        }

        completionHandler(presentationOptions);
    }

    public override void DidReceiveNotificationResponse(
        UNUserNotificationCenter center,
        UNNotificationResponse response,
        Action completionHandler)
    {
        // iOS 会负责将应用带到前台；当前课程页面不需要额外的原生导航状态。
        completionHandler();
    }
}
