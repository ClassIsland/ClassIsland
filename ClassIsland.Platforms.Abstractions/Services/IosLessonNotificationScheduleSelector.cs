namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// 在 iOS 待处理通知上限内，优先完整覆盖近期课程，再均衡覆盖更远日期。
/// </summary>
internal static class IosLessonNotificationScheduleSelector
{
    internal const int FullyCoveredNearTermDayCount = 2;

    public static IReadOnlyList<IosLessonNotificationRequest> Select(
        IEnumerable<IosLessonNotificationRequest> requests,
        int maximumCount)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumCount);

        var orderedRequests = requests
            .OrderBy(x => x.FireAt)
            .ThenBy(x => x.Identifier, StringComparer.Ordinal)
            .ToArray();
        if (orderedRequests.Length <= maximumCount)
        {
            return orderedRequests;
        }

        var groupsByDay = orderedRequests
            .GroupBy(x => x.ChainId ?? x.Identifier, StringComparer.Ordinal)
            .Select(x => x.OrderBy(request => request.FireAt)
                .ThenBy(request => request.Identifier, StringComparer.Ordinal)
                .ToArray())
            .OrderBy(x => x[0].FireAt)
            .ThenBy(x => x[0].Identifier, StringComparer.Ordinal)
            .GroupBy(x => DateOnly.FromDateTime(x[^1].FireAt.LocalDateTime))
            .Select(x => x.ToArray())
            .ToArray();

        var selectedGroups = new List<IosLessonNotificationRequest[]>();
        var remaining = maximumCount;
        var nearTermDayCount = Math.Min(
            FullyCoveredNearTermDayCount,
            groupsByDay.Length);
        for (var dayIndex = 0; dayIndex < nearTermDayCount; dayIndex++)
        {
            var dayGroups = groupsByDay[dayIndex];
            var dayRequestCount = dayGroups.Sum(x => x.Length);
            if (dayRequestCount <= remaining)
            {
                selectedGroups.AddRange(dayGroups);
                remaining -= dayRequestCount;
                continue;
            }

            selectedGroups.AddRange(SelectPriorityGroupsWithinBudget(
                dayGroups,
                remaining));
            return Flatten(selectedGroups);
        }

        if (remaining == 0 || nearTermDayCount == groupsByDay.Length)
        {
            return Flatten(selectedGroups);
        }

        var futureDays = groupsByDay.Skip(nearTermDayCount).ToArray();
        var selectedIndexes = futureDays
            .Select(_ => new HashSet<int>())
            .ToArray();
        var coveredDayIndexes = new HashSet<int>();

        // 先为更远日期均匀选择一条最高优先级完整链，避免预算全部集中在
        // 最早若干天；近期两天已经在上面获得完整覆盖。
        while (remaining > 0)
        {
            var dayIndex = FindMostSeparatedUncoveredDayIndex(
                futureDays,
                coveredDayIndexes,
                remaining);
            if (dayIndex < 0)
            {
                break;
            }

            var groupIndex = FindPreferredGroupIndex(futureDays[dayIndex]);
            var group = futureDays[dayIndex][groupIndex];
            coveredDayIndexes.Add(dayIndex);
            selectedIndexes[dayIndex].Add(groupIndex);
            selectedGroups.Add(group);
            remaining -= group.Length;
        }

        // 仍有额度时，在已覆盖日期内优先补齐其它课程链，最后才选择课间。
        while (remaining > 0)
        {
            var allocated = false;
            foreach (var dayIndex in coveredDayIndexes.Order())
            {
                var groupIndex = FindMostSeparatedGroupIndex(
                    futureDays[dayIndex],
                    selectedIndexes[dayIndex],
                    remaining);
                if (groupIndex < 0)
                {
                    continue;
                }

                var group = futureDays[dayIndex][groupIndex];
                selectedIndexes[dayIndex].Add(groupIndex);
                selectedGroups.Add(group);
                remaining -= group.Length;
                allocated = true;
                if (remaining == 0)
                {
                    break;
                }
            }

            if (!allocated)
            {
                break;
            }
        }

        return Flatten(selectedGroups);
    }

    private static int FindMostSeparatedUncoveredDayIndex(
        IReadOnlyList<IosLessonNotificationRequest[][]> days,
        IReadOnlySet<int> selectedDayIndexes,
        int remainingRequestCount)
    {
        var bestIndex = -1;
        var bestDistance = -1;
        for (var index = 0; index < days.Count; index++)
        {
            if (selectedDayIndexes.Contains(index))
            {
                continue;
            }

            var preferredGroup = days[index][FindPreferredGroupIndex(days[index])];
            if (preferredGroup.Length > remainingRequestCount)
            {
                continue;
            }

            if (selectedDayIndexes.Count == 0)
            {
                return index;
            }

            var distance = selectedDayIndexes.Min(selected => Math.Abs(selected - index));
            if (distance > bestDistance ||
                distance == bestDistance && index > bestIndex)
            {
                bestIndex = index;
                bestDistance = distance;
            }
        }

        return bestIndex;
    }

    private static int FindMostSeparatedGroupIndex(
        IReadOnlyList<IosLessonNotificationRequest[]> groups,
        IReadOnlySet<int> selectedIndexes,
        int remainingRequestCount)
    {
        var bestIndex = -1;
        var bestDistance = -1;
        var bestPriority = int.MaxValue;
        for (var index = 0; index < groups.Count; index++)
        {
            if (selectedIndexes.Contains(index) ||
                groups[index].Length > remainingRequestCount)
            {
                continue;
            }

            var priority = GetGroupPriority(groups[index]);
            var distance = selectedIndexes.Min(selected => Math.Abs(selected - index));
            if (priority < bestPriority ||
                priority == bestPriority &&
                (distance > bestDistance ||
                 distance == bestDistance && index > bestIndex))
            {
                bestIndex = index;
                bestDistance = distance;
                bestPriority = priority;
            }
        }

        return bestIndex;
    }

    private static int FindPreferredGroupIndex(
        IReadOnlyList<IosLessonNotificationRequest[]> groups)
    {
        var bestIndex = 0;
        var bestPriority = GetGroupPriority(groups[0]);
        var bestRequestCount = groups[0].Length;
        for (var index = 1; index < groups.Count; index++)
        {
            var priority = GetGroupPriority(groups[index]);
            var requestCount = groups[index].Length;
            if (priority < bestPriority ||
                priority == bestPriority && requestCount < bestRequestCount)
            {
                bestIndex = index;
                bestPriority = priority;
                bestRequestCount = requestCount;
            }
        }

        return bestIndex;
    }

    private static int GetGroupPriority(
        IReadOnlyCollection<IosLessonNotificationRequest> group)
    {
        if (group.Any(x =>
                x.ChannelId == IosNotificationSchedulingPolicy.OnClassChannelId))
        {
            return 0;
        }

        return group.Any(x =>
            x.ChannelId == IosNotificationSchedulingPolicy.PrepareOnClassChannelId)
            ? 1
            : 2;
    }

    private static IEnumerable<IosLessonNotificationRequest[]> SelectPriorityGroupsWithinBudget(
        IEnumerable<IosLessonNotificationRequest[]> groups,
        int maximumCount)
    {
        var remaining = maximumCount;
        foreach (var group in groups
                     .OrderBy(GetGroupPriority)
                     .ThenBy(x => x[0].FireAt)
                     .ThenBy(x => x[0].Identifier, StringComparer.Ordinal))
        {
            if (group.Length > remaining)
            {
                continue;
            }

            yield return group;
            remaining -= group.Length;
            if (remaining == 0)
            {
                yield break;
            }
        }
    }

    private static IReadOnlyList<IosLessonNotificationRequest> Flatten(
        IEnumerable<IosLessonNotificationRequest[]> groups) =>
        groups
            .SelectMany(x => x)
            .OrderBy(x => x.FireAt)
            .ThenBy(x => x.Identifier, StringComparer.Ordinal)
            .ToArray();
}
