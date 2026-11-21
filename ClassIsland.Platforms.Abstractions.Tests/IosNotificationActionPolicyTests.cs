using ClassIsland.iOS.Services.Notifications;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class IosNotificationActionPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 8, 0, 0, TimeSpan.FromHours(8));

    [Theory]
    [InlineData("2026-11-08", "classisland://app/system/schedule?date=2026-11-08")]
    [InlineData("2028-02-29", "classisland://app/system/schedule?date=2028-02-29")]
    [InlineData(null, "classisland://app/system/schedule")]
    [InlineData("2026-02-29", "classisland://app/system/schedule")]
    [InlineData("2026-11-08&operation=delete", "classisland://app/system/schedule")]
    [InlineData("https://example.com/", "classisland://app/system/schedule")]
    public void CreateScheduleUri_UsesOnlyValidatedDate(string? date, string expected)
    {
        Assert.Equal(expected, IosNotificationActionPolicy.CreateScheduleUri(date).AbsoluteUri);
    }

    [Fact]
    public void GetSnoozeTime_KeepsFiveMinuteDelayBeforeLessonDeadline()
    {
        var deadline = Now.AddMinutes(6).ToUnixTimeSeconds().ToString();

        Assert.Equal(Now.AddMinutes(5),
            IosNotificationActionPolicy.GetSnoozeTime(Now, deadline, false));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(5)]
    public void GetSnoozeTime_RejectsExpiredOrTooShortWindow(int deadlineMinutes)
    {
        var deadline = Now.AddMinutes(deadlineMinutes).ToUnixTimeSeconds().ToString();

        Assert.Null(IosNotificationActionPolicy.GetSnoozeTime(Now, deadline, false));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-timestamp")]
    [InlineData("9223372036854775808")]
    [InlineData("253402300800")]
    [InlineData("-62135596801")]
    public void GetSnoozeTime_RejectsMalformedOrOutOfRangeDeadline(string? deadline)
    {
        Assert.Null(IosNotificationActionPolicy.GetSnoozeTime(Now, deadline, false));
    }

    [Fact]
    public void GetSnoozeTime_DoesNotAllowRepeatedSnooze()
    {
        var deadline = Now.AddHours(1).ToUnixTimeSeconds().ToString();

        Assert.Null(IosNotificationActionPolicy.GetSnoozeTime(Now, deadline, true));
    }

    [Fact]
    public void GetSnoozeTime_NearMaximumDateDoesNotOverflow()
    {
        Assert.Null(IosNotificationActionPolicy.GetSnoozeTime(
            DateTimeOffset.MaxValue,
            "253402300799",
            false));
    }

    [Fact]
    public void GetSnoozeIdentifier_IsStableOutsideManagedSchedulePrefix()
    {
        const string identifier = "classisland.lessons.20261108.01.288000000000.on";
        var result = IosNotificationActionPolicy.GetSnoozeIdentifier(identifier);

        Assert.NotNull(result);
        Assert.StartsWith(IosNotificationActionPolicy.SnoozedIdentifierPrefix, result);
        Assert.Equal(result, IosNotificationActionPolicy.GetSnoozeIdentifier(identifier));
        Assert.NotEqual(result, IosNotificationActionPolicy.GetSnoozeIdentifier(identifier + ".prepare"));
        Assert.DoesNotContain("classisland.lessons.", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unrelated.notification")]
    [InlineData("classisland.snoozed.abc")]
    public void GetSnoozeIdentifier_RejectsUnmanagedOrRepeatedRequests(string? identifier)
    {
        Assert.Null(IosNotificationActionPolicy.GetSnoozeIdentifier(identifier));
    }

    [Fact]
    public void GetSnoozeIdentifier_RejectsUnboundedPayload()
    {
        Assert.Null(IosNotificationActionPolicy.GetSnoozeIdentifier(
            "classisland.lessons." + new string('a', 512)));
    }
}
