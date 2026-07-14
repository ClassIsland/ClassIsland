using ClassIsland.iOS.Services.Notifications;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class IosLessonNotificationSchedulingTests
{
    [Fact]
    public void Select_BalancesDenseScheduleAcrossSevenDays()
    {
        var firstDay = new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero);
        var requests = Enumerable.Range(0, 7)
            .SelectMany(day => Enumerable.Range(0, 23)
                .Select(index => CreateRequest(
                    $"day-{day}-request-{index}",
                    firstDay.AddDays(day).AddHours(7).AddMinutes(index * 25))))
            .ToArray();

        var result = IosLessonNotificationScheduleSelector.Select(requests, 60);

        Assert.Equal(60, result.Count);
        for (var day = 0; day < 7; day++)
        {
            var date = DateOnly.FromDateTime(firstDay.AddDays(day).LocalDateTime);
            var selectedForDay = result
                .Where(x => DateOnly.FromDateTime(x.FireAt.LocalDateTime) == date)
                .ToArray();
            Assert.NotEmpty(selectedForDay);
            Assert.Equal(
                requests.Where(x =>
                        DateOnly.FromDateTime(x.FireAt.LocalDateTime) == date)
                    .Min(x => x.FireAt),
                selectedForDay.Min(x => x.FireAt));
            Assert.Equal(
                requests.Where(x =>
                        DateOnly.FromDateTime(x.FireAt.LocalDateTime) == date)
                    .Max(x => x.FireAt),
                selectedForDay.Max(x => x.FireAt));
        }
    }

    [Fact]
    public void Select_NeverSplitsPreparationAndOnClassChain()
    {
        var firstDay = new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero);
        var requests = Enumerable.Range(0, 7)
            .SelectMany(day => Enumerable.Range(0, 12)
                .SelectMany(index => CreateLessonChain(
                    firstDay.AddDays(day).AddHours(8).AddMinutes(index * 35),
                    $"day-{day}-lesson-{index}")))
            .ToArray();

        var result = IosLessonNotificationScheduleSelector.Select(requests, 60);

        Assert.Equal(60, result.Count);
        foreach (var chain in requests.GroupBy(x => x.ChainId))
        {
            Assert.Contains(result.Count(x => x.ChainId == chain.Key), [0, 2]);
        }
        Assert.Equal(
            7,
            result.Select(x => DateOnly.FromDateTime(x.FireAt.LocalDateTime))
                .Distinct()
                .Count());
    }

    [Fact]
    public void Select_FullyCoversNearTermDaysBeforeSpreadingRemainingCapacity()
    {
        var firstDay = new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero);
        var requests = Enumerable.Range(0, 40)
            .SelectMany(day =>
            {
                var start = firstDay.AddDays(day).AddHours(8);
                return CreateLessonChain(start, $"day-{day}.lesson")
                    .Append(CreateRequest(
                        $"day-{day}.break",
                        start.AddHours(1),
                        IosNotificationSchedulingPolicy.OnBreakingChannelId));
            })
            .ToArray();

        var result = IosLessonNotificationScheduleSelector.Select(requests, 60);

        Assert.Equal(60, result.Count);
        foreach (var day in Enumerable.Range(
                     0,
                     IosLessonNotificationScheduleSelector.FullyCoveredNearTermDayCount))
        {
            var date = DateOnly.FromDateTime(firstDay.AddDays(day).LocalDateTime);
            Assert.Equal(
                3,
                result.Count(x =>
                    DateOnly.FromDateTime(x.FireAt.LocalDateTime) == date));
        }

        Assert.Contains(
            result,
            x => DateOnly.FromDateTime(x.FireAt.LocalDateTime) ==
                 DateOnly.FromDateTime(firstDay.AddDays(39).LocalDateTime));
        foreach (var chain in requests.Where(x => x.ChainId != null).GroupBy(x => x.ChainId))
        {
            Assert.Contains(result.Count(x => x.ChainId == chain.Key), [0, 2]);
        }
    }

    [Fact]
    public void Select_PrioritizesClassChainsOverBreakNotifications()
    {
        var firstDay = new DateTimeOffset(2026, 7, 13, 8, 0, 0, TimeSpan.Zero);
        var requests = CreateLessonChain(firstDay, "first.lesson")
            .Concat(CreateLessonChain(firstDay.AddHours(2), "second.lesson"))
            .Append(CreateRequest(
                "midday.break",
                firstDay.AddHours(1),
                IosNotificationSchedulingPolicy.OnBreakingChannelId))
            .ToArray();

        var result = IosLessonNotificationScheduleSelector.Select(requests, 4);

        Assert.Equal(4, result.Count);
        Assert.DoesNotContain(
            result,
            x => x.ChannelId == IosNotificationSchedulingPolicy.OnBreakingChannelId);
        Assert.Equal(
            ["first.lesson", "second.lesson"],
            result.Select(x => x.ChainId).Distinct());
    }

    [Fact]
    public void Select_WithOneSlotRemaining_DoesNotSplitAnotherChain()
    {
        var firstDay = new DateTimeOffset(2026, 7, 13, 8, 0, 0, TimeSpan.Zero);
        var requests = CreateLessonChain(firstDay, "first.lesson")
            .Concat(CreateLessonChain(firstDay.AddHours(2), "second.lesson"))
            .ToArray();

        var result = IosLessonNotificationScheduleSelector.Select(requests, 3);

        Assert.Equal(2, result.Count);
        Assert.All(result, x => Assert.Equal("first.lesson", x.ChainId));
    }

    [Fact]
    public void Select_KeepsSmallScheduleOrdered()
    {
        var now = new DateTimeOffset(2026, 7, 13, 8, 0, 0, TimeSpan.Zero);
        var later = CreateRequest("later", now.AddHours(1));
        var earlier = CreateRequest("earlier", now);

        Assert.Equal(
            [earlier, later],
            IosLessonNotificationScheduleSelector.Select([later, earlier], 60));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Select_WithNonPositiveMaximum_Throws(int maximumCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            IosLessonNotificationScheduleSelector.Select([], maximumCount));
    }

    [Fact]
    public void Select_WithNullRequests_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            IosLessonNotificationScheduleSelector.Select(null!, 60));
    }

    [Theory]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, true)]
    public void ShouldRequestAuthorization_RequiresAppProviderAndChannel(
        bool appEnabled,
        bool providerEnabled,
        bool channelEnabled,
        bool expected)
    {
        Assert.Equal(
            expected,
            IosNotificationSchedulingPolicy.ShouldRequestAuthorization(
                appEnabled,
                providerEnabled,
                [channelEnabled]));
    }

    [Fact]
    public void CanCompleteQueueTicket_RequiresProviderChannelAndMatchingTime()
    {
        var fireAt = new DateTimeOffset(2026, 7, 13, 8, 0, 0, TimeSpan.Zero);
        var scheduled = new[]
        {
            CreateRequest(
                "scheduled",
                fireAt,
                IosNotificationSchedulingPolicy.OnClassChannelId)
        };

        Assert.True(IosNotificationSchedulingPolicy.CanCompleteQueueTicket(
            IosNotificationSchedulingPolicy.ClassNotificationProviderId,
            IosNotificationSchedulingPolicy.OnClassChannelId,
            fireAt.LocalDateTime.AddSeconds(30),
            scheduled,
            TimeSpan.FromMinutes(1)));
        Assert.False(IosNotificationSchedulingPolicy.CanCompleteQueueTicket(
            Guid.NewGuid(),
            IosNotificationSchedulingPolicy.OnClassChannelId,
            fireAt.LocalDateTime,
            scheduled,
            TimeSpan.FromMinutes(1)));
        Assert.False(IosNotificationSchedulingPolicy.CanCompleteQueueTicket(
            IosNotificationSchedulingPolicy.ClassNotificationProviderId,
            IosNotificationSchedulingPolicy.OnBreakingChannelId,
            fireAt.LocalDateTime,
            scheduled,
            TimeSpan.FromMinutes(1)));
        Assert.False(IosNotificationSchedulingPolicy.CanCompleteQueueTicket(
            IosNotificationSchedulingPolicy.ClassNotificationProviderId,
            IosNotificationSchedulingPolicy.OnClassChannelId,
            fireAt.LocalDateTime.AddMinutes(2),
            scheduled,
            TimeSpan.FromMinutes(1)));
    }

    [Theory]
    [InlineData(2, 8)]
    [InlineData(-2, 12)]
    public void GetExpectedQueueTicketLocalFireTime_MapsLogicalOffsets(
        int logicalOffsetMinutes,
        int expectedSystemMinute)
    {
        var systemNow = new DateTimeOffset(2026, 7, 13, 8, 0, 0, TimeSpan.Zero);
        var logicalNow = systemNow.DateTime.AddMinutes(logicalOffsetMinutes);
        var logicalClassStart = new DateTime(2026, 7, 13, 8, 10, 0);

        var result = IosNotificationSchedulingPolicy
            .GetExpectedQueueTicketLocalFireTime(
                IosNotificationSchedulingPolicy.OnClassChannelId,
                true,
                logicalClassStart,
                logicalNow,
                systemNow);

        Assert.Equal(
            new DateTime(2026, 7, 13, 8, expectedSystemMinute, 0),
            result);
    }

    private static IosLessonNotificationRequest CreateRequest(
        string identifier,
        DateTimeOffset fireAt,
        Guid? channelId = null,
        string? chainId = null) =>
        new(
            identifier,
            fireAt,
            "title",
            "body",
            channelId ?? IosNotificationSchedulingPolicy.PrepareOnClassChannelId,
            true,
            ChainId: chainId);

    private static IosLessonNotificationRequest[] CreateLessonChain(
        DateTimeOffset start,
        string chainId) =>
    [
        CreateRequest(
            $"{chainId}.prepare",
            start.AddMinutes(-5),
            IosNotificationSchedulingPolicy.PrepareOnClassChannelId,
            chainId),
        CreateRequest(
            $"{chainId}.on",
            start,
            IosNotificationSchedulingPolicy.OnClassChannelId,
            chainId)
    ];
}
