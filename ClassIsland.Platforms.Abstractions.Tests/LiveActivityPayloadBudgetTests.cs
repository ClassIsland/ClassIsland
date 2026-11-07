using System.Globalization;
using System.Text;
using System.Text.Json;
using ClassIsland.Platforms.Abstraction.Models.LiveActivities;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class LiveActivityPayloadBudgetTests
{
    private const int MaximumBytes = 3800;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void DisplayComparison_EquivalentComponentArraysDoNotTriggerPublication()
    {
        var first = Create() with { Components = [new("text", "您好"), new("date", "10月12日")] };
        var second = first with { Components = [new("text", "您好"), new("date", "10月12日")] };

        Assert.True(first.HasSameDisplayContent(second));
        Assert.False(first.HasSameDisplayContent(null));
        Assert.False(first.HasSameDisplayContent(second with { Components = [new("text", "已修改"), new("date", "10月12日")] }));
        Assert.False(first.HasSameDisplayContent(second with { Components = second.Components!.Reverse().ToArray() }));
        Assert.False(first.HasSameDisplayContent(second with { Details = second.Details! with { Location = "A302" } }));
        Assert.False(first.HasSameDisplayContent(second with { EndTime = second.EndTime!.Value.AddMinutes(1) }));
    }

    [Fact]
    public void DisplayComparison_DistinguishesLegacyLayoutFromAnEmptyCustomLayout()
    {
        var legacy = Create();
        var empty = legacy with { Components = [] };

        Assert.True(legacy.HasSameDisplayContent(Create()));
        Assert.False(legacy.HasSameDisplayContent(empty));
        Assert.True(empty.HasSameDisplayContent(Create() with { Components = [] }));
    }

    [Fact]
    public void Fit_UnderBudgetPreservesTheOriginalContentAndIcon()
    {
        var original = Create() with
        {
            Details = Create().Details! with { IconPngBase64 = "aWNvbg==" },
            Components = [new("text", "记得带计算器")]
        };

        Assert.Same(original, Fit(original));
    }

    [Fact]
    public void Fit_LargeIconIsRemovedBeforeChangingComponents()
    {
        var original = Create() with
        {
            Details = Create().Details! with { IconPngBase64 = new string('a', 5000) },
            Components = [new("text", "保留的文本")]
        };
        var fitted = Fit(original);

        Assert.Null(fitted.Details!.IconPngBase64);
        Assert.Equal(original.Components, fitted.Components);
        Assert.Equal(original.Title, fitted.Title);
        Assert.True(Measure(fitted) <= MaximumBytes);
    }

    [Fact]
    public void Fit_LongChineseComponentsAreShortenedWithinActualJsonBudget()
    {
        var longText = new string('课', 100);
        var original = Create() with
        {
            Components = Enumerable.Range(0, 6).Select(_ =>
                new LessonLiveActivityComponent("schedule", longText, longText, "book.closed.fill", longText,
                    ShowProgress: true)).ToArray()
        };
        Assert.True(Measure(original) > MaximumBytes);

        var fitted = Fit(original);

        Assert.True(Measure(fitted) <= MaximumBytes);
        Assert.Equal(original.Title, fitted.Title);
        Assert.Equal(original.StartTime, fitted.StartTime);
        Assert.Equal(original.EndTime, fitted.EndTime);
        Assert.Equal(original.DeepLink, fitted.DeepLink);
        Assert.All(fitted.Components!, component => Assert.True(component.Text.Length < longText.Length));
        Assert.Equal(longText, original.Components![0].Text);
    }

    [Fact]
    public void Fit_WhenTextTruncationIsInsufficientDropsComponentsFromTheEnd()
    {
        var original = Create() with
        {
            Title = new string('课', 500),
            Components = Enumerable.Range(0, 6).Select(index =>
                new LessonLiveActivityComponent("text", index + new string('文', 80))).ToArray()
        };
        Assert.True(Measure(original) > MaximumBytes);

        var fitted = Fit(original);

        Assert.True(Measure(fitted) <= MaximumBytes);
        Assert.NotEmpty(fitted.Components!);
        Assert.True(fitted.Components!.Length < original.Components!.Length);
        Assert.StartsWith("0", fitted.Components[0].Text);
        Assert.Equal(original.Title, fitted.Title);
    }

    [Fact]
    public void Fit_DoesNotSplitEmojiSequencesOrCombiningCharacters()
    {
        var original = Create() with
        {
            Components = Enumerable.Range(0, 6).Select(_ => new LessonLiveActivityComponent(
                "text", string.Concat(Enumerable.Repeat("👩🏽‍💻", 100)))).ToArray()
        };
        var fitted = Fit(original);

        Assert.True(Measure(fitted) <= MaximumBytes);
        Assert.All(fitted.Components!, component =>
        {
            Assert.EndsWith("…", component.Text);
            var elements = StringInfo.ParseCombiningCharacters(component.Text);
            Assert.True(elements.Length <= 24);
            Assert.Equal(string.Concat(Enumerable.Repeat("👩🏽‍💻", elements.Length - 1)) + "…", component.Text);
        });
    }

    [Fact]
    public void Fit_OversizedBaseContentRemainsRejectableWithoutCorruptingCourseData()
    {
        var original = Create() with { Title = new string('课', 1000), Components = [new("text", "文本")] };
        var fitted = Fit(original);

        Assert.True(Measure(fitted) > MaximumBytes);
        Assert.Equal(original.Title, fitted.Title);
        Assert.Empty(fitted.Components!);
    }

    [Fact]
    public void Fit_LegacyContentStillDropsAnOversizedOptionalIcon()
    {
        var original = Create() with { Details = Create().Details! with { IconPngBase64 = new string('a', 5000) } };
        var fitted = Fit(original);

        Assert.Null(fitted.Components);
        Assert.Null(fitted.Details!.IconPngBase64);
        Assert.True(Measure(fitted) <= MaximumBytes);
    }

    private static LessonLiveActivityContent Fit(LessonLiveActivityContent content) =>
        LessonLiveActivityPayloadBudget.Fit(content, Measure, MaximumBytes);

    private static int Measure(LessonLiveActivityContent content) => Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(new
    {
        content.IntervalId,
        Phase = (int)content.Phase,
        content.Title,
        content.Subtitle,
        content.Detail,
        content.CompactText,
        StartTime = content.StartTime?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
        EndTime = content.EndTime?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
        content.DeepLink,
        content.Details,
        content.Components
    }, JsonOptions));

    private static LessonLiveActivityContent Create() => new(
        "20261012:on-class:1", LessonLiveActivityPhase.OnClass, "上课 · 数学", "", "14:10–14:55", "数",
        new DateTimeOffset(2026, 10, 12, 14, 10, 0, TimeSpan.FromHours(8)),
        new DateTimeOffset(2026, 10, 12, 14, 55, 0, TimeSpan.FromHours(8)))
    {
        Details = new("数学", "A301", "14:10–14:55", "")
    };
}
