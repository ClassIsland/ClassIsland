using ClassIsland.Platforms.Abstraction.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class IosFallbackNotificationPayloadPolicyTests
{
    [Fact]
    public void Create_PreservesOrderAndRemovesDuplicateText()
    {
        var payload = IosFallbackNotificationPayloadPolicy.Create(
            "课程提醒",
            [
                new(" 准备上课 ", "下节课是语文"),
                new("上课", "下节课是语文"),
                new("上课", "请打开课本")
            ]);

        Assert.Equal("准备上课", payload.Title);
        Assert.Equal(
            string.Join(Environment.NewLine, "下节课是语文", "上课", "请打开课本"),
            payload.Body);
    }

    [Fact]
    public void Create_UsesProviderWhenOnlyOverlayHasText()
    {
        var payload = IosFallbackNotificationPayloadPolicy.Create(
            "天气提醒",
            [new(null, "今日有雨")]);

        Assert.Equal("天气提醒", payload.Title);
        Assert.Equal("今日有雨", payload.Body);
    }

    [Fact]
    public void Create_UsesSafeDefaultsAndRemovesNullCharacters()
    {
        var providerOnly = IosFallbackNotificationPayloadPolicy.Create(
            "\0 管理提醒 \0",
            [new(" ", null)]);
        var empty = IosFallbackNotificationPayloadPolicy.Create(
            "\0",
            Array.Empty<IosFallbackNotificationTextEntry>());

        Assert.Equal("管理提醒", providerOnly.Title);
        Assert.Equal("你有一条新提醒。", providerOnly.Body);
        Assert.Equal("ClassIsland 提醒", empty.Title);
        Assert.Equal("你有一条新提醒。", empty.Body);
    }

    [Fact]
    public void Create_RejectsNullEntries()
    {
        Assert.Throws<ArgumentNullException>(() =>
            IosFallbackNotificationPayloadPolicy.Create("提醒", null!));
    }

    [Theory]
    [InlineData(false, new[] { true }, false)]
    [InlineData(true, new[] { false, false }, false)]
    [InlineData(true, new[] { false, true }, true)]
    public void ShouldPlaySound_RequiresGlobalAndTicketPermission(
        bool allowNotificationSound,
        bool[] ticketStates,
        bool expected)
    {
        Assert.Equal(
            expected,
            IosFallbackNotificationPayloadPolicy.ShouldPlaySound(
                allowNotificationSound,
                ticketStates));
    }

    [Fact]
    public void ShouldPlaySound_RejectsNullStates()
    {
        Assert.Throws<ArgumentNullException>(() =>
            IosFallbackNotificationPayloadPolicy.ShouldPlaySound(true, null!));
    }
}
