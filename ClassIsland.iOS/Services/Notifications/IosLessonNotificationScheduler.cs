using System.Globalization;
using ClassIsland.Platforms.Abstraction.Services;
using Foundation;
using UserNotifications;

namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// 仅管理 ClassIsland 课程提醒前缀下的 iOS 本地通知。
/// </summary>
internal sealed class IosLessonNotificationScheduler(
    LessonPreparationNotificationTimeline lessonPreparationTimeline,
    IosNotificationMutationGate mutationGate)
{
    private const string IdentifierPrefix = "classisland.lessons.";
    internal const string CategoryIdentifier = "classisland.lessons";
    // 保留旧 key 以迁移早期测试包只存 identifier 的格式。
    private const string PreparationHistoryKey = "classisland.lessons.catch-up-history";
    private const int MaximumPreparationHistoryLength = 128 * 1024;
    private const int MaximumPreparationHistoryEntries = 256;
    private const long MinimumUnixTimeSeconds = -62_135_596_800;
    private const long MaximumUnixTimeSeconds = 253_402_300_799;
    private const int MaximumBackgroundMutationCount = 8;

    private readonly Dictionary<string, DateTimeOffset> _preparationHistory =
        LoadPreparationHistory();

    public Task<IosLessonNotificationSynchronizationResult> SynchronizeAsync(
        IReadOnlyCollection<IosLessonNotificationRequest> requests,
        bool allowLargeMutations,
        Action<IosLessonNotificationSynchronizationResult> publishConfirmedResult,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(publishConfirmedResult);
        return mutationGate.ExecuteAsync(
            async () =>
            {
                var result = await SynchronizeCoreAsync(
                    requests,
                    allowLargeMutations,
                    cancellationToken);
                // 发布确认快照后才释放 mutation gate，避免即时 fallback 在
                // 原生排程已改变、托管快照尚未更新的窗口内重复投递。
                publishConfirmedResult(result);
                return result;
            },
            cancellationToken);
    }

    private async Task<IosLessonNotificationSynchronizationResult> SynchronizeCoreAsync(
        IReadOnlyCollection<IosLessonNotificationRequest> requests,
        bool allowLargeMutations,
        CancellationToken cancellationToken)
    {
        var notificationCenter = UNUserNotificationCenter.Current;
        var pending = await notificationCenter.GetPendingNotificationRequestsAsync() ?? [];
        cancellationToken.ThrowIfCancellationRequested();

        var distinctCandidates = requests
            .GroupBy(x => x.Identifier, StringComparer.Ordinal)
            .Select(x => x.First())
            .ToArray();
        var pendingByIdentifier = pending
            .GroupBy(x => x.Identifier, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
        var pendingIdentifiers = pendingByIdentifier.Keys
            .ToHashSet(StringComparer.Ordinal);
        var systemNow = DateTimeOffset.Now;
        var capacitySelection = SelectForAvailableCapacity(
            distinctCandidates,
            pending,
            systemNow);
        var distinctRequests = capacitySelection.Requests;
        var selectedIdentifiers = distinctRequests
            .Select(x => x.Identifier)
            .ToHashSet(StringComparer.Ordinal);
        var protectedImminentIdentifiers = pending
            .Where(x => x.Identifier.StartsWith(IdentifierPrefix, StringComparison.Ordinal) &&
                        !selectedIdentifiers.Contains(x.Identifier) &&
                        IsImminentPendingNotification(x, systemNow))
            .Select(x => x.Identifier)
            .ToHashSet(StringComparer.Ordinal);

        var deliveredByIdentifier = distinctRequests.Any(x => x.IsCatchUp)
            ? (await notificationCenter.GetDeliveredNotificationsAsync() ?? [])
                .GroupBy(x => x.Request.Identifier, StringComparer.Ordinal)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal)
            : new Dictionary<string, UNNotification>(StringComparer.Ordinal);
        cancellationToken.ThrowIfCancellationRequested();

        var matchingPendingFireTimes = new Dictionary<string, DateTimeOffset>(
            StringComparer.Ordinal);
        var logicallySatisfiedCatchUpFireTimes = new Dictionary<string, DateTimeOffset>(
            StringComparer.Ordinal);
        var requestsToSubmit = new List<IosLessonNotificationRequest>(
            distinctRequests.Count);
        var removeLegacyHistoryIdentifiers = new HashSet<string>(StringComparer.Ordinal);
        var skippedExpiredRequest = false;
        foreach (var request in distinctRequests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hasPendingRequest = pendingByIdentifier.TryGetValue(
                request.Identifier,
                out var pendingRequest);
            if (hasPendingRequest &&
                PendingRequestMatches(pendingRequest!, request, out var pendingFireAt))
            {
                matchingPendingFireTimes[request.Identifier] = pendingFireAt;
                continue;
            }

            if (!hasPendingRequest &&
                request.IsCatchUp &&
                deliveredByIdentifier.TryGetValue(request.Identifier, out var deliveredNotification))
            {
                logicallySatisfiedCatchUpFireTimes[request.Identifier] =
                    ToDateTimeOffset(deliveredNotification.Date);
                continue;
            }

            if (!hasPendingRequest &&
                request.IsCatchUp &&
                _preparationHistory.TryGetValue(request.Identifier, out var historicalFireAt))
            {
                if (historicalFireAt != DateTimeOffset.MinValue)
                {
                    logicallySatisfiedCatchUpFireTimes[request.Identifier] = historicalFireAt;
                    continue;
                }

                // 旧版本只保存 identifier，无法证明实际触发时间；重新调度一次。
                removeLegacyHistoryIdentifiers.Add(request.Identifier);
            }

            if (request.FireAt <= DateTimeOffset.Now.AddSeconds(1))
            {
                skippedExpiredRequest = true;
                continue;
            }

            requestsToSubmit.Add(request);
        }

        var protectedMatchingRequests = distinctCandidates
            .Where(x => protectedImminentIdentifiers.Contains(x.Identifier) &&
                        pendingByIdentifier.TryGetValue(x.Identifier, out var pendingRequest) &&
                        PendingRequestMatches(pendingRequest, x, out _))
            .ToArray();
        foreach (var request in protectedMatchingRequests)
        {
            _ = PendingRequestMatches(
                pendingByIdentifier[request.Identifier],
                request,
                out var pendingFireAt);
            matchingPendingFireTimes[request.Identifier] = pendingFireAt;
        }

        var desiredNativeIdentifiers = matchingPendingFireTimes.Keys
            .Concat(requestsToSubmit.Select(x => x.Identifier))
            .Concat(protectedImminentIdentifiers)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var plan = IosNotificationSynchronizationPolicy.CreatePlan(
            desiredNativeIdentifiers,
            requestsToSubmit.Select(x => x.Identifier),
            pendingIdentifiers,
            IdentifierPrefix,
            IosNotificationCapacityPolicy.MaximumPendingNotificationCount);
        var requestsToSubmitByIdentifier = requestsToSubmit.ToDictionary(
            x => x.Identifier,
            StringComparer.Ordinal);
        var mutationCount = IosNotificationSynchronizationExecutionPolicy
            .GetMutationCount(plan);
        if (IosNotificationSynchronizationExecutionPolicy.ShouldDeferLargeMutation(
                mutationCount,
                MaximumBackgroundMutationCount,
                allowLargeMutations))
        {
            lessonPreparationTimeline.ReconcileScheduledNotifications(
                pendingIdentifiers
                    .Where(x => x.EndsWith(".prepare", StringComparison.Ordinal))
                    .Concat(logicallySatisfiedCatchUpFireTimes.Keys)
                    .Distinct(StringComparer.Ordinal),
                DateTimeOffset.Now);
            throw new IosNotificationSynchronizationDeferredException(
                mutationCount,
                MaximumBackgroundMutationCount);
        }

        var modifiedIdentifiers = new List<string>();
        var removedObsoleteIdentifiers = new List<string>();
        IReadOnlySet<string> confirmedNativeIdentifiers;
        try
        {
            var identifiersRequiredDuringSwap = plan.RequestedIdentifiers
                .Where(pendingIdentifiers.Contains)
                .ToHashSet(StringComparer.Ordinal);
            foreach (var step in plan.UpsertSteps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (step.ObsoleteIdentifierToRemoveBeforeUpsert is { } obsoleteIdentifier)
                {
                    removedObsoleteIdentifiers.Add(obsoleteIdentifier);
                    notificationCenter.RemovePendingNotificationRequests(
                        [obsoleteIdentifier]);
                }

                var request = requestsToSubmitByIdentifier[step.Identifier];
                // 在跨过原生边界前记录；API 可能已经接受请求后才向托管侧报错。
                modifiedIdentifiers.Add(step.Identifier);
                await SubmitRequestAsync(notificationCenter, request);
                identifiersRequiredDuringSwap.Add(step.Identifier);

                if (step.ObsoleteIdentifierToRemoveBeforeUpsert != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var confirmedAfterStep = await GetConfirmedIdentifiersAsync(
                        notificationCenter,
                        identifiersRequiredDuringSwap);
                    EnsureConfirmedIdentifiers(
                        identifiersRequiredDuringSwap,
                        confirmedAfterStep,
                        $"提交课程通知 {step.Identifier} 后");
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            confirmedNativeIdentifiers = await GetConfirmedIdentifiersAsync(
                notificationCenter,
                plan.RequestedIdentifiers);
            EnsureConfirmedIdentifiers(
                plan.RequestedIdentifiers,
                confirmedNativeIdentifiers,
                "提交新的课程通知后");

            if (plan.ObsoleteIdentifiersToRemoveAfterUpsert.Count > 0)
            {
                removedObsoleteIdentifiers.AddRange(
                    plan.ObsoleteIdentifiersToRemoveAfterUpsert);
                notificationCenter.RemovePendingNotificationRequests(
                    plan.ObsoleteIdentifiersToRemoveAfterUpsert.ToArray());
            }

            cancellationToken.ThrowIfCancellationRequested();
            confirmedNativeIdentifiers = await GetConfirmedIdentifiersAsync(
                notificationCenter,
                plan.RequestedIdentifiers);
            EnsureConfirmedIdentifiers(
                plan.RequestedIdentifiers,
                confirmedNativeIdentifiers,
                "清理旧课程通知后");
        }
        catch (Exception synchronizationException)
        {
            var rollbackExceptions = await RollbackAsync(
                notificationCenter,
                modifiedIdentifiers,
                removedObsoleteIdentifiers,
                pendingByIdentifier);
            if (rollbackExceptions.Count > 0)
            {
                throw new IosNotificationSynchronizationRollbackException(
                    synchronizationException,
                    rollbackExceptions);
            }

            try
            {
                var restoredPendingPreparationIdentifiers =
                    (await notificationCenter.GetPendingNotificationRequestsAsync() ?? [])
                    .Select(x => x.Identifier)
                    .Where(x => x.StartsWith(IdentifierPrefix, StringComparison.Ordinal) &&
                                x.EndsWith(".prepare", StringComparison.Ordinal))
                    .ToArray();
                lessonPreparationTimeline.ReconcileScheduledNotifications(
                    restoredPendingPreparationIdentifiers,
                    DateTimeOffset.Now);
            }
            catch (Exception reconciliationException)
            {
                throw new IosNotificationSynchronizationRollbackException(
                    synchronizationException,
                    [reconciliationException]);
            }

            throw;
        }

        var synchronizedRequests = distinctRequests
            .Concat(protectedMatchingRequests)
            .DistinctBy(x => x.Identifier, StringComparer.Ordinal)
            .Where(x => logicallySatisfiedCatchUpFireTimes.ContainsKey(x.Identifier) ||
                        confirmedNativeIdentifiers.Contains(x.Identifier))
            .OrderBy(x => x.FireAt)
            .ThenBy(x => x.Identifier, StringComparer.Ordinal)
            .ToArray();

        // 只有原生中心确认保留（或已经送达）后，才开放 Live Activity 门槛。
        foreach (var request in synchronizedRequests.Where(x =>
                     x.Identifier.EndsWith(".prepare", StringComparison.Ordinal)))
        {
            if (logicallySatisfiedCatchUpFireTimes.TryGetValue(
                    request.Identifier,
                    out var satisfiedAt))
            {
                lessonPreparationTimeline.RestoreNotificationScheduled(
                    request.Identifier,
                    satisfiedAt);
            }
            else if (matchingPendingFireTimes.TryGetValue(
                         request.Identifier,
                         out var pendingFireAt))
            {
                lessonPreparationTimeline.RestoreNotificationScheduled(
                    request.Identifier,
                    pendingFireAt);
            }
            else
            {
                lessonPreparationTimeline.ConfirmNotificationScheduled(
                    request.Identifier,
                    request.FireAt);
            }
        }

        lessonPreparationTimeline.ReconcileScheduledNotifications(
            confirmedNativeIdentifiers
                .Where(x => x.EndsWith(".prepare", StringComparison.Ordinal))
                .Concat(synchronizedRequests
                    .Where(x => x.Identifier.EndsWith(
                        ".prepare",
                        StringComparison.Ordinal))
                    .Select(x => x.Identifier))
                .Distinct(StringComparer.Ordinal),
            DateTimeOffset.Now);

        var preparationHistoryChanged = false;
        foreach (var identifier in removeLegacyHistoryIdentifiers)
        {
            preparationHistoryChanged |= _preparationHistory.Remove(identifier);
        }
        foreach (var request in synchronizedRequests.Where(x => x.IsCatchUp))
        {
            var fireAt = logicallySatisfiedCatchUpFireTimes.TryGetValue(
                request.Identifier,
                out var satisfiedAt)
                ? satisfiedAt
                : matchingPendingFireTimes.TryGetValue(
                    request.Identifier,
                    out var pendingAt)
                    ? pendingAt
                    : request.FireAt;
            if (!_preparationHistory.TryGetValue(request.Identifier, out var recordedAt) ||
                recordedAt != fireAt)
            {
                _preparationHistory[request.Identifier] = fireAt;
                preparationHistoryChanged = true;
            }
        }

        if (preparationHistoryChanged)
        {
            try
            {
                SavePreparationHistory();
            }
            catch (Exception exception)
            {
                // 原生排程已经提交；历史持久化失败不能把有效排程降级成未知状态。
                Console.Error.WriteLine($"保存 iOS 补发通知历史失败：{exception}");
            }
        }

        var shouldRetry = IosNotificationSynchronizationExecutionPolicy.ShouldRetry(
            skippedExpiredRequest,
            distinctCandidates.Length,
            synchronizedRequests.Length,
            capacitySelection.HasTransientCapacityPressure);
        return new IosLessonNotificationSynchronizationResult(
            synchronizedRequests,
            shouldRetry,
            pending.Count(x => !x.Identifier.StartsWith(
                IdentifierPrefix,
                StringComparison.Ordinal)) +
            plan.RequestedIdentifiers.Count <
            IosNotificationCapacityPolicy.MaximumPendingNotificationCount);
    }

    private static CapacitySelectionResult SelectForAvailableCapacity(
        IReadOnlyCollection<IosLessonNotificationRequest> candidates,
        IReadOnlyCollection<UNNotificationRequest> pending,
        DateTimeOffset systemNow)
    {
        var nonManagedPendingCount = pending.Count(x =>
            !x.Identifier.StartsWith(IdentifierPrefix, StringComparison.Ordinal));
        var maximumManagedCount = IosNotificationCapacityPolicy
            .GetMaximumManagedNotificationCount(
                IosLessonNotificationScheduleFactory.MaximumPendingNotifications,
                nonManagedPendingCount);

        IReadOnlyList<IosLessonNotificationRequest> selected = [];
        while (maximumManagedCount > 0)
        {
            selected = IosLessonNotificationScheduleSelector.Select(
                candidates,
                maximumManagedCount);
            var selectedIdentifiers = selected
                .Select(x => x.Identifier)
                .ToHashSet(StringComparer.Ordinal);
            var protectedImminentCount = pending.Count(x =>
                x.Identifier.StartsWith(IdentifierPrefix, StringComparison.Ordinal) &&
                !selectedIdentifiers.Contains(x.Identifier) &&
                IsImminentPendingNotification(x, systemNow));
            var adjustedMaximum = IosNotificationCapacityPolicy
                .GetMaximumManagedNotificationCount(
                    IosLessonNotificationScheduleFactory.MaximumPendingNotifications,
                    nonManagedPendingCount + protectedImminentCount);
            if (adjustedMaximum >= maximumManagedCount)
            {
                break;
            }

            if (adjustedMaximum == 0)
            {
                return new CapacitySelectionResult(
                    [],
                    protectedImminentCount > 0 ||
                    HasTransientCapacityPressure(pending));
            }

            maximumManagedCount = adjustedMaximum;
        }

        var selectedIdentifiers = selected
            .Select(x => x.Identifier)
            .ToHashSet(StringComparer.Ordinal);
        var hasProtectedImminentRequest = pending.Any(x =>
            x.Identifier.StartsWith(IdentifierPrefix, StringComparison.Ordinal) &&
            !selectedIdentifiers.Contains(x.Identifier) &&
            IsImminentPendingNotification(x, systemNow));
        return new CapacitySelectionResult(
            selected,
            hasProtectedImminentRequest || HasTransientCapacityPressure(pending));
    }

    private static bool HasTransientCapacityPressure(
        IEnumerable<UNNotificationRequest> pending) =>
        pending.Any(x => x.Identifier.StartsWith(
            IosNotificationCapacityPolicy.ImmediateFallbackIdentifierPrefix,
            StringComparison.Ordinal));

    private static async Task SubmitRequestAsync(
        UNUserNotificationCenter notificationCenter,
        IosLessonNotificationRequest request)
    {
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
    }

    private static async Task<IReadOnlyList<Exception>> RollbackAsync(
        UNUserNotificationCenter notificationCenter,
        IReadOnlyCollection<string> modifiedIdentifiers,
        IReadOnlyCollection<string> removedObsoleteIdentifiers,
        IReadOnlyDictionary<string, UNNotificationRequest> pendingByIdentifier)
    {
        var rollbackPlan = IosNotificationSynchronizationPolicy.CreateRollbackPlan(
            modifiedIdentifiers,
            pendingByIdentifier.Keys,
            removedObsoleteIdentifiers);
        if (rollbackPlan.AddedIdentifiersToRemove.Count > 0)
        {
            notificationCenter.RemovePendingNotificationRequests(
                rollbackPlan.AddedIdentifiersToRemove.ToArray());
        }

        var exceptions = new List<Exception>();
        // 恢复完整的原课程排程；iOS 达到上限时也可能静默淘汰未直接修改的请求。
        foreach (var identifier in pendingByIdentifier.Keys.Where(x =>
                     x.StartsWith(IdentifierPrefix, StringComparison.Ordinal)))
        {
            try
            {
                await notificationCenter.AddNotificationRequestAsync(
                    pendingByIdentifier[identifier]);
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }
        }

        try
        {
            var originalManagedIdentifiers = pendingByIdentifier.Keys
                .Where(x => x.StartsWith(IdentifierPrefix, StringComparison.Ordinal))
                .ToArray();
            var restoredIdentifiers = await GetConfirmedIdentifiersAsync(
                notificationCenter,
                originalManagedIdentifiers);
            var missingOriginalIdentifiers = IosNotificationSynchronizationPolicy
                .GetMissingIdentifiers(originalManagedIdentifiers, restoredIdentifiers);
            if (missingOriginalIdentifiers.Count > 0)
            {
                exceptions.Add(new InvalidOperationException(
                    "The previous iOS notification schedule was not fully restored: " +
                    string.Join(", ", missingOriginalIdentifiers)));
            }
        }
        catch (Exception exception)
        {
            exceptions.Add(exception);
        }

        return exceptions;
    }

    private static async Task<IReadOnlySet<string>> GetConfirmedIdentifiersAsync(
        UNUserNotificationCenter notificationCenter,
        IEnumerable<string> expectedIdentifiers)
    {
        var expected = expectedIdentifiers.ToHashSet(StringComparer.Ordinal);
        var confirmed = (await notificationCenter.GetPendingNotificationRequestsAsync() ?? [])
            .Select(x => x.Identifier)
            .Where(expected.Contains)
            .ToHashSet(StringComparer.Ordinal);
        if (confirmed.Count == expected.Count)
        {
            return confirmed;
        }

        // 很近的补发通知可能在 native upsert 和确认查询之间已经送达。
        foreach (var deliveredIdentifier in
                 (await notificationCenter.GetDeliveredNotificationsAsync() ?? [])
                 .Select(x => x.Request.Identifier))
        {
            if (expected.Contains(deliveredIdentifier))
            {
                confirmed.Add(deliveredIdentifier);
            }
        }

        return confirmed;
    }

    private static void EnsureConfirmedIdentifiers(
        IEnumerable<string> expectedIdentifiers,
        IReadOnlySet<string> confirmedIdentifiers,
        string operation)
    {
        var missingIdentifiers = IosNotificationSynchronizationPolicy
            .GetMissingIdentifiers(expectedIdentifiers, confirmedIdentifiers);
        if (missingIdentifiers.Count > 0)
        {
            throw new InvalidOperationException(
                $"{operation}，iOS 未保留或送达以下课程通知：" +
                string.Join(", ", missingIdentifiers));
        }
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

internal sealed record IosLessonNotificationSynchronizationResult(
    IReadOnlyList<IosLessonNotificationRequest> Requests,
    bool ShouldRetry,
    bool HasFallbackCapacity);

internal sealed record CapacitySelectionResult(
    IReadOnlyList<IosLessonNotificationRequest> Requests,
    bool HasTransientCapacityPressure);

internal sealed class IosNotificationSynchronizationDeferredException(
    int mutationCount,
    int maximumBackgroundMutationCount)
    : Exception(
        $"iOS/iPadOS 后台通知同步需要 {mutationCount} 次修改，" +
        $"超过后台安全阈值 {maximumBackgroundMutationCount}，已延后到前台。")
{
}

internal sealed class IosNotificationSynchronizationRollbackException(
    Exception synchronizationException,
    IReadOnlyCollection<Exception> rollbackExceptions)
    : Exception(
        "回滚 iOS/iPadOS 课程通知排程失败，当前原生排程状态无法确认。",
        new AggregateException(
            new[] { synchronizationException }.Concat(rollbackExceptions)))
{
}
