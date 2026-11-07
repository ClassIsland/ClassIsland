using System.Globalization;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Icons;
using ClassIsland.Core.Models.Weather;
using ClassIsland.Models;
using ClassIsland.Models.LiveActivities;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClassIsland.Lessons.Tests;

[Collection(nameof(LiveActivitySettingsCollection))]
public sealed class LiveActivityPreviewTests
{
    [Fact]
    public void DefaultLayout_UsesTheSameHeaderOptionsAsTheActivity()
    {
        using var environment = new PreviewEnvironment(new Settings());
        var preview = environment.ViewModel;

        Assert.Equal("数学", preview.PreviewTitle);
        Assert.True(preview.PreviewShowPhase);
        Assert.True(preview.PreviewShowRemainingTime);
        Assert.Equal("32:18", preview.PreviewRemainingTime);
        Assert.Equal(762.0 / 2700 * 100, preview.PreviewProgressValue);
        Assert.False(preview.PreviewHasMetadata);
        Assert.False(preview.PreviewHasCustomLayout);
        Assert.False(preview.PreviewHasCustomRows);
        Assert.Empty(preview.PreviewItems);
        Assert.Empty(preview.PreviewRows);
    }

    [Fact]
    public void CustomizedEmptyLayout_DoesNotFallBackToTheDefaultLayout()
    {
        using var environment = new PreviewEnvironment(Customized());

        Assert.True(environment.ViewModel.PreviewHasCustomLayout);
        Assert.False(environment.ViewModel.PreviewHasCustomRows);
        Assert.Empty(environment.ViewModel.PreviewItems);
        Assert.Empty(environment.ViewModel.PreviewRows);
    }

    [Fact]
    public void CustomizedDefaults_UseNativeRowsAndRealComponentContent()
    {
        var settings = new Settings { IsLiveActivityLayoutCustomized = true };
        using var environment = new PreviewEnvironment(settings);
        var preview = environment.ViewModel;

        Assert.True(preview.PreviewHasCustomRows);
        Assert.Collection(preview.PreviewRows,
            row => Assert.Equal("schedule", Assert.Single(row.Items).Label),
            row => Assert.Equal(new[] { "date", "weather" }, row.Items.Select(x => x.Label)),
            row => Assert.Equal("countdown", Assert.Single(row.Items).Label));
        Assert.Equal(new[] { 1, 2, 1 }, preview.PreviewRows.Select(x => x.Columns));
        Assert.Collection(preview.PreviewItems,
            item =>
            {
                Assert.True(item.IsSchedule);
                Assert.True(item.ShowProgress);
                Assert.Equal("数学", item.Text);
                Assert.Empty(item.SecondaryText);
                Assert.Equal("下一节：物理", item.ExtraText);
                Assert.Equal(FluentIcons.BookFilled, item.Glyph);
            },
            item =>
            {
                Assert.Equal(DateTime.Today.ToString("M月d日 ddd", CultureInfo.GetCultureInfo("zh-CN")), item.Text);
                Assert.Equal(FluentIcons.CalendarRegular, item.Glyph);
            },
            item =>
            {
                Assert.Equal("天气暂不可用", item.Text);
                Assert.Empty(item.SecondaryText);
                Assert.Equal(FluentIcons.WeatherCloudyRegular, item.Glyph);
            },
            item =>
            {
                Assert.True(item.IsCountdown);
                Assert.Equal("剩余时间", item.Text);
                Assert.Equal("32:18", item.ValueText);
                Assert.Equal(FluentIcons.HourglassRegular, item.Glyph);
            });
    }

    [Fact]
    public void Text_UsesRuntimeTruncationAndSkipsEmptyComponents()
    {
        var text = string.Concat(Enumerable.Repeat("👩🏽‍💻", 60));
        using var environment = new PreviewEnvironment(Customized(
            new() { Kind = LiveActivityComponentKind.Text, Text = $"  {text}  " },
            new() { Kind = LiveActivityComponentKind.Text, Text = "   " }));

        var item = Assert.Single(environment.ViewModel.PreviewItems);
        Assert.Equal(string.Concat(Enumerable.Repeat("👩🏽‍💻", 47)) + "…", item.Text);
        Assert.Single(Assert.Single(environment.ViewModel.PreviewRows).Items);
    }

    [Fact]
    public void ShortComponents_ShareColumnsAndTextOccupiesItsOwnRow()
    {
        using var environment = new PreviewEnvironment(Customized(
            new() { Kind = LiveActivityComponentKind.Date },
            new() { Kind = LiveActivityComponentKind.Separator },
            new() { Kind = LiveActivityComponentKind.Text, Text = "记得带计算器" },
            new() { Kind = LiveActivityComponentKind.Clock },
            new() { Kind = LiveActivityComponentKind.Weather }));

        Assert.Collection(environment.ViewModel.PreviewRows,
            row =>
            {
                Assert.Equal(new[] { "date", "separator" }, row.Items.Select(x => x.Label));
                Assert.True(row.Items[1].IsSeparator);
            },
            row => Assert.Equal("text", Assert.Single(row.Items).Label),
            row =>
            {
                Assert.Equal(new[] { "clock", "weather" }, row.Items.Select(x => x.Label));
                Assert.Equal("14:22:42", row.Items[0].Text);
            });
    }

    [Fact]
    public void TextRows_StopAtTheNativeExpandedHeightBudget()
    {
        var components = Enumerable.Range(0, 6).Select(index => new LiveActivityComponentSettings
        {
            Kind = LiveActivityComponentKind.Text,
            Text = $"文本 {index}"
        }).ToArray();
        using var environment = new PreviewEnvironment(Customized(components));

        Assert.Equal(4, environment.ViewModel.PreviewRows.Count);
        Assert.Equal(new[] { "文本 0", "文本 1", "文本 2", "文本 3" },
            environment.ViewModel.PreviewItems.Select(x => x.Text));
    }

    [Theory]
    [InlineData(true, 3)]
    [InlineData(false, 4)]
    public void ScheduleRows_ReserveSpaceForProgress(bool showProgress, int expectedRows)
    {
        var components = Enumerable.Range(0, 6).Select(_ => new LiveActivityComponentSettings
        {
            Kind = LiveActivityComponentKind.Schedule,
            ShowProgress = showProgress
        }).ToArray();
        using var environment = new PreviewEnvironment(Customized(components));

        Assert.Equal(expectedRows, environment.ViewModel.PreviewRows.Count);
        Assert.All(environment.ViewModel.PreviewRows, row => Assert.Single(row.Items));
        Assert.All(environment.ViewModel.PreviewItems, item => Assert.Equal(showProgress, item.ShowProgress));
    }

    [Fact]
    public void DisplayOptions_ImmediatelyUpdateTheHeaderAndSchedule()
    {
        using var environment = new PreviewEnvironment(new Settings { IsLiveActivityLayoutCustomized = true });
        var preview = environment.ViewModel;
        var settings = environment.Settings;

        settings.CurrentClassRealtimeNotificationShowSubjectName = false;
        settings.CurrentClassRealtimeNotificationShowRemainingTime = false;
        settings.CurrentClassRealtimeNotificationShowLocation = true;
        settings.CurrentClassRealtimeNotificationShowTimeRange = true;
        settings.CurrentClassRealtimeNotificationShowTeacherName = true;

        Assert.Equal("正在上课", preview.PreviewTitle);
        Assert.False(preview.PreviewShowPhase);
        Assert.False(preview.PreviewShowRemainingTime);
        Assert.Empty(preview.PreviewRemainingTime);
        Assert.True(preview.PreviewHasMetadata);
        Assert.Equal("A301", preview.PreviewLocation);
        Assert.Equal("张老师", preview.PreviewTeacherName);
        Assert.Equal("14:10–14:55", preview.PreviewTimeRange);
        var schedule = Assert.Single(preview.PreviewItems, x => x.IsSchedule);
        Assert.Empty(schedule.Text);
        Assert.Equal("A301 · 14:10–14:55 · 张老师", schedule.SecondaryText);
        Assert.Equal("下一节：15:10–15:55", schedule.ExtraText);
        Assert.DoesNotContain(preview.PreviewItems, item => item.IsCountdown);

        settings.CurrentClassRealtimeNotificationShowLocation = false;
        settings.CurrentClassRealtimeNotificationShowTimeRange = false;
        settings.CurrentClassRealtimeNotificationShowTeacherName = false;

        Assert.False(preview.PreviewHasMetadata);
        schedule = Assert.Single(preview.PreviewItems, x => x.IsSchedule);
        Assert.Empty(schedule.SecondaryText);
        Assert.Empty(schedule.ExtraText);
        Assert.True(schedule.ShowProgress);
    }

    [Fact]
    public void CustomCountdown_RemainsVisibleWhenCourseRemainingTimeIsHidden()
    {
        var settings = Customized(
            new() { Kind = LiveActivityComponentKind.Countdown },
            new()
            {
                Kind = LiveActivityComponentKind.Countdown,
                UseCustomCountdown = true,
                CountdownName = "考试",
                TargetDate = DateTime.Today
            });
        settings.CurrentClassRealtimeNotificationShowRemainingTime = false;
        using var environment = new PreviewEnvironment(settings);

        var countdown = Assert.Single(environment.ViewModel.PreviewItems);
        Assert.True(countdown.IsCountdown);
        Assert.Equal("考试", countdown.Text);
        Assert.Equal("已到日期", countdown.ValueText);
    }

    [Fact]
    public void CustomCountdown_UsesTotalHoursAndTheConfiguredTargetDate()
    {
        var target = DateTime.Today.AddDays(2).AddHours(17);
        using var environment = new PreviewEnvironment(Customized(new LiveActivityComponentSettings
        {
            Kind = LiveActivityComponentKind.Countdown,
            UseCustomCountdown = true,
            TargetDate = target
        }));
        var expected = new DateTimeOffset(target.Date) - DateTimeOffset.Now;
        var parts = Assert.Single(environment.ViewModel.PreviewItems).ValueText.Split(':');
        var actual = TimeSpan.FromHours(int.Parse(parts[0])) + TimeSpan.FromMinutes(int.Parse(parts[1])) +
                     TimeSpan.FromSeconds(int.Parse(parts[2]));

        Assert.InRange(Math.Abs((actual - expected).TotalSeconds), 0, 2);
    }

    [Fact]
    public void Weather_UsesCurrentCachedWeatherAndUpdatesWhenTheCacheChanges()
    {
        var weather = IosLessonNotificationScheduleFactoryTests.CreateProxy<IWeatherService>((method, _) =>
            method.Name == nameof(IWeatherService.GetWeatherTextByCode)
                ? "晴"
                : throw new NotSupportedException(method.Name));
        var settings = Customized(new LiveActivityComponentSettings { Kind = LiveActivityComponentKind.Weather });
        using var environment = new PreviewEnvironment(settings, weather);

        Assert.Equal("天气暂不可用", Assert.Single(environment.ViewModel.PreviewItems).Text);

        settings.LastWeatherInfo = new WeatherInfo
        {
            UpdateTimeUnix = 1,
            Current = new CurrentWeather { Temperature = new ValueUnitPair { Value = "23", Unit = "°C" } }
        };

        var item = Assert.Single(environment.ViewModel.PreviewItems);
        Assert.Equal("23°C", item.Text);
        Assert.Equal("晴", item.SecondaryText);
        Assert.Equal(FluentIcons.WeatherSunnyRegular, item.Glyph);
    }

    [Fact]
    public void ComponentChanges_RefreshPreviewOrderAndEnabledState()
    {
        var date = new LiveActivityComponentSettings { Kind = LiveActivityComponentKind.Date };
        var text = new LiveActivityComponentSettings { Kind = LiveActivityComponentKind.Text, Text = "提醒" };
        using var environment = new PreviewEnvironment(Customized(date, text));
        var preview = environment.ViewModel;

        preview.MoveUpCommand.Execute(text);
        Assert.Equal(new[] { "text", "date" }, preview.PreviewItems.Select(x => x.Label));
        text.IsEnabled = false;
        Assert.Equal("date", Assert.Single(preview.PreviewItems).Label);
        date.IsEnabled = false;
        Assert.Empty(preview.PreviewRows);
        Assert.False(preview.PreviewHasCustomRows);
        Assert.True(preview.PreviewHasCustomLayout);
    }

    [Fact]
    public void SettingsReplacementAndDeactivation_StopRefreshingFromOldSubscriptions()
    {
        var oldSettings = Customized(new LiveActivityComponentSettings { Kind = LiveActivityComponentKind.Text, Text = "原配置" });
        using var environment = new PreviewEnvironment(oldSettings);
        var preview = environment.ViewModel;
        var replacement = Customized(new LiveActivityComponentSettings { Kind = LiveActivityComponentKind.Text, Text = "新配置" });

        preview.SettingsService.Settings = replacement;
        oldSettings.LiveActivityComponents[0].Text = "已弃用";
        Assert.Equal("新配置", Assert.Single(preview.PreviewItems).Text);
        replacement.LiveActivityComponents[0].Text = "新配置已修改";
        Assert.Equal("新配置已修改", Assert.Single(preview.PreviewItems).Text);

        preview.Deactivate();
        replacement.LiveActivityComponents[0].Text = "页面已关闭";
        Assert.Equal("新配置已修改", Assert.Single(preview.PreviewItems).Text);
    }

    private static Settings Customized(params LiveActivityComponentSettings[] components) => new()
    {
        IsLiveActivityLayoutCustomized = true,
        LiveActivityComponents = new(components)
    };

    private sealed class PreviewEnvironment : IDisposable
    {
        private readonly string _previousRoot = CommonDirectories.AppRootFolderPath;
        private string Root { get; } = Path.Combine(Path.GetTempPath(), $"classisland-live-activity-preview-{Guid.NewGuid():N}");

        public LiveActivitySettingsViewModel ViewModel { get; }
        public Settings Settings => ViewModel.SettingsService.Settings;

        public PreviewEnvironment(Settings settings, IWeatherService? weatherService = null)
        {
            Directory.CreateDirectory(Root);
            typeof(CommonDirectories).GetProperty(nameof(CommonDirectories.AppRootFolderPath))!.SetValue(null, Root);
            var settingsService = new SettingsService(NullLogger<SettingsService>.Instance, null!) { Settings = settings };
            ViewModel = new LiveActivitySettingsViewModel(settingsService, weatherService);
            ViewModel.Activate();
        }

        public void Dispose()
        {
            ViewModel.Deactivate();
            typeof(CommonDirectories).GetProperty(nameof(CommonDirectories.AppRootFolderPath))!.SetValue(null, _previousRoot);
            Directory.Delete(Root, true);
        }
    }
}
