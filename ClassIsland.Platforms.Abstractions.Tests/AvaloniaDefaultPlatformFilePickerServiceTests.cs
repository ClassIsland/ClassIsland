using Avalonia.Platform.Storage;
using ClassIsland.Platforms.Abstraction.Stubs.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class AvaloniaDefaultPlatformFilePickerServiceTests
{
    [Fact]
    public void MaterializeFiles_RejectsNullInput()
    {
        var service = new AvaloniaDefaultPlatformFilePickerService();

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = service.MaterializeFilesAsync(null!);
        });
    }

    [Fact]
    public async Task MaterializeFiles_ReturnsOnlyLocalPathsWithoutReadingFiles()
    {
        var localPath = Path.Combine(Path.GetTempPath(), "plugin.cipx");
        var localUri = new UriBuilder(Uri.UriSchemeFile, "", -1, Path.GetFullPath(localPath)).Uri;
        var localFile = new FakeStorageFile("plugin.cipx", localUri);
        var remoteFile = new FakeStorageFile(
            "remote.cipx",
            new Uri("https://example.com/remote.cipx"));
        var service = new AvaloniaDefaultPlatformFilePickerService();

        var paths = await service.MaterializeFilesAsync([localFile, remoteFile]);

        Assert.Equal(Path.GetFullPath(localPath), Assert.Single(paths));
        Assert.Equal(0, localFile.OpenReadCount);
        Assert.Equal(0, remoteFile.OpenReadCount);
    }

    private sealed class FakeStorageFile(string name, Uri path) : IStorageFile
    {
        public string Name { get; } = name;
        public Uri Path { get; } = path;
        public bool CanBookmark => false;
        public int OpenReadCount { get; private set; }

        public Task<StorageItemProperties> GetBasicPropertiesAsync() =>
            Task.FromResult(new StorageItemProperties(null, null, null));

        public Task<string?> SaveBookmarkAsync() => Task.FromResult<string?>(null);
        public Task<IStorageFolder?> GetParentAsync() => Task.FromResult<IStorageFolder?>(null);
        public Task DeleteAsync() => throw new NotSupportedException();
        public Task<IStorageItem?> MoveAsync(IStorageFolder destination) =>
            throw new NotSupportedException();

        public Task<Stream> OpenReadAsync()
        {
            OpenReadCount++;
            return Task.FromResult<Stream>(new MemoryStream());
        }

        public Task<Stream> OpenWriteAsync() => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}
