using ClassIsland.Platforms.Abstraction.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class SafeChildDirectoryPathTests
{
    [Fact]
    public void SafeName_IsAcceptedWithoutResolvingAPath()
    {
        SafeChildDirectoryPath.ValidateName("cn.classisland.example-plugin");
    }

    [Fact]
    public void SafeName_ResolvesDirectlyUnderRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "ClassIsland", "Plugins");

        var result = SafeChildDirectoryPath.Resolve(root, "cn.classisland.example-plugin");

        Assert.Equal(
            Path.Combine(Path.GetFullPath(root), "cn.classisland.example-plugin"),
            result);
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("../Data")]
    [InlineData("..\\Data")]
    [InlineData("nested/plugin")]
    [InlineData("nested\\plugin")]
    [InlineData(" plugin")]
    [InlineData("plugin ")]
    public void UnsafeName_IsRejected(string value)
    {
        Assert.Throws<InvalidDataException>(() =>
            SafeChildDirectoryPath.Resolve(Path.GetTempPath(), value));
        Assert.Throws<InvalidDataException>(() =>
            SafeChildDirectoryPath.ValidateName(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyName_IsRejected(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            SafeChildDirectoryPath.Resolve(Path.GetTempPath(), value!));
        Assert.ThrowsAny<ArgumentException>(() =>
            SafeChildDirectoryPath.ValidateName(value!));
    }

    [Fact]
    public void RootedName_IsRejectedOnCurrentPlatform()
    {
        var rootedName = Path.GetPathRoot(Path.GetFullPath(Path.GetTempPath()));

        Assert.False(string.IsNullOrEmpty(rootedName));
        Assert.Throws<InvalidDataException>(() =>
            SafeChildDirectoryPath.Resolve(Path.GetTempPath(), rootedName!));
        Assert.Throws<InvalidDataException>(() =>
            SafeChildDirectoryPath.ValidateName(rootedName!));
    }

    [Fact]
    public void InvalidFileNameCharacter_IsRejected()
    {
        Assert.Throws<InvalidDataException>(() =>
            SafeChildDirectoryPath.Resolve(Path.GetTempPath(), "plugin\0name"));
        Assert.Throws<InvalidDataException>(() =>
            SafeChildDirectoryPath.ValidateName("plugin\0name"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyRoot_IsRejected(string? root)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            SafeChildDirectoryPath.Resolve(root!, "plugin"));
    }
}
