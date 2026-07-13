using ClassIsland.Platforms.Abstraction.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class AppNavigationUriParserTests
{
    [Theory]
    [InlineData("classisland://app/live-activity")]
    [InlineData("CLASSISLAND://app/settings")]
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
    public void TryParseClassIslandUri_RejectsInvalidOrExternalLinks(string? value)
    {
        Assert.False(AppNavigationUriParser.TryParseClassIslandUri(value, out var uri));
        Assert.Null(uri);
    }
}
