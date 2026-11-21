using System.Security.Cryptography;
using System.Text;
using Avalonia.Threading;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;
using Foundation;
using UserNotifications;

namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// 用系统本地通知显示共享代码发出的应用消息（崩溃报告、配置文件损坏、插件更新等）。
/// </summary>
internal sealed class IosDesktopToastService : IDesktopToastService
{
    private const string IdentifierPrefix = "classisland.toast.";
    private const string CategoryPrefix = "classisland.toast.category.";
    private const string ButtonActionPrefix = "classisland.toast.button.";
    // 与 Android 实现保持一致，iOS 横幅展开后最多也只适合展示少量按钮。
    private const int MaximumButtonCount = 3;
    // 回调只保存在内存中；用户未处理的旧通知不应无限占用回调表。
    private const int MaximumTrackedNotificationCount = 64;

    private readonly object _lock = new();
    private readonly Dictionary<Guid, ToastCallbacks> _callbacks = new();
    private readonly Queue<Guid> _callbackOrder = new();
    private readonly SemaphoreSlim _categoryLock = new(1, 1);
    private readonly HashSet<string> _registeredCategories = new(StringComparer.Ordinal);

    public async Task ShowToastAsync(DesktopToastContent content)
    {
        var center = UNUserNotificationCenter.Current;
        var settings = await center.GetNotificationSettingsAsync();
        // 与 Android 一致：只在已获授权时发送，不为应用消息主动弹出授权请求。
        if (settings.AuthorizationStatus is not
            (UNAuthorizationStatus.Authorized or UNAuthorizationStatus.Provisional))
        {
            return;
        }

        var buttons = content.Buttons.Take(MaximumButtonCount).ToList();
        var categoryIdentifier = await EnsureCategoryAsync(buttons.Select(x => x.Key).ToList());
        var notificationId = Guid.NewGuid();
        TrackCallbacks(notificationId, new ToastCallbacks(
            () => content.Activated?.Invoke(this, EventArgs.Empty),
            buttons.Select(x => x.Value).ToList()));

        using var notificationContent = new UNMutableNotificationContent();
        notificationContent.Title = content.Title;
        notificationContent.Body = content.Body;
        notificationContent.Sound = UNNotificationSound.Default;
        notificationContent.CategoryIdentifier = categoryIdentifier;
        using var request = UNNotificationRequest.FromIdentifier(
            IdentifierPrefix + notificationId.ToString("N"),
            notificationContent,
            null);
        try
        {
            await center.AddNotificationRequestAsync(request);
        }
        catch
        {
            RemoveCallbacks(notificationId);
            throw;
        }
    }

    public Task ShowToastAsync(string title, string body, Action? activated = null) =>
        ShowToastAsync(new DesktopToastContent
        {
            Title = title,
            Body = body,
            Activated = (_, _) => activated?.Invoke()
        });

    public void ActivateNotificationAction(Guid id)
    {
        // iOS 的通知响应由 UNUserNotificationCenterDelegate 直接回调 HandleResponse，不经过激活 id。
    }

    /// <summary>
    /// 处理属于应用消息的通知响应。
    /// </summary>
    /// <returns>响应是否属于本服务发出的通知。</returns>
    public bool HandleResponse(UNNotificationResponse response)
    {
        var identifier = response.Notification.Request.Identifier;
        if (!identifier.StartsWith(IdentifierPrefix, StringComparison.Ordinal) ||
            !Guid.TryParseExact(identifier[IdentifierPrefix.Length..], "N", out var notificationId))
        {
            return false;
        }

        var callbacks = RemoveCallbacks(notificationId);
        if (callbacks == null || response.IsDismissAction)
        {
            return true;
        }

        Action? callback = null;
        var actionIdentifier = response.ActionIdentifier.ToString();
        if (response.IsDefaultAction)
        {
            callback = callbacks.Activated;
        }
        else if (actionIdentifier.StartsWith(ButtonActionPrefix, StringComparison.Ordinal) &&
                 int.TryParse(actionIdentifier[ButtonActionPrefix.Length..], out var index) &&
                 index >= 0 && index < callbacks.Buttons.Count)
        {
            callback = callbacks.Buttons[index];
        }

        if (callback != null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    callback();
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"处理 iOS 应用消息操作时发生异常：{exception}");
                }
            });
        }

        return true;
    }

    private async Task<string> EnsureCategoryAsync(IReadOnlyList<string> buttonTitles)
    {
        // iOS 通知按钮标题属于类别而非单条通知，因此按按钮标题组合生成并按需追加类别。
        var categoryIdentifier = CategoryPrefix + Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', buttonTitles))))[..16];
        await _categoryLock.WaitAsync();
        try
        {
            if (_registeredCategories.Contains(categoryIdentifier))
            {
                return categoryIdentifier;
            }

            var center = UNUserNotificationCenter.Current;
            var existing = await center.GetNotificationCategoriesAsync();
            var actions = buttonTitles
                .Select((title, index) => UNNotificationAction.FromIdentifier(
                    ButtonActionPrefix + index,
                    title,
                    UNNotificationActionOptions.Foreground))
                .ToArray();
            // CustomDismissAction 让用户划掉通知时也能收到响应，从而释放对应回调。
            var category = UNNotificationCategory.FromIdentifier(
                categoryIdentifier,
                actions,
                [],
                UNNotificationCategoryOptions.CustomDismissAction);
            // SetNotificationCategories 会整体替换，必须保留课程提醒等已注册的类别。
            var categories = (existing?.ToArray() ?? [])
                .Where(x => x.Identifier != categoryIdentifier)
                .Append(category)
                .ToArray();
            using var categorySet = new NSSet<UNNotificationCategory>(categories);
            center.SetNotificationCategories(categorySet);
            _registeredCategories.Add(categoryIdentifier);
            return categoryIdentifier;
        }
        finally
        {
            _categoryLock.Release();
        }
    }

    private void TrackCallbacks(Guid notificationId, ToastCallbacks callbacks)
    {
        lock (_lock)
        {
            _callbacks[notificationId] = callbacks;
            _callbackOrder.Enqueue(notificationId);
            while (_callbackOrder.Count > MaximumTrackedNotificationCount)
            {
                _callbacks.Remove(_callbackOrder.Dequeue());
            }
        }
    }

    private ToastCallbacks? RemoveCallbacks(Guid notificationId)
    {
        lock (_lock)
        {
            // 队列中的残留 id 会在超出上限时被出队，Remove 对不存在的键是安全的。
            return _callbacks.Remove(notificationId, out var callbacks) ? callbacks : null;
        }
    }

    private sealed record ToastCallbacks(Action Activated, IReadOnlyList<Action> Buttons);
}
