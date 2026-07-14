using ClassIsland.Platforms.Abstraction.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class SafeRelativePathTests
{
    [Theory]
    [InlineData("Profiles/default.json", "Profiles/default.json")]
    [InlineData("Config\\Settings.json", "Config/Settings.json")]
    [InlineData("Plugins/example/", "Plugins/example")]
    public void Normalize_ProducesCanonicalArchivePath(
        string input,
        string expected)
    {
        Assert.Equal(expected, SafeRelativePath.Normalize(input));
    }

    [Theory]
    [InlineData("../Settings.json")]
    [InlineData("Profiles/../Settings.json")]
    [InlineData("Profiles/./default.json")]
    [InlineData("Profiles//default.json")]
    [InlineData("/absolute/path")]
    [InlineData("C:/absolute/path")]
    public void Normalize_RejectsUnsafePaths(string input)
    {
        Assert.Throws<InvalidDataException>(() => SafeRelativePath.Normalize(input));
    }

    [Fact]
    public void ResolveUnderRoot_StaysInsideRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "ClassIsland", "Data");

        var result = SafeRelativePath.ResolveUnderRoot(
            root,
            "Profiles/default.json");

        Assert.Equal(
            Path.Combine(Path.GetFullPath(root), "Profiles", "default.json"),
            result);
    }
}
