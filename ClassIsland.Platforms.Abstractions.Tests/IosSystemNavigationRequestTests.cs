using ClassIsland.Platforms.Abstraction.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class IosSystemNavigationRequestTests
{
    [Fact]
    public void TryParse_RejectsRelativeUriWithoutThrowing()
    {
        Assert.False(IosSystemNavigationRequest.TryParse(new Uri("system/schedule", UriKind.Relative), out var request));
        Assert.Null(request);
    }

    [Fact]
    public void TryParse_AcceptsUriNormalizedUnreservedPath()
    {
        var uri = new Uri("classisland://app/system/%73chedule");

        Assert.True(IosSystemNavigationRequest.TryParse(uri, out var request));
        Assert.Equal("schedule", request!.Action);
    }

    [Theory]
    [InlineData("schedule")]
    [InlineData("today")]
    [InlineData("previous-day")]
    [InlineData("next-day")]
    [InlineData("settings")]
    [InlineData("profile")]
    public void TryParse_AcceptsSupportedNavigationActions(string action)
    {
        Assert.True(IosSystemNavigationRequest.TryParse(new Uri($"classisland://app/system/{action}"), out var request));

        Assert.NotNull(request);
        Assert.Equal(action, request.Action);
        Assert.Null(request.Date);
        Assert.Null(request.ClassPlanId);
        Assert.Null(request.LessonId);
        Assert.Null(request.NotificationsEnabled);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("2147483647")]
    [InlineData("87396bbd-afda-4f20-961e-8348799f28d5")]
    public void TryParse_PreservesLogicalDateAndLessonIdentity(string lessonId)
    {
        var uri = new Uri($"classisland://app/system/schedule?date=2028-02-29&lesson={lessonId}");

        Assert.True(IosSystemNavigationRequest.TryParse(uri, out var request));

        Assert.Equal(new DateOnly(2028, 2, 29), request!.Date);
        Assert.Equal(lessonId, request.LessonId);
    }

    [Theory]
    [InlineData("class-plan")]
    [InlineData("view-plan")]
    public void TryParse_ClassPlanActionsRequireAndParseIdentity(string action)
    {
        var id = Guid.NewGuid();

        Assert.True(IosSystemNavigationRequest.TryParse(new Uri($"classisland://app/system/{action}?id={id:D}"), out var request));

        Assert.Equal(action, request!.Action);
        Assert.Equal(id, request.ClassPlanId);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("True", true)]
    public void TryParse_NotificationsRequiresExplicitBoolean(string value, bool expected)
    {
        Assert.True(IosSystemNavigationRequest.TryParse(new Uri($"classisland://app/system/notifications?enabled={value}"), out var request));

        Assert.Equal(expected, request!.NotificationsEnabled);
    }

    [Theory]
    [InlineData("https://app/system/schedule")]
    [InlineData("classisland://plugins/system/schedule")]
    [InlineData("classisland://user@app/system/schedule")]
    [InlineData("classisland://app:123/system/schedule")]
    [InlineData("classisland://app/settings")]
    [InlineData("classisland://app/system")]
    [InlineData("classisland://app/system/")]
    [InlineData("classisland://app/system/unknown")]
    [InlineData("classisland://app/system/Schedule")]
    [InlineData("classisland://app/system/schedule/extra")]
    [InlineData("classisland://app/system%2fschedule")]
    [InlineData("classisland://app/system/schedule?date=2026-02-29")]
    [InlineData("classisland://app/system/schedule?date=2026-1-01")]
    [InlineData("classisland://app/system/schedule?date=not-a-date")]
    [InlineData("classisland://app/system/schedule?date=")]
    [InlineData("classisland://app/system/schedule?lesson=-1")]
    [InlineData("classisland://app/system/schedule?lesson=2147483648")]
    [InlineData("classisland://app/system/schedule?lesson=math")]
    [InlineData("classisland://app/system/schedule?lesson=")]
    [InlineData("classisland://app/system/class-plan")]
    [InlineData("classisland://app/system/view-plan?id=not-a-guid")]
    [InlineData("classisland://app/system/class-plan?id=00000000-0000-0000-0000-000000000000")]
    [InlineData("classisland://app/system/notifications")]
    [InlineData("classisland://app/system/notifications?enabled=1")]
    [InlineData("classisland://app/system/notifications?enabled=")]
    [InlineData("classisland://app/system/schedule?date")]
    [InlineData("classisland://app/system/schedule?date=2026-11-08&date=2026-11-09")]
    [InlineData("classisland://app/system/schedule?date=2026-11-08&%64ate=2026-11-09")]
    public void TryParse_RejectsInvalidRoutesAndParameters(string value)
    {
        Assert.False(IosSystemNavigationRequest.TryParse(new Uri(value), out var request));
        Assert.Null(request);
    }
}
