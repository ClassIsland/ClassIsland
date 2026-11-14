using Foundation;
using UserNotifications;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 请求并检查 iOS/iPadOS 通知授权。
/// </summary>
internal sealed class IosNotificationAuthorizationService
{
    private readonly object _requestLock = new();
    private Task<bool>? _requestTask;

    /// <summary>
    /// 在授权状态尚未确定时请求提醒和声音权限。
    /// </summary>
    /// <returns>当前是否已获得通知授权。</returns>
    public Task<bool> RequestAuthorizationIfNeededAsync()
    {
        lock (_requestLock)
        {
            if (_requestTask != null)
            {
                return _requestTask;
            }

            var requestTask = RequestAuthorizationCoreAsync();
            _requestTask = requestTask;
            _ = ResetRequestTaskWhenCompletedAsync(requestTask);
            return requestTask;
        }
    }

    private async Task ResetRequestTaskWhenCompletedAsync(Task<bool> requestTask)
    {
        try
        {
            await requestTask;
        }
        catch
        {
            // 原始任务由调用方观察；这里只负责在完成后允许下次重新检查授权状态。
        }
        finally
        {
            lock (_requestLock)
            {
                if (ReferenceEquals(_requestTask, requestTask))
                {
                    _requestTask = null;
                }
            }
        }
    }

    private static async Task<bool> RequestAuthorizationCoreAsync()
    {
        var notificationCenter = UNUserNotificationCenter.Current;
        var settings = await notificationCenter.GetNotificationSettingsAsync();
        if (settings.AuthorizationStatus != UNAuthorizationStatus.NotDetermined)
        {
            return IsAuthorized(settings.AuthorizationStatus);
        }

        var result = await notificationCenter.RequestAuthorizationAsync(
            UNAuthorizationOptions.Alert |
            UNAuthorizationOptions.Sound);
        if (result.Item2 is not null)
        {
            throw new NSErrorException(result.Item2);
        }

        return result.Item1;
    }

    private static bool IsAuthorized(UNAuthorizationStatus status) =>
        status is UNAuthorizationStatus.Authorized or UNAuthorizationStatus.Provisional;
}
