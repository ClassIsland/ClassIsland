using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// 可独立测试的 iOS 课程通知支持范围及队列完成策略。
/// </summary>
internal static class IosNotificationSchedulingPolicy
{
    internal static readonly Guid ClassNotificationProviderId = Guid.Parse(
        "08F0D9C3-C770-4093-A3D0-02F3D90C24BC");
    internal static readonly Guid PrepareOnClassChannelId = Guid.Parse(
        "CDDFE7FF-B904-4C73-B458-82793B2F66E9");
    internal static readonly Guid OnClassChannelId = Guid.Parse(
        "AFF5B9A4-037C-4A71-8563-C9EA87DDA75C");
    internal static readonly Guid OnBreakingChannelId = Guid.Parse(
        "77C9F3FB-0A2A-4B22-BDDF-3C333462B2F9");

    internal static IReadOnlyList<Guid> SupportedChannelIds { get; } =
    [
        PrepareOnClassChannelId,
        OnClassChannelId,
        OnBreakingChannelId
    ];

    public static bool ShouldRequestAuthorization(
        bool appNotificationsEnabled,
        bool providerEnabled,
        IEnumerable<bool> supportedChannelEnabledStates)
    {
        ArgumentNullException.ThrowIfNull(supportedChannelEnabledStates);
        return appNotificationsEnabled &&
               providerEnabled &&
               supportedChannelEnabledStates.Any(x => x);
    }

    public static bool CanCompleteQueueTicket(
        Guid providerId,
        Guid channelId,
        DateTime expectedLocalFireTime,
        IReadOnlyCollection<IosLessonNotificationRequest> scheduledRequests,
        TimeSpan matchTolerance)
    {
        ArgumentNullException.ThrowIfNull(scheduledRequests);
        if (providerId != ClassNotificationProviderId ||
            !SupportedChannelIds.Contains(channelId) ||
            matchTolerance < TimeSpan.Zero)
        {
            return false;
        }

        return scheduledRequests.Any(x =>
            x.ChannelId == channelId &&
            (x.FireAt.LocalDateTime - expectedLocalFireTime).Duration() <= matchTolerance);
    }

    public static DateTime GetExpectedQueueTicketLocalFireTime(
        Guid channelId,
        bool isChainedTail,
        DateTime? chainedLogicalEndTime,
        DateTime logicalNow,
        DateTimeOffset systemNow)
    {
        if (channelId == OnClassChannelId &&
            isChainedTail &&
            chainedLogicalEndTime is { } logicalEndTime)
        {
            return IosNotificationTimeMapper.ToSystemTime(
                    logicalEndTime,
                    logicalNow,
                    systemNow)
                .LocalDateTime;
        }

        return systemNow.LocalDateTime;
    }
}
