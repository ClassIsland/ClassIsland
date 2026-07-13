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
        // DateTime 保留传入 offset 的墙上时间；LocalDateTime 会被构建主机时区再次换算。
        var localFireAt = DateTime.SpecifyKind(
            systemNow.DateTime + (logicalFireAt - logicalNow),
            DateTimeKind.Unspecified);
        var offset = (timeZone ?? TimeZoneInfo.Local).GetUtcOffset(localFireAt);
        var result = new DateTimeOffset(localFireAt, offset);
        return result.AddTicks(-(result.Ticks % TimeSpan.TicksPerSecond));
    }
}
