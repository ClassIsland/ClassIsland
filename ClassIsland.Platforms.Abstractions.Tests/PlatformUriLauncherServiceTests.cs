using System.Diagnostics;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Stubs.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class PlatformUriLauncherServiceTests
{
    [Fact]
    public void PlatformServices_DefaultsToShellUriLauncherService()
    {
        Assert.IsType<ShellPlatformUriLauncherService>(PlatformServices.UriLauncherService);
    }

    [Fact]
    public async Task ShellService_RejectsRelativeUri()
    {
        var service = new ShellPlatformUriLauncherService();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.OpenUriAsync(new Uri("docs/index.html", UriKind.Relative)));
    }

    [Fact]
    public async Task ShellService_RejectsNullUri()
    {
        var service = new ShellPlatformUriLauncherService();

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.OpenUriAsync(null!));
    }

    [Fact]
    public async Task ShellService_OpensAbsoluteUriWithShellExecution()
    {
        ProcessStartInfo? capturedStartInfo = null;
        var service = new ShellPlatformUriLauncherService(startInfo => capturedStartInfo = startInfo);
        var uri = new Uri("https://classisland.tech/path?q=1");

        Assert.True(await service.OpenUriAsync(uri));

        Assert.NotNull(capturedStartInfo);
        Assert.Equal(uri.AbsoluteUri, capturedStartInfo.FileName);
        Assert.True(capturedStartInfo.UseShellExecute);
    }

    [Fact]
    public async Task ShellService_PropagatesStartFailure()
    {
        var exception = new InvalidOperationException("Shell rejected the URI.");
        var service = new ShellPlatformUriLauncherService(_ => throw exception);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.OpenUriAsync(new Uri("https://classisland.tech/")));

        Assert.Same(exception, actual);
    }
}
