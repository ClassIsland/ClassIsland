using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Stubs.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class PlatformFolderServiceTests
{
    [Fact]
    public void PlatformServices_DefaultsToShellFolderService()
    {
        Assert.IsType<ShellPlatformFolderService>(PlatformServices.FolderService);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ShellService_RejectsEmptyPath(string path)
    {
        var service = new ShellPlatformFolderService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.OpenFolderAsync(path));
    }

    [Fact]
    public async Task ShellService_RejectsMissingDirectory()
    {
        var service = new ShellPlatformFolderService();
        var missingDirectory = Path.Combine(
            Path.GetTempPath(),
            $"classisland-missing-{Guid.NewGuid():N}");

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => service.OpenFolderAsync(missingDirectory));
    }
}
