using ClassIsland.Models;
using ClassIsland.Models.AttachedSettings;
using ClassIsland.Platforms.Abstraction.Models.LiveActivities;
using ClassIsland.Services.LiveActivities;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;
using Xunit;

namespace ClassIsland.Lessons.Tests;

public sealed class LessonLiveActivitySettingsPolicyTests
{
    private static readonly DateTimeOffset LessonStart = new(2026, 11, 7, 8, 0, 0, TimeSpan.FromHours(8));

    [Theory]
    [InlineData(LessonLiveActivityPhase.None, 1)]
    [InlineData(LessonLiveActivityPhase.None, 20)]
    [InlineData(LessonLiveActivityPhase.Breaking, 20)]
    [InlineData(LessonLiveActivityPhase.Breaking, 1440)]
    public void Upcoming_UsesItsOwnLeadWindowIncludingBreaks(LessonLiveActivityPhase phase, int leadMinutes)
    {
        var settings = new Settings { UpcomingClassRealtimeNotificationLeadMinutes = leadMinutes };
        var content = Upcoming(phase);
        var boundary = LessonStart.AddMinutes(-leadMinutes);

        Assert.Equal(boundary, LessonLiveActivitySettingsPolicy.GetPublicationTime(content, settings));
        Assert.False(LessonLiveActivitySettingsPolicy.ShouldPublish(content, settings, boundary.AddTicks(-1)));
        Assert.True(LessonLiveActivitySettingsPolicy.ShouldPublish(content, settings, boundary));
        Assert.True(LessonLiveActivitySettingsPolicy.ShouldPublish(content, settings, LessonStart.AddTicks(-1)));
        Assert.False(LessonLiveActivitySettingsPolicy.ShouldPublish(content, settings, LessonStart));
        Assert.False(LessonLiveActivitySettingsPolicy.ShouldPublish(content, settings, LessonStart.AddMinutes(1)));
    }

    [Theory]
    [InlineData(false, 20)]
    [InlineData(true, 0)]
    public void Upcoming_DisabledOrZeroLeadNeverPublishes(bool enabled, int leadMinutes)
    {
        var settings = new Settings
        {
            IsUpcomingClassRealtimeNotificationEnabled = enabled,
            UpcomingClassRealtimeNotificationLeadMinutes = leadMinutes
        };

        Assert.Null(LessonLiveActivitySettingsPolicy.GetPublicationTime(Upcoming(), settings));
        Assert.False(LessonLiveActivitySettingsPolicy.ShouldPublish(Upcoming(), settings, LessonStart.AddMinutes(-1)));
        Assert.False(LessonLiveActivitySettingsPolicy.ShouldPublish(
            Upcoming(LessonLiveActivityPhase.Breaking), settings, LessonStart.AddMinutes(-1)));
    }

    [Theory]
    [InlineData(LessonLiveActivityPhase.OnClass)]
    [InlineData(LessonLiveActivityPhase.Breaking)]
    public void Current_UsesCurrentSwitchWithoutUpcomingWindow(LessonLiveActivityPhase phase)
    {
        var settings = new Settings { IsUpcomingClassRealtimeNotificationEnabled = false };
        var content = Upcoming(phase) with { IsUpcomingLesson = false };

        Assert.Null(LessonLiveActivitySettingsPolicy.GetPublicationTime(content, settings));
        Assert.True(LessonLiveActivitySettingsPolicy.ShouldPublish(content, settings, LessonStart));
        settings.IsCurrentClassRealtimeNotificationEnabled = false;
        Assert.False(LessonLiveActivitySettingsPolicy.ShouldPublish(content, settings, LessonStart));
    }

    [Fact]
    public void InactiveOrInvalidIntervalsDoNotPublish()
    {
        var settings = new Settings();
        var upcoming = Upcoming();

        Assert.False(LessonLiveActivitySettingsPolicy.ShouldPublish(
            upcoming with { Phase = LessonLiveActivityPhase.AfterSchool }, settings, LessonStart));
        Assert.False(LessonLiveActivitySettingsPolicy.ShouldPublish(
            upcoming with { EndTime = null }, settings, LessonStart));
        Assert.False(LessonLiveActivitySettingsPolicy.ShouldPublish(
            upcoming with { EndTime = upcoming.StartTime }, settings, LessonStart));
        Assert.Null(LessonLiveActivitySettingsPolicy.GetPublicationTime(upcoming with { EndTime = null }, settings));
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 4)]
    [InlineData(true, 8)]
    [InlineData(true, 16)]
    [InlineData(true, 31)]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 4)]
    [InlineData(false, 8)]
    [InlineData(false, 16)]
    [InlineData(false, 31)]
    public void DisplayOptions_SelectEachFlagFromTheRelevantGroup(bool isUpcoming, int flags)
    {
        var settings = new Settings();
        if (isUpcoming)
        {
            settings.UpcomingClassRealtimeNotificationShowSubjectName = (flags & 1) != 0;
            settings.UpcomingClassRealtimeNotificationShowRemainingTime = (flags & 2) != 0;
            settings.UpcomingClassRealtimeNotificationShowTimeRange = (flags & 4) != 0;
            settings.UpcomingClassRealtimeNotificationShowLocation = (flags & 8) != 0;
            settings.UpcomingClassRealtimeNotificationShowTeacherName = (flags & 16) != 0;
        }
        else
        {
            settings.CurrentClassRealtimeNotificationShowSubjectName = (flags & 1) != 0;
            settings.CurrentClassRealtimeNotificationShowRemainingTime = (flags & 2) != 0;
            settings.CurrentClassRealtimeNotificationShowTimeRange = (flags & 4) != 0;
            settings.CurrentClassRealtimeNotificationShowLocation = (flags & 8) != 0;
            settings.CurrentClassRealtimeNotificationShowTeacherName = (flags & 16) != 0;
        }

        var options = LessonLiveActivitySettingsPolicy.GetDisplayOptions(settings, isUpcoming);

        Assert.Equal((flags & 1) != 0, options.ShowSubjectName);
        Assert.Equal((flags & 2) != 0, options.ShowRemainingTime);
        Assert.Equal((flags & 4) != 0, options.ShowTimeRange);
        Assert.Equal((flags & 8) != 0, options.ShowLocation);
        Assert.Equal((flags & 16) != 0, options.ShowTeacherName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EffectiveSettings_UseAndroidAttachmentPriorityAndGlobalFallback(bool isUpcoming)
    {
        var settings = new Settings();
        var subject = new Subject();
        var item = new TimeLayoutItem();
        var layout = new TimeLayout();
        var layoutId = Guid.NewGuid();
        var profile = new Profile();
        profile.TimeLayouts.Add(layoutId, layout);
        var plan = new ClassPlan { TimeLayoutId = layoutId };
        profile.ClassPlans = new() { { Guid.NewGuid(), plan } };
        var lessons = new TestLessonsService
        {
            CurrentClassPlan = plan,
            CurrentSubject = isUpcoming ? new Subject() : subject,
            NextClassSubject = isUpcoming ? subject : new Subject(),
            CurrentTimeLayoutItem = isUpcoming ? new TimeLayoutItem() : item,
            NextClassTimeLayoutItem = isUpcoming ? item : new TimeLayoutItem()
        };
        AttachableSettingsObject[] targets = [subject, item, plan, layout];
        var attached = targets.Select((target, index) =>
        {
            var value = new RealtimeNotificationAttachedSettings
            {
                IsAttachSettingsEnabled = true,
                UpcomingClassRealtimeNotificationLeadMinutes = index + 1,
                IsUpcomingClassRealtimeNotificationEnabled = false
            };
            target.WriteAttachedObject(Guid.Parse(RealtimeNotificationAttachedSettings.SettingsId), value);
            return value;
        }).ToArray();

        foreach (var expected in attached)
        {
            var selected = LessonLiveActivitySettingsPolicy.GetEffectiveSettings(settings, lessons, isUpcoming);
            Assert.Same(expected, selected);
            Assert.False(LessonLiveActivitySettingsPolicy.ShouldPublish(Upcoming(), selected, LessonStart.AddSeconds(-1)));
            expected.IsAttachSettingsEnabled = false;
        }

        Assert.Same(settings, LessonLiveActivitySettingsPolicy.GetEffectiveSettings(settings, lessons, isUpcoming));
        Assert.Same(settings, LessonLiveActivitySettingsPolicy.GetEffectiveSettings(settings, new TestLessonsService(), isUpcoming));
    }

    private static LessonLiveActivityContent Upcoming(LessonLiveActivityPhase phase = LessonLiveActivityPhase.None) =>
        new("lesson", phase, "下一节 · 数学", "", "", "数",
            LessonStart.AddMinutes(-30), LessonStart, IsUpcomingLesson: true);
}
