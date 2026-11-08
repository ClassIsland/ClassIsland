using System.Globalization;
using ClassIsland.Platforms.Abstraction.Services;
using Foundation;
using UserNotifications;

namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// 将课程通知操作接到系统通知中心，稍后提醒与常规排程共享修改锁。
/// </summary>
internal static class IosNotificationActions
{
    private const string SnoozableCategoryIdentifier = "classisland.lessons.snoozable";
    private const string ViewActionIdentifier = "classisland.lessons.view";
    private const string SnoozeActionIdentifier = "classisland.lessons.snooze";
    private const string ScheduleDateKey = "classisland.schedule-date";
    private const string ExpiresAtKey = "classisland.action-expires-at";
    private const string SnoozedKey = "classisland.snoozed";
    private const string SchedulingEnabledKey = "classisland.lessons.actions-enabled";

    public static void RegisterCategories()
    {
        using var view = UNNotificationAction.FromIdentifier(
            ViewActionIdentifier,
            "查看课表",
            UNNotificationActionOptions.Foreground);
        using var snooze = UNNotificationAction.FromIdentifier(
            SnoozeActionIdentifier,
            "5 分钟后提醒",
            UNNotificationActionOptions.None);
        using var lessonCategory = UNNotificationCategory.FromIdentifier(
            IosLessonNotificationScheduler.CategoryIdentifier,
            [view],
            [],
            UNNotificationCategoryOptions.None);
        using var snoozableCategory = UNNotificationCategory.FromIdentifier(
            SnoozableCategoryIdentifier,
            [view, snooze],
            [],
            UNNotificationCategoryOptions.None);
        using var categories = new NSSet<UNNotificationCategory>(
            lessonCategory,
            snoozableCategory);
        UNUserNotificationCenter.Current.SetNotificationCategories(categories);
    }

    public static void ConfigureContent(
        UNMutableNotificationContent content,
        IosLessonNotificationRequest request)
    {
        content.CategoryIdentifier = GetCategoryIdentifier(request);
        using var dateKey = new NSString(ScheduleDateKey);
        using var expiresKey = new NSString(ExpiresAtKey);
        using var date = new NSString(request.ScheduleDate?.ToString(
            "yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "");
        using var expiresAt = new NSString(request.ActionExpiresAt?
            .ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) ?? "");
        using var userInfo = NSDictionary.FromObjectsAndKeys(
            [date, expiresAt],
            [dateKey, expiresKey]);
        content.UserInfo = userInfo;
    }

    public static bool ContentMatches(
        UNNotificationContent content,
        IosLessonNotificationRequest request) =>
        string.Equals(content.CategoryIdentifier, GetCategoryIdentifier(request),
            StringComparison.Ordinal) &&
        string.Equals(ReadString(content.UserInfo, ScheduleDateKey),
            request.ScheduleDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "",
            StringComparison.Ordinal) &&
        string.Equals(ReadString(content.UserInfo, ExpiresAtKey),
            request.ActionExpiresAt?.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) ?? "",
            StringComparison.Ordinal);

    public static async Task SetSchedulingEnabledAsync(
        bool enabled,
        IosNotificationMutationGate mutationGate,
        CancellationToken cancellationToken)
    {
        NSUserDefaults.StandardUserDefaults.SetBool(enabled, SchedulingEnabledKey);
        if (enabled)
        {
            return;
        }

        await mutationGate.ExecuteAsync(async () =>
        {
            var center = UNUserNotificationCenter.Current;
            var pending = await center.GetPendingNotificationRequestsAsync() ?? [];
            var identifiers = pending
                .Where(x => x.Identifier.StartsWith(
                    IosNotificationActionPolicy.SnoozedIdentifierPrefix,
                    StringComparison.Ordinal))
                .Select(x => x.Identifier)
                .ToArray();
            if (identifiers.Length > 0)
            {
                center.RemovePendingNotificationRequests(identifiers);
            }
        }, cancellationToken);
    }

    public static async Task<Uri?> HandleResponseAsync(
        UNUserNotificationCenter center,
        UNNotificationResponse response,
        IosNotificationMutationGate mutationGate)
    {
        var request = response.Notification.Request;
        var content = request.Content;
        if (content.CategoryIdentifier is not
            (IosLessonNotificationScheduler.CategoryIdentifier or SnoozableCategoryIdentifier))
        {
            return null;
        }

        var action = response.ActionIdentifier;
        if (action == ViewActionIdentifier || response.IsDefaultAction)
        {
            return IosNotificationActionPolicy.CreateScheduleUri(
                ReadString(content.UserInfo, ScheduleDateKey));
        }

        if (action != SnoozeActionIdentifier ||
            content.CategoryIdentifier != SnoozableCategoryIdentifier)
        {
            return null;
        }

        await mutationGate.ExecuteAsync(async () =>
        {
            if (!NSUserDefaults.StandardUserDefaults.BoolForKey(SchedulingEnabledKey))
            {
                return;
            }

            var settings = await center.GetNotificationSettingsAsync();
            if (settings.AuthorizationStatus is not
                (UNAuthorizationStatus.Authorized or UNAuthorizationStatus.Provisional))
            {
                return;
            }

            var now = DateTimeOffset.Now;
            var fireAt = IosNotificationActionPolicy.GetSnoozeTime(
                now,
                ReadString(content.UserInfo, ExpiresAtKey),
                ReadString(content.UserInfo, SnoozedKey) == "true");
            var identifier = IosNotificationActionPolicy.GetSnoozeIdentifier(request.Identifier);
            if (fireAt == null || identifier == null)
            {
                return;
            }

            var pending = await center.GetPendingNotificationRequestsAsync() ?? [];
            if (IosNotificationCapacityPolicy.GetFallbackSubmissionDecision(
                    identifier,
                    pending.Select(x => x.Identifier),
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1)) != IosFallbackNotificationCapacityDecision.Submit)
            {
                return;
            }

            using var snoozedContent = (UNMutableNotificationContent)content.MutableCopy();
            snoozedContent.CategoryIdentifier = IosLessonNotificationScheduler.CategoryIdentifier;
            using var userInfo = (NSMutableDictionary)content.UserInfo.MutableCopy();
            using var snoozedKey = new NSString(SnoozedKey);
            using var snoozedValue = new NSString("true");
            userInfo[snoozedKey] = snoozedValue;
            snoozedContent.UserInfo = userInfo;
            if (IosNotificationActionPolicy.GetSnoozeTime(
                    DateTimeOffset.Now,
                    ReadString(content.UserInfo, ExpiresAtKey),
                    false) == null ||
                !NSUserDefaults.StandardUserDefaults.BoolForKey(SchedulingEnabledKey))
            {
                return;
            }
            using var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(
                IosNotificationActionPolicy.SnoozeInterval.TotalSeconds,
                false);
            using var snoozedRequest = UNNotificationRequest.FromIdentifier(
                identifier,
                snoozedContent,
                trigger);
            await center.AddNotificationRequestAsync(snoozedRequest);
        }, CancellationToken.None);
        return null;
    }

    private static string GetCategoryIdentifier(IosLessonNotificationRequest request) =>
        IosNotificationActionPolicy.GetSnoozeTime(
            request.FireAt,
            request.ActionExpiresAt?.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            false) != null
            ? SnoozableCategoryIdentifier
            : IosLessonNotificationScheduler.CategoryIdentifier;

    private static string? ReadString(NSDictionary values, string key)
    {
        using var nativeKey = new NSString(key);
        return values[nativeKey] is NSString value ? value.ToString() : null;
    }
}
