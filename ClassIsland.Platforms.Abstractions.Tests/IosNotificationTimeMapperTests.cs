using ClassIsland.Platforms.Abstraction.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class IosNotificationTimeMapperTests
{
    private static readonly TimeZoneInfo ChinaStandardTime =
        TimeZoneInfo.CreateCustomTimeZone(
            "Test/China",
            TimeSpan.FromHours(8),
            "Test China Time",
            "Test China Time");

    [Fact]
    public void ToSystemTime_PreservesLogicalDelayAfterNtpCorrection()
    {
        var logicalNow = new DateTime(2026, 7, 13, 14, 26, 0);
        var logicalFireAt = new DateTime(2026, 7, 13, 14, 40, 0);
        var systemNow = new DateTimeOffset(2026, 7, 13, 14, 25, 57, TimeSpan.FromHours(8));

        var result = IosNotificationTimeMapper.ToSystemTime(
            logicalFireAt,
            logicalNow,
            systemNow,
            ChinaStandardTime);

        Assert.Equal(
            new DateTimeOffset(2026, 7, 13, 14, 39, 57, TimeSpan.FromHours(8)),
            result);
    }

    [Fact]
    public void ToSystemTime_TruncatesSubsecondJitterForStableScheduling()
    {
        var logicalNow = new DateTime(2026, 7, 13, 14, 26, 0, 456);
        var logicalFireAt = new DateTime(2026, 7, 13, 14, 40, 0, 456);
        var systemNow = new DateTimeOffset(
            2026,
            7,
            13,
            14,
            26,
            0,
            987,
            TimeSpan.FromHours(8));

        var result = IosNotificationTimeMapper.ToSystemTime(
            logicalFireAt,
            logicalNow,
            systemNow,
            ChinaStandardTime);

        Assert.Equal(0, result.Millisecond);
        Assert.Equal(new TimeSpan(14, 40, 0), result.TimeOfDay);
    }
}
