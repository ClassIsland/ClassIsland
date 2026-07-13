using System.Globalization;
using Foundation;
using UserNotifications;

namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// 仅管理 ClassIsland 课程提醒前缀下的 iOS 本地通知。
/// </summary>
internal sealed class IosLessonNotificationScheduler
{
    private const string IdentifierPrefix = "classisland.lessons.";
    internal const string CategoryIdentifier = "classisland.lessons";
    private const string CatchUpHistoryKey = "classisland.lessons.catch-up-history";

    private readonly HashSet<string> _catchUpHistory = LoadCatchUpHistory();

    public async Task SynchronizeAsync(
        IReadOnlyCollection<IosLessonNotificationRequest> requests,
        CancellationToken cancellationToken)
    {
        var notificationCenter = UNUserNotificationCenter.Current;
        var pending = await notificationCenter.GetPendingNotificationRequestsAsync() ?? [];
        cancellationToken.ThrowIfCancellationRequested();

        var requestedIdentifiers = requests
            .Select(x => x.Identifier)
            .ToHashSet(StringComparer.Ordinal);
        var pendingIdentifiers = pending
            .Select(x => x.Identifier)
            .ToHashSet(StringComparer.Ordinal);
        var deliveredIdentifiers = requests.Any(x => x.IsCatchUp)
            ? (await notificationCenter.GetDeliveredNotificationsAsync() ?? [])
                .Select(x => x.Request.Identifier)
                .ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);
        cancellationToken.ThrowIfCancellationRequested();
        var obsoleteIdentifiers = pending
            .Where(x => x.Identifier.StartsWith(IdentifierPrefix, StringComparison.Ordinal) &&
                        !requestedIdentifiers.Contains(x.Identifier))
            .Select(x => x.Identifier)
            .ToArray();
        if (obsoleteIdentifiers.Length > 0)
        {
            notificationCenter.RemovePendingNotificationRequests(obsoleteIdentifiers);
        }

        foreach (var request in requests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (request.IsCatchUp &&
                (_catchUpHistory.Contains(request.Identifier) ||
                 pendingIdentifiers.Contains(request.Identifier) ||
                 deliveredIdentifiers.Contains(request.Identifier)))
            {
                continue;
            }

            if (request.FireAt <= DateTimeOffset.Now.AddSeconds(1))
            {
                continue;
            }

            var localFireAt = request.FireAt.LocalDateTime;
            using var dateComponents = new NSDateComponents
            {
                Year = localFireAt.Year,
                Month = localFireAt.Month,
                Day = localFireAt.Day,
                Hour = localFireAt.Hour,
                Minute = localFireAt.Minute,
                Second = localFireAt.Second,
                TimeZone = NSTimeZone.LocalTimeZone
            };
            using var trigger = UNCalendarNotificationTrigger.CreateTrigger(
                dateComponents,
                false);
            using var content = new UNMutableNotificationContent
            {
                Title = request.Title,
                Body = request.Body,
                CategoryIdentifier = CategoryIdentifier,
                ThreadIdentifier = CategoryIdentifier
            };
            if (request.PlaySound)
            {
                content.Sound = UNNotificationSound.Default;
            }

            using var nativeRequest = UNNotificationRequest.FromIdentifier(
                request.Identifier,
                content,
                trigger);
            await notificationCenter.AddNotificationRequestAsync(nativeRequest);
            if (request.IsCatchUp && _catchUpHistory.Add(request.Identifier))
            {
                SaveCatchUpHistory();
            }
        }
    }

    private static HashSet<string> LoadCatchUpHistory()
    {
        var raw = NSUserDefaults.StandardUserDefaults.StringForKey(CatchUpHistoryKey);
        return string.IsNullOrWhiteSpace(raw)
            ? new HashSet<string>(StringComparer.Ordinal)
            : raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(IsRecentCatchUpIdentifier)
                .ToHashSet(StringComparer.Ordinal);
    }

    private void SaveCatchUpHistory()
    {
        _catchUpHistory.RemoveWhere(x => !IsRecentCatchUpIdentifier(x));
        var value = string.Join('\n', _catchUpHistory.OrderBy(x => x, StringComparer.Ordinal));
        NSUserDefaults.StandardUserDefaults.SetString(value, CatchUpHistoryKey);
    }

    private static bool IsRecentCatchUpIdentifier(string identifier)
    {
        var parts = identifier.Split('.');
        return parts.Length >= 3 &&
               DateTime.TryParseExact(
                   parts[2],
                   "yyyyMMdd",
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out var date) &&
               date >= DateTime.Today.AddDays(-1);
    }
}
