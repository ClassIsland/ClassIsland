using System.Diagnostics;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Platforms.Abstraction.Stubs.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class LauncherServiceTests
{
    [Fact]
    public void PlatformServices_DefaultsToLauncherServiceStub()
    {
        Assert.IsType<LauncherServiceStub>(PlatformServices.LauncherService);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ShellService_RejectsEmptyPath(string path)
    {
        var service = new ShellLauncherService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.LaunchPath(path));
    }

    [Fact]
    public async Task ShellService_RejectsMissingDirectory()
    {
        var service = new ShellLauncherService();
        var missingDirectory = Path.Combine(
            Path.GetTempPath(),
            $"classisland-missing-{Guid.NewGuid():N}");

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => service.LaunchPath(missingDirectory));
    }

    [Fact]
    public async Task ShellService_OpensExistingDirectoryWithShellExecution()
    {
        var directory = Directory.CreateTempSubdirectory("classisland-launcher-");
        ProcessStartInfo? capturedStartInfo = null;
        var service = new ShellLauncherService(startInfo => capturedStartInfo = startInfo);

        try
        {
            await service.LaunchPath(directory.FullName);

            Assert.NotNull(capturedStartInfo);
            Assert.Equal(directory.FullName, capturedStartInfo.FileName);
            Assert.True(capturedStartInfo.UseShellExecute);
        }
        finally
        {
            directory.Delete();
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("docs/index.html")]
    public async Task ShellService_RejectsInvalidUrl(string url)
    {
        var service = new ShellLauncherService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.LaunchUrl(url));
    }

    [Fact]
    public async Task ShellService_OpensAbsoluteUrlWithShellExecution()
    {
        ProcessStartInfo? capturedStartInfo = null;
        var service = new ShellLauncherService(startInfo => capturedStartInfo = startInfo);
        const string url = "https://classisland.tech/path?q=1";

        await service.LaunchUrl(url);

        Assert.NotNull(capturedStartInfo);
        Assert.Equal(url, capturedStartInfo.FileName);
        Assert.True(capturedStartInfo.UseShellExecute);
    }

    [Fact]
    public async Task ShellService_PropagatesStartFailure()
    {
        var exception = new InvalidOperationException("Shell rejected the request.");
        var service = new ShellLauncherService(_ => throw exception);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LaunchUrl("https://classisland.tech/"));

        Assert.Same(exception, actual);
    }

    [Fact]
    public async Task ShellService_PropagatesDirectoryStartFailure()
    {
        var directory = Directory.CreateTempSubdirectory("classisland-launcher-");
        var exception = new InvalidOperationException("Shell rejected the directory.");
        var service = new ShellLauncherService(_ => throw exception);

        try
        {
            var actual = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.LaunchPath(directory.FullName));
            Assert.Same(exception, actual);
        }
        finally
        {
            directory.Delete();
        }
    }

    [Fact]
    public async Task SharedDocumentsService_EncodesAndOpensChildDirectory()
    {
        var documents = Directory.CreateTempSubdirectory("classisland-documents-");
        var profiles = documents.CreateSubdirectory("Profiles #1");
        Uri? capturedUri = null;
        var service = new SharedDocumentsLauncherService(
            () => documents.FullName,
            uri =>
            {
                capturedUri = uri;
                return Task.FromResult(true);
            });

        try
        {
            await service.LaunchPath(profiles.FullName);

            Assert.NotNull(capturedUri);
            Assert.Equal("shareddocuments", capturedUri.Scheme);
            Assert.StartsWith("shareddocuments:///", capturedUri.AbsoluteUri);
            Assert.Contains("Profiles%20%231", capturedUri.AbsoluteUri);
        }
        finally
        {
            documents.Delete(true);
        }
    }

    [Fact]
    public async Task SharedDocumentsService_RejectsDirectoryOutsideDocuments()
    {
        var documents = Directory.CreateTempSubdirectory("classisland-documents-");
        var externalDirectory = Directory.CreateTempSubdirectory("classisland-external-");
        var openCalled = false;
        var service = new SharedDocumentsLauncherService(
            () => documents.FullName,
            _ =>
            {
                openCalled = true;
                return Task.FromResult(true);
            });

        try
        {
            await Assert.ThrowsAsync<PlatformNotSupportedException>(
                () => service.LaunchPath(externalDirectory.FullName));
            Assert.False(openCalled);
        }
        finally
        {
            documents.Delete();
            externalDirectory.Delete();
        }
    }

    [Fact]
    public async Task SharedDocumentsService_ThrowsWhenFilesAppRejectsDirectory()
    {
        var documents = Directory.CreateTempSubdirectory("classisland-documents-");
        var service = new SharedDocumentsLauncherService(
            () => documents.FullName,
            _ => Task.FromResult(false));

        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.LaunchPath(documents.FullName));
            Assert.Contains("文件", exception.Message);
            Assert.Contains(documents.FullName, exception.Message);
        }
        finally
        {
            documents.Delete();
        }
    }

    [Fact]
    public async Task SharedDocumentsService_OpensExternalUrl()
    {
        Uri? capturedUri = null;
        var service = new SharedDocumentsLauncherService(
            () => Path.GetTempPath(),
            uri =>
            {
                capturedUri = uri;
                return Task.FromResult(true);
            });

        await service.LaunchUrl("https://classisland.tech/");

        Assert.Equal("https://classisland.tech/", capturedUri?.AbsoluteUri);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("docs/index.html")]
    [InlineData("file:///private/var/mobile/example.txt")]
    [InlineData("tel:+861234567890")]
    [InlineData("classisland://app/settings")]
    public async Task SharedDocumentsService_RejectsInvalidUrl(string url)
    {
        var openCalled = false;
        var service = new SharedDocumentsLauncherService(
            () => Path.GetTempPath(),
            _ =>
            {
                openCalled = true;
                return Task.FromResult(true);
            });

        await Assert.ThrowsAsync<ArgumentException>(() => service.LaunchUrl(url));
        Assert.False(openCalled);
    }

    [Fact]
    public async Task SharedDocumentsService_ThrowsWhenSystemRejectsUrl()
    {
        var service = new SharedDocumentsLauncherService(
            () => Path.GetTempPath(),
            _ => Task.FromResult(false));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LaunchUrl("https://classisland.tech/"));
        Assert.Contains("https://classisland.tech/", exception.Message);
    }

    [Fact]
    public async Task SharedDocumentsService_WrapsSystemUrlOpenFailure()
    {
        var systemException = new InvalidOperationException("UIApplication rejected the request.");
        var service = new SharedDocumentsLauncherService(
            () => Path.GetTempPath(),
            _ => Task.FromException<bool>(systemException));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LaunchUrl("https://classisland.tech/"));
        Assert.Contains("https://classisland.tech/", exception.Message);
        Assert.Same(systemException, exception.InnerException);
    }

    [Fact]
    public async Task DefaultFileLaunch_PreservesExistingPlatformPathBehavior()
    {
        var legacy = new LegacyLauncherService();
        ILauncherService service = legacy;

        await service.LaunchFile("bookmark:test.pdf");

        Assert.Equal("bookmark:test.pdf", legacy.Path);
    }

    [Fact]
    public async Task DefaultAppLinkLaunch_PreservesExistingPlatformUrlBehavior()
    {
        var legacy = new LegacyLauncherService();
        ILauncherService service = legacy;

        await service.LaunchAppLink("exampleapp://document/42");

        Assert.Equal("exampleapp://document/42", legacy.Url);
    }

    [Theory]
    [InlineData("exampleapp://document/42?title=hello%20world")]
    [InlineData("https://example.com/app/document/42")]
    [InlineData("http://example.com/app")]
    [InlineData("mailto:test@example.com")]
    [InlineData("tel:+861234567890")]
    public async Task SharedDocumentsService_OpensAppLink(string link)
    {
        Uri? capturedUri = null;
        var service = new SharedDocumentsLauncherService(
            () => Path.GetTempPath(),
            uri =>
            {
                capturedUri = uri;
                return Task.FromResult(true);
            });

        await service.LaunchAppLink(link);

        Assert.Equal(link, capturedUri?.AbsoluteUri);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("example.com/app")]
    [InlineData("C:\\Program Files\\app.exe")]
    [InlineData("/private/var/mobile/document.pdf")]
    [InlineData("file:///private/var/mobile/document.pdf")]
    [InlineData("FILE:///private/var/mobile/document.pdf")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/plain,hello")]
    [InlineData("about:blank")]
    [InlineData("blob:https://example.com/id")]
    [InlineData("shareddocuments:///private/var/mobile")]
    public async Task SharedDocumentsService_RejectsInvalidAppLinkWithoutOpening(string link)
    {
        var openCalled = false;
        var service = new SharedDocumentsLauncherService(
            () => Path.GetTempPath(),
            _ =>
            {
                openCalled = true;
                return Task.FromResult(true);
            });

        await Assert.ThrowsAsync<ArgumentException>(() => service.LaunchAppLink(link));

        Assert.False(openCalled);
    }

    [Fact]
    public async Task SharedDocumentsService_ReportsUnavailableApp()
    {
        var service = new SharedDocumentsLauncherService(
            () => Path.GetTempPath(), _ => Task.FromResult(false));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LaunchAppLink("exampleapp://document/42"));

        Assert.Contains("安装", exception.Message);
    }

    [Fact]
    public async Task SharedDocumentsService_PreservesAppLinkOpenFailureForDiagnostics()
    {
        var systemException = new InvalidOperationException("System opener failed.");
        var service = new SharedDocumentsLauncherService(
            () => Path.GetTempPath(), _ => Task.FromException<bool>(systemException));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LaunchAppLink("exampleapp://document/42"));

        Assert.Contains("检查链接", exception.Message);
        Assert.Same(systemException, exception.InnerException);
    }

    private sealed class LegacyLauncherService : ILauncherService
    {
        public string? Path { get; private set; }
        public string? Url { get; private set; }

        public Task LaunchPath(string path)
        {
            Path = path;
            return Task.CompletedTask;
        }

        public Task LaunchUrl(string url)
        {
            Url = url;
            return Task.CompletedTask;
        }
    }
}
