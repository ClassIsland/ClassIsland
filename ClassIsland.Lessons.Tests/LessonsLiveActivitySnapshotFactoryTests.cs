using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Services.LiveActivities;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;
using Xunit;

namespace ClassIsland.Lessons.Tests;

public sealed class LessonsLiveActivitySnapshotFactoryTests
{
    [Theory]
    [InlineData(TimeState.OnClass, "B302", "B302")]
    [InlineData(TimeState.None, "B302", "B302")]
    [InlineData(TimeState.Breaking, "B302", "B302")]
    [InlineData(TimeState.OnClass, "", "A301")]
    [InlineData(TimeState.None, "   ", "A301")]
    public void Create_UsesEffectiveCourseLocation(TimeState state, string location, string expected)
    {
        var subject = new Subject { Name = "数学", Location = "A301" };
        var classInfo = new ClassInfo { Subject = subject, Location = location };
        var item = new TimeLayoutItem
        {
            StartTime = TimeSpan.FromHours(8),
            EndTime = TimeSpan.FromHours(8.75)
        };
        var now = new DateTime(2026, 10, 26, state == TimeState.OnClass ? 8 : 7, 10, 0);
        var lessons = new TestLessonsService
        {
            CurrentState = state,
            CurrentSubject = subject,
            NextClassSubject = subject,
            CurrentClassInfo = classInfo,
            NextClassInfo = classInfo,
            CurrentTimeLayoutItem = state == TimeState.OnClass ? item : TimeLayoutItem.Empty,
            NextClassTimeLayoutItem = item
        };
        var clock = IosLessonNotificationScheduleFactoryTests.CreateProxy<IExactTimeService>((method, _) => method.Name switch
        {
            "GetCurrentLocalDateTime" => now,
            _ => throw new NotSupportedException(method.Name)
        });
        var factory = new LessonsLiveActivitySnapshotFactory(lessons, clock);

        var snapshot = factory.Create();

        Assert.Equal(expected, snapshot.ActivityDetails?.Location);
        classInfo.Location = "C403";
        Assert.Equal("C403", factory.Create().ActivityDetails?.Location);
        classInfo.Location = "";
        subject.Location = "D504";
        Assert.Equal("D504", factory.Create().ActivityDetails?.Location);
    }
}
