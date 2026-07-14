using System.Globalization;
using ClassIsland.Platforms.Abstraction.Services;
using Foundation;
using UserNotifications;

namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// 仅管理 ClassIsland 课程提醒前缀下的 iOS 本地通知。
/// </summary>
internal sealed class IosLessonNotificationScheduler(
    LessonPreparationNotificationTimeline lessonPreparationTimeline)
{
    private const string IdentifierPrefix = "classisland.lessons.";
    internal const string CategoryIdentifier = "classisland.lessons";
    // 保留旧 key 以迁移早期测试包只存 identifier 的格式。
    private const string PreparationHistoryKey = "classisland.lessons.catch-up-history";
    private const int MaximumPreparationHistoryLength = 128 * 1024;
    private const int MaximumPreparationHistoryEntries = 256;
    private const long MinimumUnixTimeSeconds = -62_135_596_800;
    private const long MaximumUnixTimeSeconds = 253_402_300_799;

    private readonly Dictionary<string, DateTimeOffset> _preparationHistory =
        LoadPreparationHistory();

    public async Task<bool> SynchronizeAsync(
        IReadOnlyCollection<IosLessonNotificationRequest> requests,
        CancellationToken cancellationToken)
    {
        var requestedIdentifiers = requests
            .Select(x => x.Identifier)
            .ToHashSet(StringComparer.Ordinal);
        lessonPreparationTimeline.ReconcileScheduledNotifications(
            requestedIdentifiers,
            DateTimeOffset.Now);
        var notificationCenter = UNUserNotificationCenter.Current;
        var pending = await notificationCenter.GetPendingNotificationRequestsAsync() ?? [];
        cancellationToken.ThrowIfCancellationRequested();

        var pendingByIdentifier = pending
            .GroupBy(x => x.Identifier, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
        var deliveredByIdentifier = requests.Any(x => x.IsCatchUp)
            ? (await notificationCenter.GetDeliveredNotificationsAsync() ?? [])
                .GroupBy(x => x.Request.Identifier, StringComparer.Ordinal)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal)
            : new Dictionary<string, UNNotification>(StringComparer.Ordinal);
        cancellationToken.ThrowIfCancellationRequested();
        var systemNow = DateTimeOffset.Now;
        var obsoleteIdentifiers = pending
            .Where(x => x.Identifier.StartsWith(IdentifierPrefix, StringComparison.Ordinal) &&
                        !requestedIdentifiers.Contains(x.Identifier) &&
                        !IsImminentPendingNotification(x, systemNow))
            .Select(x => x.Identifier)
            .ToArray();
        if (obsoleteIdentifiers.Length > 0)
        {
            notificationCenter.RemovePendingNotificationRequests(obsoleteIdentifiers);
        }

        var needsRetry = false;
        var preparationHistoryChanged = false;
        foreach (var request in requests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hasPendingRequest = pendingByIdentifier.TryGetValue(
                request.Identifier,
                out var pendingRequest);
            if (hasPendingRequest &&
                PendingRequestMatches(pendingRequest!, request, out var pendingFireAt))
            {
                if (request.Identifier.EndsWith(".prepare", StringComparison.Ordinal))
                {
                    lessonPreparationTimeline.RestoreNotificationScheduled(
                        request.Identifier,
                        pendingFireAt);
                }
                continue;
            }

            if (!hasPendingRequest &&
                request.IsCatchUp &&
                deliveredByIdentifier.TryGetValue(request.Identifier, out var deliveredNotification))
            {
                var deliveredAt = ToDateTimeOffset(deliveredNotification.Date);
                lessonPreparationTimeline.RestoreNotificationScheduled(
                    request.Identifier,
                    deliveredAt);
                if (!_preparationHistory.TryGetValue(request.Identifier, out var recordedAt) ||
                    recordedAt != deliveredAt)
                {
                    _preparationHistory[request.Identifier] = deliveredAt;
                    preparationHistoryChanged = true;
                }
                continue;
            }

            if (!hasPendingRequest &&
                request.IsCatchUp &&
                _preparationHistory.TryGetValue(request.Identifier, out var historicalFireAt))
            {
                if (historicalFireAt != DateTimeOffset.MinValue)
                {
                    lessonPreparationTimeline.RestoreNotificationScheduled(
                        request.Identifier,
                        historicalFireAt);
                    continue;
                }

                // 旧版本只保存 identifier，无法证明实际触发时间；移除旧记录后
                // 重新调度一次，避免恢复出与系统通知不一致的 Live Activity 门槛。
                _preparationHistory.Remove(request.Identifier);
                preparationHistoryChanged = true;
            }

            if (request.FireAt <= DateTimeOffset.Now.AddSeconds(1))
            {
                if (hasPendingRequest)
                {
                    notificationCenter.RemovePendingNotificationRequests([request.Identifier]);
                }

                if (request.Identifier.EndsWith(".prepare", StringComparison.Ordinal))
                {
                    preparationHistoryChanged |=
                        _preparationHistory.Remove(request.Identifier);
                    needsRetry = true;
                }
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
            lessonPreparationTimeline.ConfirmNotificationScheduled(
                request.Identifier,
                request.FireAt);
            if (request.Identifier.EndsWith(".prepare", StringComparison.Ordinal))
            {
                _preparationHistory[request.Identifier] = request.FireAt;
                preparationHistoryChanged = true;
            }
        }

        if (preparationHistoryChanged)
        {
            SavePreparationHistory();
        }

        return !needsRetry;
    }

    private static Dictionary<string, DateTimeOffset> LoadPreparationHistory()
    {
        var raw = NSUserDefaults.StandardUserDefaults.StringForKey(PreparationHistoryKey);
        var history = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(raw) ||
            raw.Length > MaximumPreparationHistoryLength)
        {
            return history;
        }

        foreach (var line in raw.Split(
                     '\n',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Take(MaximumPreparationHistoryEntries))
        {
            var fields = line.Split('\t', 2, StringSplitOptions.TrimEntries);
            var identifier = fields[0];
            if (!IsRecentPreparationIdentifier(identifier))
            {
                continue;
            }

            history[identifier] = TryParseUnixTime(fields, out var fireAt)
                ? fireAt
                : DateTimeOffset.MinValue;
        }

        return history;
    }

    private void SavePreparationHistory()
    {
        foreach (var identifier in _preparationHistory.Keys
                     .Where(x => !IsRecentPreparationIdentifier(x))
                     .ToArray())
        {
            _preparationHistory.Remove(identifier);
        }

        var value = string.Join(
            '\n',
            _preparationHistory
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x =>
                    $"{x.Key}\t{x.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}"));
        NSUserDefaults.StandardUserDefaults.SetString(value, PreparationHistoryKey);
    }

    private static DateTimeOffset ToDateTimeOffset(NSDate date) =>
        DateTimeOffset.UnixEpoch.AddSeconds(date.SecondsSince1970);

    private static bool TryParseUnixTime(
        IReadOnlyList<string> fields,
        out DateTimeOffset value)
    {
        value = default;
        if (fields.Count != 2 ||
            !long.TryParse(
                fields[1],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var unixSeconds) ||
            unixSeconds is < MinimumUnixTimeSeconds or > MaximumUnixTimeSeconds)
        {
            return false;
        }

        value = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        return true;
    }

    private static bool IsImminentPendingNotification(
        UNNotificationRequest request,
        DateTimeOffset systemNow)
    {
        if (request.Trigger is not UNCalendarNotificationTrigger
            {
                NextTriggerDate: { } nextTriggerDate
            })
        {
            return false;
        }

        var fireAt = ToDateTimeOffset(nextTriggerDate);
        return fireAt >= systemNow.AddSeconds(-5) &&
               fireAt <= systemNow.AddSeconds(2);
    }

    private static bool PendingRequestMatches(
        UNNotificationRequest pendingRequest,
        IosLessonNotificationRequest request,
        out DateTimeOffset fireAt)
    {
        fireAt = default;
        if (pendingRequest.Trigger is not UNCalendarNotificationTrigger
            {
                NextTriggerDate: { } pendingFireDate
            })
        {
            return false;
        }

        fireAt = ToDateTimeOffset(pendingFireDate);
        var content = pendingRequest.Content;
        return fireAt == request.FireAt &&
               string.Equals(content.Title, request.Title, StringComparison.Ordinal) &&
               string.Equals(content.Body, request.Body, StringComparison.Ordinal) &&
               string.Equals(
                   content.CategoryIdentifier,
                   CategoryIdentifier,
                   StringComparison.Ordinal) &&
               string.Equals(
                   content.ThreadIdentifier,
                   CategoryIdentifier,
                   StringComparison.Ordinal) &&
               (content.Sound != null) == request.PlaySound;
    }

    private static bool IsRecentPreparationIdentifier(string identifier)
    {
        var parts = identifier.Split('.');
        return parts.Length >= 3 &&
               DateTime.TryParseExact(
                   parts[2],
                   "yyyyMMdd",
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out var date) &&
               date >= DateTime.Today.AddDays(-1) &&
               date <= DateTime.Today.AddDays(61);
    }
}
