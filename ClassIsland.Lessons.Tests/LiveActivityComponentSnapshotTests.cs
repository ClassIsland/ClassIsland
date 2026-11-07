using System.Globalization;
using ClassIsland.Core.Models.Weather;
using ClassIsland.Models;
using ClassIsland.Models.LiveActivities;
using ClassIsland.Services.LiveActivities;
using ClassIsland.Shared.Enums;
using ClassIsland.Platforms.Abstraction.Models.LiveActivities;
using Xunit;

namespace ClassIsland.Lessons.Tests;

public sealed class LiveActivityComponentSnapshotTests
{
    private static readonly DateTime Now = new(2026, 10, 12, 14, 20, 10);
    private static readonly LessonLiveActivityDetails Details = new("数学", "A301", "14:10–14:55", "");
    private static readonly LessonLiveActivityDetails Next = new("物理", "A302", "15:10–15:55", "");

    [Fact]
    public void Create_DisabledCustomizationUsesLegacyLayoutAndEmptyConfigurationStaysEmpty()
    {
        var settings = new Settings();
        Assert.Null(Create(settings));
        settings.IsLiveActivityLayoutCustomized = true;
        settings.LiveActivityComponents = [];
        Assert.Empty(Create(settings)!);
        settings.LiveActivityComponents.Add(new() { Kind = LiveActivityComponentKind.Schedule, IsEnabled = false });
        Assert.Empty(Create(settings)!);
    }

    [Fact]
    public void Create_PreservesComponentOrderAndSkipsBlankTextOrInvalidComponents()
    {
        var settings = Customized(
            new() { Kind = LiveActivityComponentKind.Text, Text = "   " },
            new() { Kind = LiveActivityComponentKind.Separator },
            new() { Kind = LiveActivityComponentKind.Text, Text = "  带计算器  " },
            new() { Kind = LiveActivityComponentKind.Schedule });
        settings.LiveActivityComponents.Add(new() { Kind = (LiveActivityComponentKind)999 });

        Assert.Collection(Create(settings)!,
            item => Assert.Equal("separator", item.Kind),
            item => Assert.Equal("带计算器", item.Text),
            item => Assert.Equal("schedule", item.Kind));
    }

    [Fact]
    public void Create_ScheduleOptionsControlNextLessonAndProgress()
    {
        var component = new LiveActivityComponentSettings { Kind = LiveActivityComponentKind.Schedule };
        var settings = Customized(component);
        var first = Assert.Single(Create(settings)!);
        Assert.Equal("数学", first.Text);
        Assert.Equal("A301 · 14:10–14:55", first.SecondaryText);
        Assert.Equal("下一节：物理 15:10–15:55", first.ExtraText);
        Assert.True(first.ShowProgress);

        component.ShowNextLesson = false;
        component.ShowProgress = false;
        var changed = Assert.Single(Create(settings)!);
        Assert.Empty(changed.ExtraText);
        Assert.False(changed.ShowProgress);
    }

    [Fact]
    public void Create_CourseCountdownDoesNotRepublishForEverySecond()
    {
        var settings = Customized(new LiveActivityComponentSettings { Kind = LiveActivityComponentKind.Countdown });
        var first = Create(settings);
        var second = LiveActivityComponentSnapshotFactory.Create(settings,
            Snapshot() with { RemainingText = "31:59" }, Now.AddSeconds(1), "晴", Next);

        Assert.Equal(first, second);
        Assert.Equal("countdown", Assert.Single(first!).Kind);
        Assert.Null(first![0].TargetTime);
    }

    [Fact]
    public void Create_CustomCountdownCarriesAnAbsoluteTargetAndHandlesMissingDate()
    {
        var component = new LiveActivityComponentSettings
        {
            Kind = LiveActivityComponentKind.Countdown,
            UseCustomCountdown = true,
            TargetDate = new DateTime(2027, 6, 7),
            CountdownName = "考试"
        };
        var settings = Customized(component);
        var countdown = Assert.Single(Create(settings)!);
        Assert.Equal("考试", countdown.Text);
        Assert.Equal((double)new DateTimeOffset(component.TargetDate.Value).ToUnixTimeSeconds(), countdown.TargetTime);

        component.TargetDate = null;
        Assert.Equal("请设置倒计时日期", Assert.Single(Create(settings)!).Text);
        component.TargetDate = DateTime.MinValue;
        Assert.Equal("text", Assert.Single(Create(settings)!).Kind);
    }

    [Theory]
    [InlineData("晴", "sun.max.fill")]
    [InlineData("阴", "cloud.fill")]
    [InlineData("大雪", "cloud.snow.fill")]
    [InlineData("雷阵雨", "cloud.bolt.rain.fill")]
    [InlineData("小雨", "cloud.rain.fill")]
    [InlineData("雾", "cloud.fog.fill")]
    public void Create_WeatherUsesReadableNamesAndSemanticIcons(string weatherText, string symbol)
    {
        var settings = Customized(new LiveActivityComponentSettings { Kind = LiveActivityComponentKind.Weather });
        settings.LastWeatherInfo = new WeatherInfo
        {
            UpdateTimeUnix = 1,
            Current = new CurrentWeather { Temperature = new ValueUnitPair { Value = "23", Unit = "°C" } }
        };
        var item = Assert.Single(LiveActivityComponentSnapshotFactory.Create(settings, Snapshot(), Now, weatherText, Next)!);
        Assert.Equal("23°C", item.Text);
        Assert.Equal(weatherText, item.SecondaryText);
        Assert.Equal(symbol, item.SymbolName);
    }

    [Fact]
    public void Create_WeatherWithoutDataShowsAnActionableFallback()
    {
        var item = Assert.Single(Create(Customized(new LiveActivityComponentSettings { Kind = LiveActivityComponentKind.Weather }))!);
        Assert.Equal("天气暂不可用", item.Text);
        Assert.Empty(item.SecondaryText);
    }

    [Fact]
    public void Create_ClockCarriesMidnightForSystemTimerAndDateUsesLocalDay()
    {
        var items = Create(Customized(new() { Kind = LiveActivityComponentKind.Date }, new() { Kind = LiveActivityComponentKind.Clock }))!;
        Assert.Equal("10月12日 周一", items[0].Text);
        Assert.Empty(items[1].Text);
        Assert.Equal((double)new DateTimeOffset(Now.Date).ToUnixTimeSeconds(), items[1].TargetTime);
    }

    [Fact]
    public void TrimText_TrimsBeforeTruncationAndPreservesEmojiAndCombiningCharacters()
    {
        Assert.Equal("您好", LiveActivityComponentSnapshotFactory.TrimText(new string(' ', 50) + "您好" + new string(' ', 50)));
        Assert.Empty(LiveActivityComponentSnapshotFactory.TrimText(null));
        var longText = string.Concat(Enumerable.Repeat("👩🏽‍💻", 60));
        var trimmed = LiveActivityComponentSnapshotFactory.TrimText(longText);
        Assert.Equal(48, StringInfo.ParseCombiningCharacters(trimmed).Length);
        Assert.Equal(string.Concat(Enumerable.Repeat("👩🏽‍💻", 47)) + "…", trimmed);
    }

    private static Settings Customized(params LiveActivityComponentSettings[] components) => new()
    {
        IsLiveActivityLayoutCustomized = true,
        LiveActivityComponents = new(components)
    };

    private static LessonLiveActivityComponent[]? Create(Settings settings) =>
        LiveActivityComponentSnapshotFactory.Create(settings, Snapshot(), Now, "晴", Next);

    private static LessonsLiveActivitySnapshot Snapshot() => new(TimeState.OnClass, "interval", "上课 · 数学", "", "14:10–14:55", "数")
    {
        ActivityDetails = Details
    };
}
