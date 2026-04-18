using System;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums.Notification;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Services.NotificationProviders;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Helpers;

public readonly record struct DynamicIslandDisplayState(bool IsOnClassModeActive, bool IsCompactModeActive);

public static class DynamicIslandDisplayStateHelper
{
    private static readonly Guid OnClassNotificationChannelId = Guid.Parse(ClassNotificationProvider.OnClassChannelId);

    public static readonly TimeSpan ExpandBeforeClassOff = TimeSpan.FromSeconds(10);

    public static DynamicIslandDisplayState GetCurrentState(
        Settings settings,
        ILessonsService lessonsService,
        IExactTimeService exactTimeService,
        INotificationHostService notificationHostService)
    {
        if (!settings.IsDynamicIslandModeEnabled)
        {
            return new DynamicIslandDisplayState(false, false);
        }

        var currentItem = lessonsService.CurrentTimeLayoutItem;
        var isOnClass = lessonsService.CurrentState == TimeState.OnClass &&
                        currentItem != TimeLayoutItem.Empty &&
                        currentItem.TimeType == 0;
        if (!isOnClass)
        {
            return new DynamicIslandDisplayState(false, false);
        }

        var now = exactTimeService.GetCurrentLocalDateTime().TimeOfDay;
        var leftTime = currentItem.EndTime - now;
        if (leftTime < TimeSpan.Zero)
        {
            leftTime = TimeSpan.Zero;
        }

        var currentRequest = (notificationHostService as NotificationHostService)?.CurrentRequest;
        var isOnClassAnimationPlaying =
            notificationHostService.IsNotificationsPlaying &&
            currentRequest?.State == NotificationState.Playing &&
            currentRequest.ChannelId == OnClassNotificationChannelId;
        var shouldExpandForClassOffAnimation = leftTime <= ExpandBeforeClassOff;

        var isCompact = !isOnClassAnimationPlaying && !shouldExpandForClassOffAnimation;
        return new DynamicIslandDisplayState(true, isCompact);
    }
}
