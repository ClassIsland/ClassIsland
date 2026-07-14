using ClassIsland.Platforms.Abstraction.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class LessonPreparationNotificationTimelineTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 14, 14, 26, 0, TimeSpan.FromHours(8));

    [Fact]
    public void FuturePreparation_UsesOriginalTimeWithoutRegistration()
    {
        var timeline = new LessonPreparationNotificationTimeline();
        var planned = Now.AddMinutes(4);
        var lessonStart = Now.AddMinutes(14);

        Assert.Equal(
            planned,
            timeline.GetLiveActivityPublicationTime(
                "lesson.prepare",
                planned,
                lessonStart,
                Now));
        Assert.Equal(
            planned,
            timeline.PlanNotification(
                "lesson.prepare",
                planned,
                lessonStart,
                Now));
    }

    [Fact]
    public void MissedPreparation_WaitsForPlannerAndSharesCatchUpTime()
    {
        var timeline = new LessonPreparationNotificationTimeline();
        var planned = Now.AddMinutes(-1);
        var lessonStart = Now.AddMinutes(10);

        Assert.Null(timeline.GetLiveActivityPublicationTime(
            "lesson.prepare",
            planned,
            lessonStart,
            Now));

        var catchUp = timeline.PlanNotification(
            "lesson.prepare",
            planned,
            lessonStart,
            Now);

        Assert.Equal(Now.AddSeconds(2), catchUp);
        Assert.Null(timeline.GetLiveActivityPublicationTime(
            "lesson.prepare",
            planned,
            lessonStart,
            Now.AddSeconds(1)));
        timeline.ConfirmNotificationScheduled("lesson.prepare", catchUp!.Value);
        Assert.Equal(
            catchUp,
            timeline.GetLiveActivityPublicationTime(
                "lesson.prepare",
                planned,
                lessonStart,
                Now.AddSeconds(1)));
        Assert.Equal(
            catchUp,
            timeline.PlanNotification(
                "lesson.prepare",
                planned,
                lessonStart,
                Now.AddSeconds(1)));
    }

    [Fact]
    public void CatchUp_IsNotCreatedWhenLessonStartsTooSoon()
    {
        var timeline = new LessonPreparationNotificationTimeline();

        Assert.Null(timeline.PlanNotification(
            "lesson.prepare",
            Now.AddMinutes(-1),
            Now.AddSeconds(2),
            Now));
        Assert.Null(timeline.GetLiveActivityPublicationTime(
            "lesson.prepare",
            Now.AddMinutes(-1),
            Now.AddSeconds(2),
            Now));
    }

    [Fact]
    public void ScheduledPreparation_RemainsAvailableAfterOriginalFireTime()
    {
        var timeline = new LessonPreparationNotificationTimeline();
        var planned = Now.AddMinutes(4);
        var lessonStart = Now.AddMinutes(14);

        Assert.Equal(
            planned,
            timeline.PlanNotification(
                "lesson.prepare",
                planned,
                lessonStart,
                Now));
        timeline.ConfirmNotificationScheduled("lesson.prepare", planned);

        Assert.Equal(
            planned,
            timeline.GetLiveActivityPublicationTime(
                "lesson.prepare",
                planned,
                lessonStart,
                planned.AddSeconds(2)));
    }

    [Fact]
    public void CatchUp_RoundsUpToWholeSecondForNativeCalendarTrigger()
    {
        var timeline = new LessonPreparationNotificationTimeline();
        var fractionalNow = Now.AddMilliseconds(250);

        var catchUp = timeline.PlanNotification(
            "lesson.prepare",
            Now.AddMinutes(-1),
            Now.AddMinutes(10),
            fractionalNow);

        Assert.Equal(Now.AddSeconds(3), catchUp);
    }

    [Fact]
    public void Reconcile_RemovesPreparationThatWasNotAcceptedForScheduling()
    {
        var timeline = new LessonPreparationNotificationTimeline();
        var planned = Now.AddMinutes(4);
        var lessonStart = Now.AddMinutes(14);
        timeline.PlanNotification("lesson.prepare", planned, lessonStart, Now);

        timeline.ReconcileScheduledNotifications([], Now);

        Assert.Null(timeline.GetLiveActivityPublicationTime(
            "lesson.prepare",
            planned,
            lessonStart,
            planned.AddSeconds(2)));
    }

    [Fact]
    public void Reconcile_PreservesConfirmedPreparationUntilLessonStarts()
    {
        var timeline = new LessonPreparationNotificationTimeline();
        var planned = Now.AddMinutes(4);
        var lessonStart = Now.AddMinutes(14);
        timeline.PlanNotification("lesson.prepare", planned, lessonStart, Now);
        timeline.ConfirmNotificationScheduled("lesson.prepare", planned);

        timeline.ReconcileScheduledNotifications([], planned.AddSeconds(1));

        Assert.Equal(
            planned,
            timeline.GetLiveActivityPublicationTime(
                "lesson.prepare",
                planned,
                lessonStart,
                planned.AddSeconds(2)));

        timeline.ReconcileScheduledNotifications([], lessonStart);
        Assert.Null(timeline.GetLiveActivityPublicationTime(
            "lesson.prepare",
            planned,
            lessonStart,
            lessonStart));
    }

    [Fact]
    public void ConfirmedPreparation_UsesStableIdentifierAcrossClockRemapping()
    {
        var timeline = new LessonPreparationNotificationTimeline();
        var planned = Now.AddMinutes(4);
        var lessonStart = Now.AddMinutes(14);
        timeline.PlanNotification("lesson.prepare", planned, lessonStart, Now);
        timeline.ConfirmNotificationScheduled("lesson.prepare", planned);

        Assert.Equal(
            planned,
            timeline.GetLiveActivityPublicationTime(
                "lesson.prepare",
                planned.AddSeconds(3),
                lessonStart.AddSeconds(3),
                planned.AddSeconds(4)));
    }

    [Fact]
    public void Restore_UsesActualFireTimeRecoveredFromNativeNotification()
    {
        var timeline = new LessonPreparationNotificationTimeline();
        var planned = Now.AddMinutes(-1);
        var lessonStart = Now.AddMinutes(10);
        timeline.PlanNotification("lesson.prepare", planned, lessonStart, Now);
        var deliveredAt = Now.AddSeconds(-2);

        timeline.RestoreNotificationScheduled("lesson.prepare", deliveredAt);

        Assert.Equal(
            deliveredAt,
            timeline.GetLiveActivityPublicationTime(
                "lesson.prepare",
                planned,
                lessonStart,
                Now));
    }

    [Fact]
    public void UnconfirmedCatchUp_IsReplannedAfterItsSafetyWindow()
    {
        var timeline = new LessonPreparationNotificationTimeline();
        var planned = Now.AddMinutes(-1);
        var lessonStart = Now.AddMinutes(10);
        var firstAttempt = timeline.PlanNotification(
            "lesson.prepare",
            planned,
            lessonStart,
            Now);

        var secondAttempt = timeline.PlanNotification(
            "lesson.prepare",
            planned,
            lessonStart,
            firstAttempt!.Value);

        Assert.Equal(firstAttempt.Value.AddSeconds(2), secondAttempt);
    }

    [Fact]
    public void CatchUp_DoesNotLeakAcrossIdentifiers()
    {
        var timeline = new LessonPreparationNotificationTimeline();
        var planned = Now.AddMinutes(-1);
        var lessonStart = Now.AddMinutes(10);

        timeline.PlanNotification("first.prepare", planned, lessonStart, Now);

        Assert.Null(timeline.GetLiveActivityPublicationTime(
            "second.prepare",
            planned,
            lessonStart,
            Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyIdentifier_IsRejected(string? identifier)
    {
        var timeline = new LessonPreparationNotificationTimeline();

        Assert.ThrowsAny<ArgumentException>(() => timeline.PlanNotification(
            identifier!,
            Now,
            Now.AddMinutes(1),
            Now));
    }
}
