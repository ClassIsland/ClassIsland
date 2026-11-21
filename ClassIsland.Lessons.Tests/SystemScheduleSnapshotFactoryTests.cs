using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Services.SystemIntegration;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;
using Xunit;

namespace ClassIsland.Lessons.Tests;

public sealed class SystemScheduleSnapshotFactoryTests
{
    [Fact]
    public void Create_UsesSevenLogicalDaysAndMapsLessonTimesToSystemClock()
    {
        var fixture = new SnapshotFixture();
        var queriedDates = new List<DateTime>();
        var factory = fixture.CreateFactory(date =>
        {
            queriedDates.Add(date);
            return date == SnapshotFixture.LogicalNow.Date ? fixture.Plan : null;
        });

        var snapshot = factory.Create(true, SnapshotFixture.SystemNow);

        Assert.Equal(Enumerable.Range(0, 7).Select(day => SnapshotFixture.LogicalNow.Date.AddDays(day)), queriedDates);
        Assert.Equal(SnapshotFixture.SystemNow.ToUnixTimeSeconds(), snapshot.GeneratedAt);
        Assert.Equal(SnapshotFixture.SystemNow.AddDays(7).AddHours(-7).ToUnixTimeSeconds(), snapshot.ValidUntil);
        Assert.True(snapshot.NotificationsEnabled);
        var lesson = Assert.Single(snapshot.Lessons);
        Assert.Equal(SnapshotFixture.SystemNow.AddHours(1).ToUnixTimeSeconds(), lesson.StartAt);
        Assert.Equal(SnapshotFixture.SystemNow.AddHours(1.75).ToUnixTimeSeconds(), lesson.EndAt);
        Assert.Equal("2026-10-26/0", lesson.Id);
        Assert.Equal("classisland://app/system/schedule?date=2026-10-26&lesson=0", lesson.Url);
    }

    [Theory]
    [InlineData("B302", "B302")]
    [InlineData("", "A301")]
    [InlineData("   ", "A301")]
    public void Create_UsesCourseLocationBeforeSubjectFallback(string location, string expected)
    {
        var fixture = new SnapshotFixture();
        fixture.Plan.Classes[0].Location = location;

        var snapshot = fixture.CreateFactory().Create(false, SnapshotFixture.SystemNow);

        var lesson = Assert.Single(snapshot.Lessons);
        Assert.Equal("数学", lesson.Subject);
        Assert.Equal(expected, lesson.Location);
        Assert.False(snapshot.NotificationsEnabled);
    }

    [Fact]
    public void Create_MissingSubjectUsesUserFacingFallbacks()
    {
        var fixture = new SnapshotFixture();
        fixture.Plan.Classes[0].SubjectId = Guid.NewGuid();

        var lesson = Assert.Single(fixture.CreateFactory().Create(true, SnapshotFixture.SystemNow).Lessons);

        Assert.Equal("未命名课程", lesson.Subject);
        Assert.Equal("", lesson.Location);
    }

    [Fact]
    public void Create_SkipsDisabledPlansAndPlansWithoutLayout()
    {
        var fixture = new SnapshotFixture();
        fixture.Plan.IsEnabled = false;
        var noLayout = new ClassPlan();
        var factory = fixture.CreateFactory(date => date.Day % 2 == 0 ? fixture.Plan : noLayout);

        Assert.Empty(factory.Create(true, SnapshotFixture.SystemNow).Lessons);
    }

    [Fact]
    public void Create_SkipsDisabledCoursesBreaksAndNonPositiveIntervals()
    {
        var fixture = new SnapshotFixture(
            Lesson(8, 8.75),
            new TimeLayoutItem { TimeType = 1, StartTime = TimeSpan.FromHours(8.75), EndTime = TimeSpan.FromHours(9) },
            Lesson(9, 9.75),
            Lesson(10, 10),
            Lesson(11, 10.75));
        fixture.Plan.Classes[1].IsEnabled = false;

        var lesson = Assert.Single(fixture.CreateFactory().Create(true, SnapshotFixture.SystemNow).Lessons);

        Assert.Equal("2026-10-26/0", lesson.Id);
    }

    [Fact]
    public void Create_ScheduleModePreservesGuidIdentityAfterDisabledCourse()
    {
        var fixture = new SnapshotFixture(Lesson(8, 8.75), Lesson(9, 9.75));
        fixture.Profile.ScheduleType = ScheduleType.Schedule;
        fixture.Plan.Classes[0].IsEnabled = false;
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        OrderedDictionary<Guid, ScheduleItem> items = new()
        {
            [firstId] = new ScheduleItem(),
            [secondId] = new ScheduleItem()
        };

        var snapshot = fixture.CreateFactory(getSchedule: _ => items).Create(true, SnapshotFixture.SystemNow);

        var lesson = Assert.Single(snapshot.Lessons);
        Assert.Equal($"2026-10-26/{secondId:D}", lesson.Id);
        Assert.Equal($"classisland://app/system/schedule?date=2026-10-26&lesson={secondId:D}", lesson.Url);
    }

    [Fact]
    public void Create_ClassPlansExposeOnlyEnabledPermanentPlans()
    {
        var fixture = new SnapshotFixture();
        fixture.Profile.ClassPlans.Clear();
        var id = Guid.NewGuid();
        fixture.Profile.ClassPlans.Add(id, new ClassPlan { Name = " " });
        fixture.Profile.ClassPlans.Add(Guid.NewGuid(), new ClassPlan { Name = "禁用", IsEnabled = false });
        fixture.Profile.ClassPlans.Add(Guid.NewGuid(), new ClassPlan { Name = "临时", IsOverlay = true });

        var plan = Assert.Single(fixture.CreateFactory(_ => null).Create(true, SnapshotFixture.SystemNow).ClassPlans);

        Assert.Equal(id.ToString("D"), plan.Id);
        Assert.Equal("未命名课表", plan.Name);
        Assert.Equal($"classisland://app/system/view-plan?id={id:D}", plan.Url);
    }

    [Fact]
    public void Create_BoundsClassPlanCount()
    {
        var fixture = new SnapshotFixture();
        fixture.Profile.ClassPlans.Clear();
        for (var index = 0; index < 513; index++)
        {
            fixture.Profile.ClassPlans.Add(Guid.NewGuid(), new ClassPlan { Name = $"课表 {index}" });
        }

        var snapshot = fixture.CreateFactory(_ => null).Create(true, SnapshotFixture.SystemNow);

        Assert.Equal(512, snapshot.ClassPlans.Length);
    }

    [Fact]
    public void Create_BoundsAndOrdersLessonsAcrossTheWeek()
    {
        var items = Enumerable.Range(0, 300).Select(index => Lesson(8 + index / 1000.0, 9 + index / 1000.0)).ToArray();
        var fixture = new SnapshotFixture(items);

        var snapshot = fixture.CreateFactory(_ => fixture.Plan).Create(true, SnapshotFixture.SystemNow);

        Assert.Equal(2048, snapshot.Lessons.Length);
        Assert.Equal(snapshot.Lessons.OrderBy(lesson => lesson.StartAt).ThenBy(lesson => lesson.Id, StringComparer.Ordinal), snapshot.Lessons);
        Assert.Equal(2048, snapshot.Lessons.Select(lesson => lesson.Id).Distinct().Count());
    }

    private static TimeLayoutItem Lesson(double start, double end) => new()
    {
        TimeType = 0,
        StartTime = TimeSpan.FromHours(start),
        EndTime = TimeSpan.FromHours(end)
    };

    private sealed class SnapshotFixture
    {
        internal static readonly DateTime LogicalNow = new(2026, 10, 26, 7, 0, 0);
        private static readonly DateTime SystemDateTime = new(2026, 11, 8, 7, 0, 0);
        internal static readonly DateTimeOffset SystemNow = new(SystemDateTime, TimeZoneInfo.Local.GetUtcOffset(SystemDateTime));
        public Profile Profile { get; } = new();
        public ClassPlan Plan { get; }

        public SnapshotFixture(params TimeLayoutItem[] items)
        {
            var subjectId = Guid.NewGuid();
            var layoutId = Guid.NewGuid();
            Profile.Subjects.Add(subjectId, new Subject { Name = "数学", Location = "A301" });
            Profile.TimeLayouts.Add(layoutId, new TimeLayout { Layouts = [.. (items.Length == 0 ? [Lesson(8, 8.75)] : items)] });
            Plan = new ClassPlan { TimeLayoutId = layoutId };
            Profile.ClassPlans = new() { [Guid.NewGuid()] = Plan };
            foreach (var info in Plan.Classes)
            {
                info.SubjectId = subjectId;
            }
        }

        public SystemScheduleSnapshotFactory CreateFactory(
            Func<DateTime, ClassPlan?>? getPlan = null,
            Func<DateOnly, OrderedDictionary<Guid, ScheduleItem>>? getSchedule = null)
        {
            var lessons = new TestLessonsService
            {
                GetPlan = getPlan ?? (date => date == LogicalNow.Date ? Plan : null),
                GetSchedule = getSchedule ?? (_ => [])
            };
            var profileService = new TestProfileService { Profile = Profile };
            var clock = IosLessonNotificationScheduleFactoryTests.CreateProxy<IExactTimeService>((method, _) => method.Name switch
            {
                "GetCurrentLocalDateTime" => LogicalNow,
                _ => throw new NotSupportedException(method.Name)
            });
            return new SystemScheduleSnapshotFactory(lessons, profileService, clock);
        }
    }
}
