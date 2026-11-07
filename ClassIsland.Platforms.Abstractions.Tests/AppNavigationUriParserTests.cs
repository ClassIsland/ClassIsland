using ClassIsland.Platforms.Abstraction.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class AppNavigationUriParserTests
{
    [Theory]
    [InlineData("classisland://app/live-activity")]
    [InlineData("CLASSISLAND://app/settings")]
    [InlineData("classisland://app/settings/notification?ci_keepHistory=true")]
    [InlineData("classisland://app/profile/timeLayouts")]
    [InlineData("classisland://app/helps")]
    public void TryParseClassIslandUri_AcceptsAbsoluteAppLinks(string value)
    {
        Assert.True(AppNavigationUriParser.TryParseClassIslandUri(value, out var uri));
        Assert.NotNull(uri);
        Assert.Equal("classisland", uri.Scheme, ignoreCase: true);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("app/live-activity")]
    [InlineData("https://classisland.tech/")]
    [InlineData("classisland://plugins/example")]
    [InlineData("classisland://user@app/settings")]
    [InlineData("classisland://app:123/settings")]
    [InlineData("classisland://app/")]
    [InlineData("classisland://app/test")]
    [InlineData("classisland://app/edit")]
    [InlineData("classisland://app/api/automation/run/example")]
    [InlineData("classisland://app/api/automation/revert/example")]
    [InlineData("classisland://app/%61pi/automation/run/example")]
    [InlineData("classisland://app/settings%2f..%2fapi/automation/run/example")]
    [InlineData("classisland://app/settings%5c..%5capi/automation/run/example")]
    public void TryParseClassIslandUri_RejectsInvalidOrExternalLinks(string? value)
    {
        Assert.False(AppNavigationUriParser.TryParseClassIslandUri(value, out var uri));
        Assert.Null(uri);
    }
}
