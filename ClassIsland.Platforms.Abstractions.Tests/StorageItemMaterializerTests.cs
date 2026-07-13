using Avalonia.Platform.Storage;
using ClassIsland.Platforms.Abstraction.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class StorageItemMaterializerTests
{
    [Fact]
    public async Task MaterializeSelections_RejectsNullInput()
    {
        var materializer = new StorageItemMaterializer(Path.GetTempPath());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => materializer.MaterializeFilesAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => materializer.MaterializeFoldersAsync(null!));
    }

    [Fact]
    public async Task MaterializeEmptySelections_DoesNotCreateStagingRoot()
    {
        var stagingRoot = Path.Combine(
            Path.GetTempPath(),
            $"classisland-storage-materializer-{Guid.NewGuid():N}");
        var materializer = new StorageItemMaterializer(stagingRoot);

        Assert.Empty(await materializer.MaterializeFilesAsync([]));
        Assert.Empty(await materializer.MaterializeFoldersAsync([]));
        Assert.False(Directory.Exists(stagingRoot));
    }

    [Fact]
    public async Task MaterializeFiles_ReadsStorageStreamAndPreservesName()
    {
        var stagingRoot = CreateStagingRoot();
        var source = new FakeStorageFile("schedule.yaml", "lessons: []"u8.ToArray());
        var materializer = new StorageItemMaterializer(stagingRoot);

        try
        {
            var paths = await materializer.MaterializeFilesAsync([source]);

            var path = Assert.Single(paths);
            Assert.Equal("schedule.yaml", Path.GetFileName(path));
            Assert.Equal("lessons: []", await File.ReadAllTextAsync(path));
            Assert.Equal(1, source.OpenReadCount);
            Assert.True(source.IsDisposed);
        }
        finally
        {
            Directory.Delete(stagingRoot, true);
        }
    }

    [Fact]
    public async Task MaterializeFiles_DoesNotAllowSelectedNameToEscapeStagingRoot()
    {
        var stagingRoot = CreateStagingRoot();
        var source = new FakeStorageFile("../outside.cipx", [1, 2, 3]);
        var materializer = new StorageItemMaterializer(stagingRoot);

        try
        {
            var path = Assert.Single(await materializer.MaterializeFilesAsync([source]));

            Assert.True(Path.GetFullPath(path).StartsWith(
                Path.GetFullPath(stagingRoot) + Path.DirectorySeparatorChar,
                StringComparison.Ordinal));
            Assert.Equal("outside.cipx", Path.GetFileName(path));
        }
        finally
        {
            Directory.Delete(stagingRoot, true);
        }
    }

    [Fact]
    public async Task MaterializeFolders_RecursivelyCopiesFiles()
    {
        var stagingRoot = CreateStagingRoot();
        var nestedFile = new FakeStorageFile("profile.json", "{}"u8.ToArray());
        var nestedFolder = new FakeStorageFolder("Profiles", [nestedFile]);
        var source = new FakeStorageFolder("Class Widgets", [nestedFolder]);
        var materializer = new StorageItemMaterializer(stagingRoot);

        try
        {
            var folder = Assert.Single(await materializer.MaterializeFoldersAsync([source]));

            Assert.Equal(
                "{}",
                await File.ReadAllTextAsync(Path.Combine(folder, "Profiles", "profile.json")));
            Assert.True(source.IsDisposed);
            Assert.True(nestedFolder.IsDisposed);
            Assert.True(nestedFile.IsDisposed);
        }
        finally
        {
            Directory.Delete(stagingRoot, true);
        }
    }

    [Fact]
    public async Task MaterializeFiles_UsesUniqueNamesForDuplicateSelections()
    {
        var stagingRoot = CreateStagingRoot();
        var first = new FakeStorageFile("schedule.yaml", "first"u8.ToArray());
        var second = new FakeStorageFile("schedule.yaml", "second"u8.ToArray());
        var materializer = new StorageItemMaterializer(stagingRoot);

        try
        {
            var paths = await materializer.MaterializeFilesAsync([first, second]);

            Assert.Equal(2, paths.Count);
            Assert.Equal("schedule.yaml", Path.GetFileName(paths[0]));
            Assert.Equal("schedule (2).yaml", Path.GetFileName(paths[1]));
            Assert.Equal("first", await File.ReadAllTextAsync(paths[0]));
            Assert.Equal("second", await File.ReadAllTextAsync(paths[1]));
        }
        finally
        {
            Directory.Delete(stagingRoot, true);
        }
    }

    [Fact]
    public async Task MaterializeFiles_CleansOperationDirectoryWhenReadFails()
    {
        var stagingRoot = CreateStagingRoot();
        var source = new FakeStorageFile(
            "broken.cipx",
            [],
            new IOException("source unavailable"));
        var materializer = new StorageItemMaterializer(stagingRoot);

        try
        {
            var exception = await Assert.ThrowsAsync<IOException>(
                () => materializer.MaterializeFilesAsync([source]));

            Assert.Equal("source unavailable", exception.Message);
            Assert.Empty(Directory.EnumerateFileSystemEntries(stagingRoot));
            Assert.True(source.IsDisposed);
        }
        finally
        {
            Directory.Delete(stagingRoot, true);
        }
    }

    private static string CreateStagingRoot()
    {
        return Directory.CreateTempSubdirectory("classisland-storage-materializer-").FullName;
    }

    private abstract class FakeStorageItem(string name) : IStorageItem
    {
        public string Name { get; } = name;
        public Uri Path { get; } = new(name, UriKind.Relative);
        public bool CanBookmark => false;
        public bool IsDisposed { get; private set; }

        public Task<StorageItemProperties> GetBasicPropertiesAsync() =>
            Task.FromResult(new StorageItemProperties(null, null, null));

        public Task<string?> SaveBookmarkAsync() => Task.FromResult<string?>(null);
        public Task<IStorageFolder?> GetParentAsync() => Task.FromResult<IStorageFolder?>(null);
        public Task DeleteAsync() => throw new NotSupportedException();
        public Task<IStorageItem?> MoveAsync(IStorageFolder destination) =>
            throw new NotSupportedException();

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class FakeStorageFile(
        string name,
        byte[] content,
        Exception? openException = null) : FakeStorageItem(name), IStorageFile
    {
        public int OpenReadCount { get; private set; }

        public Task<Stream> OpenReadAsync()
        {
            OpenReadCount++;
            if (openException != null)
            {
                return Task.FromException<Stream>(openException);
            }

            return Task.FromResult<Stream>(new MemoryStream(content, writable: false));
        }

        public Task<Stream> OpenWriteAsync() => throw new NotSupportedException();
    }

    private sealed class FakeStorageFolder(string name, IReadOnlyList<IStorageItem> items)
        : FakeStorageItem(name), IStorageFolder
    {
        public async IAsyncEnumerable<IStorageItem> GetItemsAsync()
        {
            await Task.CompletedTask;
            foreach (var item in items)
            {
                yield return item;
            }
        }

        public Task<IStorageFolder?> GetFolderAsync(string name) =>
            Task.FromResult<IStorageFolder?>(null);

        public Task<IStorageFile?> GetFileAsync(string name) =>
            Task.FromResult<IStorageFile?>(null);

        public Task<IStorageFile?> CreateFileAsync(string name) =>
            throw new NotSupportedException();

        public Task<IStorageFolder?> CreateFolderAsync(string name) =>
            throw new NotSupportedException();
    }
}
