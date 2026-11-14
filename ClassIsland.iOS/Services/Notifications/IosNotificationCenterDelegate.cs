using UserNotifications;

namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// 让本地通知在 Avalonia 移动界面位于前台时也能正常显示。
/// </summary>
internal sealed class IosNotificationCenterDelegate : UNUserNotificationCenterDelegate
{
    public event Action<Uri>? NavigationRequested;

    internal IosNotificationMutationGate NotificationMutationGate { get; } = new();

    public void RegisterCategories() => IosNotificationActions.RegisterCategories();

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

    public override async void DidReceiveNotificationResponse(
        UNUserNotificationCenter center,
        UNNotificationResponse response,
        Action completionHandler)
    {
        try
        {
            var uri = await IosNotificationActions.HandleResponseAsync(
                center,
                response,
                NotificationMutationGate);
            if (uri != null)
            {
                NavigationRequested?.Invoke(uri);
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"处理 iOS 通知操作时发生异常：{exception}");
        }
        finally
        {
            completionHandler();
        }
    }
}
