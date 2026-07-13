using System.Diagnostics;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Services;
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

    [Fact]
    public async Task ShellService_OpensExistingDirectoryWithShellExecution()
    {
        var directory = Directory.CreateTempSubdirectory("classisland-folder-service-");
        ProcessStartInfo? capturedStartInfo = null;
        var service = new ShellPlatformFolderService(startInfo => capturedStartInfo = startInfo);

        try
        {
            Assert.True(await service.OpenFolderAsync(directory.FullName));
            Assert.NotNull(capturedStartInfo);
            Assert.Equal(directory.FullName, capturedStartInfo.FileName);
            Assert.True(capturedStartInfo.UseShellExecute);
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
        var service = new SharedDocumentsPlatformFolderService(
            () => documents.FullName,
            uri =>
            {
                capturedUri = uri;
                return Task.FromResult(false);
            });

        try
        {
            Assert.False(await service.OpenFolderAsync(profiles.FullName));
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
        var service = new SharedDocumentsPlatformFolderService(
            () => documents.FullName,
            _ =>
            {
                openCalled = true;
                return Task.FromResult(true);
            });

        try
        {
            await Assert.ThrowsAsync<PlatformNotSupportedException>(
                () => service.OpenFolderAsync(externalDirectory.FullName));
            Assert.False(openCalled);
        }
        finally
        {
            documents.Delete();
            externalDirectory.Delete();
        }
    }

    [Fact]
    public async Task SharedDocumentsService_PropagatesOpenFailure()
    {
        var documents = Directory.CreateTempSubdirectory("classisland-documents-");
        var exception = new InvalidOperationException("Files app rejected the URL.");
        var service = new SharedDocumentsPlatformFolderService(
            () => documents.FullName,
            _ => Task.FromException<bool>(exception));

        try
        {
            var actual = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.OpenFolderAsync(documents.FullName));
            Assert.Same(exception, actual);
        }
        finally
        {
            documents.Delete();
        }
    }
}
