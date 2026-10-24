using System.Reflection;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.iOS.Services.Notifications;
using ClassIsland.Models.AttachedSettings;
using ClassIsland.Models.NotificationProviderSettings;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Services;
using ClassIsland.Shared.Models.Profile;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClassIsland.Lessons.Tests;

public sealed class IosLessonNotificationScheduleFactoryTests
{
    [Theory]
    [InlineData("B302", "B302")]
    [InlineData("", "A301")]
    [InlineData("   ", "A301")]
    public void Create_UsesCourseLocationOverrideWithSubjectFallback(string location, string expected)
    {
        var fixture = new NotificationFixture();
        fixture.Class.Location = location;
        fixture.Provider.ShowLocation = true;
        fixture.Provider.ShowTeacherName = true;

        var requests = fixture.Factory.Create();

        Assert.Equal(2, requests.Count);
        Assert.All(requests, request =>
        {
            Assert.Contains(expected, request.Body);
            Assert.Contains("王老师", request.Body);
            Assert.Contains("08:00–08:45", request.Body);
        });
        if (expected != fixture.Subject.Location)
        {
            Assert.All(requests, request => Assert.DoesNotContain(fixture.Subject.Location, request.Body));
        }
    }

    [Fact]
    public void Create_SubjectAttachedSettingsOverrideGlobalDisplayOptions()
    {
        var fixture = new NotificationFixture();
        fixture.Provider.ShowLocation = true;
        fixture.Provider.ShowTeacherName = true;
        fixture.Subject.WriteAttachedObject(IosNotificationSchedulingPolicy.ClassNotificationProviderId,
            new ClassNotificationAttachedSettings
            {
                IsAttachSettingsEnabled = true,
                ShowClassTime = false,
                ShowTeacherName = false,
                ShowLocation = false
            });

        var requests = fixture.Factory.Create();

        Assert.Equal(2, requests.Count);
        Assert.All(requests, request =>
        {
            Assert.Contains("数学", request.Body);
            Assert.DoesNotContain("王老师", request.Body);
            Assert.DoesNotContain("A301", request.Body);
            Assert.DoesNotContain("08:00", request.Body);
        });
    }

    [Fact]
    public void Create_BreakNotificationUsesItsEffectiveDisplaySettingsAndNextCourseLocation()
    {
        var fixture = new NotificationFixture();
        fixture.Class.Location = "B302";
        var breakItem = new TimeLayoutItem
        {
            TimeType = 1,
            StartTime = TimeSpan.FromHours(7.5),
            EndTime = TimeSpan.FromHours(8)
        };
        breakItem.WriteAttachedObject(IosNotificationSchedulingPolicy.ClassNotificationProviderId,
            new ClassNotificationAttachedSettings
            {
                IsAttachSettingsEnabled = true,
                ShowTeacherName = true,
                ShowLocation = true,
                ShowClassTime = false
            });
        fixture.Layout.Layouts.Insert(0, breakItem);

        var request = Assert.Single(fixture.Factory.Create(),
            request => request.ChannelId == IosNotificationSchedulingPolicy.OnBreakingChannelId);

        Assert.Contains("B302", request.Body);
        Assert.Contains("王老师", request.Body);
        Assert.DoesNotContain("08:00", request.Body);
    }

    [Fact]
    public void Create_DisabledCourseRemovesAlreadyPlannedLessonNotifications()
    {
        var fixture = new NotificationFixture();
        Assert.Equal(2, fixture.Factory.Create().Count);

        fixture.Class.IsEnabled = false;

        Assert.Empty(fixture.Factory.Create());
    }

    [Fact]
    public void Create_ChangedLocationIsReflectedWithoutChangingNotificationIdentity()
    {
        var fixture = new NotificationFixture();
        fixture.Provider.ShowLocation = true;
        var before = fixture.Factory.Create();

        fixture.Class.Location = "B302";
        var after = fixture.Factory.Create();

        Assert.Equal(before.Select(request => request.Identifier), after.Select(request => request.Identifier));
        Assert.All(after, request => Assert.Contains("B302", request.Body));
    }

    private sealed class NotificationFixture
    {
        private static readonly DateTime LogicalNow = new(2026, 10, 26, 7, 0, 0);
        public Profile Profile { get; } = new();
        public Subject Subject { get; } = new() { Name = "数学", TeacherName = "王老师", Location = "A301" };
        public TimeLayout Layout { get; } = new()
        {
            Layouts = [new TimeLayoutItem
            {
                TimeType = 0,
                StartTime = TimeSpan.FromHours(8),
                EndTime = TimeSpan.FromHours(8.75)
            }]
        };
        public ClassPlan Plan { get; }
        public ClassInfo Class => Plan.Classes[0];
        public ClassNotificationSettings Provider { get; } = new();
        public IosLessonNotificationScheduleFactory Factory { get; }

        public NotificationFixture()
        {
            var subjectId = Guid.NewGuid();
            var layoutId = Guid.NewGuid();
            Profile.Subjects.Add(subjectId, Subject);
            Profile.TimeLayouts.Add(layoutId, Layout);
            Plan = new ClassPlan { TimeLayoutId = layoutId };
            Profile.ClassPlans = new() { { Guid.NewGuid(), Plan } };
            Class.SubjectId = subjectId;
            var lessons = new TestLessonsService
            {
                GetPlan = date => date.Date == LogicalNow.Date ? Plan : null
            };
            var profileService = new TestProfileService { Profile = Profile };
            var notifications = new TestNotificationHostService(Provider);
            var clock = CreateProxy<IExactTimeService>((method, _) => method.Name switch
            {
                "GetCurrentLocalDateTime" => LogicalNow,
                _ => throw new NotSupportedException(method.Name)
            });
            var settings = new SettingsService(NullLogger<SettingsService>.Instance, null!);
            settings.Settings.IsNotificationEnabled = true;
            Factory = new IosLessonNotificationScheduleFactory(lessons, profileService,
                notifications, settings, clock, new LessonPreparationNotificationTimeline());
        }
    }

    internal static T CreateProxy<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        var proxy = DispatchProxy.Create<T, CallbackDispatchProxy>();
        ((CallbackDispatchProxy)(object)proxy).Handler = handler;
        return proxy;
    }

    public class CallbackDispatchProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(targetMethod ?? throw new InvalidOperationException("Proxy method is unavailable."), args);
    }
}
