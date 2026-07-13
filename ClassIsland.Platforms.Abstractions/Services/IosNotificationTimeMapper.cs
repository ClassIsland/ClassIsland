namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 将经过 NTP/调试偏移的逻辑课程时间映射回 iOS 系统通知时间。
/// </summary>
internal static class IosNotificationTimeMapper
{
    public static DateTimeOffset ToSystemTime(
        DateTime logicalFireAt,
        DateTime logicalNow,
        DateTimeOffset systemNow,
        TimeZoneInfo? timeZone = null)
    {
        var localFireAt = DateTime.SpecifyKind(
            systemNow.LocalDateTime + (logicalFireAt - logicalNow),
            DateTimeKind.Unspecified);
        var offset = (timeZone ?? TimeZoneInfo.Local).GetUtcOffset(localFireAt);
        var result = new DateTimeOffset(localFireAt, offset);
        return result.AddTicks(-(result.Ticks % TimeSpan.TicksPerSecond));
    }
}
