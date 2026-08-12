using System.Reflection;
using Avalonia.Platform.Storage;
using ClassIsland.Platforms.Abstraction.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class StorageItemMaterializerTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyStagingRoot(string stagingRoot)
    {
        Assert.Throws<ArgumentException>(() =>
            new StorageItemMaterializer(stagingRoot));
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    public void Constructor_RejectsNonPositiveLimits(
        int maximumFileCount,
        long maximumFileLength,
        long maximumTotalLength)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StorageItemMaterializer(
                Path.GetTempPath(),
                maximumFileCount,
                maximumFileLength,
                maximumTotalLength));
    }

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
        using var scope = new TemporaryDirectory();
        var stagingRoot = Path.Combine(scope.Path, "missing-staging");
        var materializer = new StorageItemMaterializer(stagingRoot);

        Assert.Empty(await materializer.MaterializeFilesAsync([]));
        Assert.Empty(await materializer.MaterializeFoldersAsync([]));
        Assert.False(Directory.Exists(stagingRoot));
    }

    [Fact]
    public async Task MaterializeFiles_ReadsStorageStreamAndPreservesName()
    {
        using var scope = new TemporaryDirectory();
        var stagingRoot = scope.CreateDirectory("staging");
        var sourcePath = Path.Combine(scope.Path, "schedule.yaml");
        await File.WriteAllTextAsync(sourcePath, "lessons: []");
        var source = CreateStorageFile(sourcePath);
        var materializer = new StorageItemMaterializer(stagingRoot);

        var path = Assert.Single(await materializer.MaterializeFilesAsync([source]));

        Assert.Equal("schedule.yaml", Path.GetFileName(path));
        Assert.Equal("lessons: []", await File.ReadAllTextAsync(path));
        Assert.StartsWith(
            Path.GetFullPath(stagingRoot) + Path.DirectorySeparatorChar,
            Path.GetFullPath(path),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task MaterializeFiles_DisposesEverySecurityScopedSelectionOnFailure()
    {
        using var scope = new TemporaryDirectory();
        var stagingRoot = scope.CreateDirectory("staging");
        var disposeCounts = new int[2];
        var files = new IStorageFile[]
        {
            CreateCallbackStorageFile(
                "first.txt",
                () => Task.FromResult<Stream>(new MemoryStream([1])),
                () => disposeCounts[0]++),
            CreateCallbackStorageFile(
                "failure.txt",
                () => Task.FromException<Stream>(new IOException("read failed")),
                () => disposeCounts[1]++)
        };
        var materializer = new StorageItemMaterializer(stagingRoot);

        await Assert.ThrowsAsync<IOException>(() =>
            materializer.MaterializeFilesAsync(files));

        Assert.Equal([1, 1], disposeCounts);
        Assert.Empty(Directory.EnumerateFileSystemEntries(stagingRoot));
    }

    [Fact]
    public async Task MaterializeFiles_NormalizesNamesForCrossPlatformArchiveRoundTrip()
    {
        using var scope = new TemporaryDirectory();
        var stagingRoot = scope.CreateDirectory("staging");
        var files = new IStorageFile[]
        {
            CreateCallbackStorageFile(
                "sound:name.",
                () => Task.FromResult<Stream>(new MemoryStream([1])),
                () => { }),
            CreateCallbackStorageFile(
                "CON.txt",
                () => Task.FromResult<Stream>(new MemoryStream([2])),
                () => { })
        };
        var materializer = new StorageItemMaterializer(stagingRoot);

        var paths = await materializer.MaterializeFilesAsync(files);

        Assert.Equal("sound_name", Path.GetFileName(paths[0]));
        Assert.Equal("_CON.txt", Path.GetFileName(paths[1]));
    }

    [Fact]
    public async Task MaterializeFiles_AvoidsCaseInsensitiveNameCollisions()
    {
        using var scope = new TemporaryDirectory();
        var stagingRoot = scope.CreateDirectory("staging");
        var files = new IStorageFile[]
        {
            CreateCallbackStorageFile(
                "Logo.png",
                () => Task.FromResult<Stream>(new MemoryStream([1])),
                () => { }),
            CreateCallbackStorageFile(
                "logo.png",
                () => Task.FromResult<Stream>(new MemoryStream([2])),
                () => { })
        };
        var materializer = new StorageItemMaterializer(stagingRoot);

        var paths = await materializer.MaterializeFilesAsync(files);

        Assert.Equal("Logo.png", Path.GetFileName(paths[0]));
        Assert.Equal("logo (2).png", Path.GetFileName(paths[1]));
    }

    [Fact]
    public async Task MaterializeFolders_RecursivelyCopiesFiles()
    {
        using var scope = new TemporaryDirectory();
        var stagingRoot = scope.CreateDirectory("staging");
        var sourceRoot = scope.CreateDirectory("Class Widgets");
        var profiles = Directory.CreateDirectory(
            Path.Combine(sourceRoot, "Profiles")).FullName;
        await File.WriteAllTextAsync(
            Path.Combine(profiles, "profile.json"),
            "{}");
        var source = CreateStorageFolder(sourceRoot);
        var materializer = new StorageItemMaterializer(stagingRoot);

        var folder = Assert.Single(await materializer.MaterializeFoldersAsync([source]));

        Assert.Equal(
            "{}",
            await File.ReadAllTextAsync(Path.Combine(folder, "Profiles", "profile.json")));
    }

    [Fact]
    public async Task MaterializeFiles_UsesUniqueNamesForDuplicateSelections()
    {
        using var scope = new TemporaryDirectory();
        var stagingRoot = scope.CreateDirectory("staging");
        var firstDirectory = scope.CreateDirectory("first");
        var secondDirectory = scope.CreateDirectory("second");
        var firstPath = Path.Combine(firstDirectory, "schedule.yaml");
        var secondPath = Path.Combine(secondDirectory, "schedule.yaml");
        await File.WriteAllTextAsync(firstPath, "first");
        await File.WriteAllTextAsync(secondPath, "second");
        var materializer = new StorageItemMaterializer(stagingRoot);

        var paths = await materializer.MaterializeFilesAsync(
            [CreateStorageFile(firstPath), CreateStorageFile(secondPath)]);

        Assert.Equal(2, paths.Count);
        Assert.Equal("schedule.yaml", Path.GetFileName(paths[0]));
        Assert.Equal("schedule (2).yaml", Path.GetFileName(paths[1]));
        Assert.Equal("first", await File.ReadAllTextAsync(paths[0]));
        Assert.Equal("second", await File.ReadAllTextAsync(paths[1]));
    }

    [Fact]
    public async Task MaterializeFiles_CleansOperationDirectoryWhenReadFails()
    {
        using var scope = new TemporaryDirectory();
        var stagingRoot = scope.CreateDirectory("staging");
        var sourcePath = Path.Combine(scope.Path, "missing.cipx");
        await File.WriteAllTextAsync(sourcePath, "temporary");
        var source = CreateStorageFile(sourcePath);
        File.Delete(sourcePath);
        var materializer = new StorageItemMaterializer(stagingRoot);

        await Assert.ThrowsAnyAsync<IOException>(
            () => materializer.MaterializeFilesAsync([source]));

        Assert.Empty(Directory.EnumerateFileSystemEntries(stagingRoot));
    }

    [Fact]
    public async Task MaterializeFolders_EnforcesFileCountLimitAndCleansOperationDirectory()
    {
        using var scope = new TemporaryDirectory();
        var stagingRoot = scope.CreateDirectory("staging");
        var sourceRoot = scope.CreateDirectory("source");
        var firstPath = Path.Combine(sourceRoot, "first.txt");
        var secondPath = Path.Combine(sourceRoot, "second.txt");
        await File.WriteAllTextAsync(firstPath, "1");
        await File.WriteAllTextAsync(secondPath, "2");
        var materializer = new StorageItemMaterializer(
            stagingRoot,
            maximumFileCount: 1,
            maximumFileLength: 100,
            maximumTotalLength: 100);

        await Assert.ThrowsAsync<IOException>(() =>
            materializer.MaterializeFoldersAsync(
                [CreateStorageFolder(sourceRoot)]));

        Assert.Empty(Directory.EnumerateFileSystemEntries(stagingRoot));
    }

    [Fact]
    public async Task MaterializeFiles_RejectsOversizedSelectionBeforeStaging()
    {
        using var scope = new TemporaryDirectory();
        var stagingRoot = scope.CreateDirectory("staging");
        var firstPath = Path.Combine(scope.Path, "first.txt");
        var secondPath = Path.Combine(scope.Path, "second.txt");
        await File.WriteAllTextAsync(firstPath, "1");
        await File.WriteAllTextAsync(secondPath, "2");
        var materializer = new StorageItemMaterializer(
            stagingRoot,
            maximumFileCount: 1,
            maximumFileLength: 100,
            maximumTotalLength: 100);

        await Assert.ThrowsAsync<IOException>(() =>
            materializer.MaterializeFilesAsync(
                [CreateStorageFile(firstPath), CreateStorageFile(secondPath)]));

        Assert.Empty(Directory.EnumerateFileSystemEntries(stagingRoot));
    }

    [Fact]
    public async Task MaterializeFolders_RejectsOversizedSelectionBeforeStaging()
    {
        using var scope = new TemporaryDirectory();
        var stagingRoot = scope.CreateDirectory("staging");
        var firstRoot = scope.CreateDirectory("first");
        var secondRoot = scope.CreateDirectory("second");
        var materializer = new StorageItemMaterializer(
            stagingRoot,
            maximumFileCount: 1,
            maximumFileLength: 100,
            maximumTotalLength: 100);

        await Assert.ThrowsAsync<IOException>(() =>
            materializer.MaterializeFoldersAsync(
                [CreateStorageFolder(firstRoot), CreateStorageFolder(secondRoot)]));

        Assert.Empty(Directory.EnumerateFileSystemEntries(stagingRoot));
    }

    [Fact]
    public async Task MaterializeFiles_EnforcesSingleFileLengthAndCleansOperationDirectory()
    {
        using var scope = new TemporaryDirectory();
        var stagingRoot = scope.CreateDirectory("staging");
        var sourcePath = Path.Combine(scope.Path, "large.bin");
        await File.WriteAllBytesAsync(sourcePath, [1, 2, 3, 4]);
        var materializer = new StorageItemMaterializer(
            stagingRoot,
            maximumFileCount: 10,
            maximumFileLength: 3,
            maximumTotalLength: 100);

        await Assert.ThrowsAsync<IOException>(() =>
            materializer.MaterializeFilesAsync([CreateStorageFile(sourcePath)]));

        Assert.Empty(Directory.EnumerateFileSystemEntries(stagingRoot));
    }

    [Fact]
    public async Task MaterializeFiles_AcceptsResourceLimitsAtBoundary()
    {
        using var scope = new TemporaryDirectory();
        var stagingRoot = scope.CreateDirectory("staging");
        var sourcePath = Path.Combine(scope.Path, "boundary.bin");
        await File.WriteAllBytesAsync(sourcePath, [1, 2, 3, 4]);
        var materializer = new StorageItemMaterializer(
            stagingRoot,
            maximumFileCount: 1,
            maximumFileLength: 4,
            maximumTotalLength: 4);

        var path = Assert.Single(await materializer.MaterializeFilesAsync(
            [CreateStorageFile(sourcePath)]));

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task MaterializeFolders_EnforcesTotalLengthAndCleansOperationDirectory()
    {
        using var scope = new TemporaryDirectory();
        var stagingRoot = scope.CreateDirectory("staging");
        var sourceRoot = scope.CreateDirectory("source");
        await File.WriteAllTextAsync(Path.Combine(sourceRoot, "first.txt"), "123");
        await File.WriteAllTextAsync(Path.Combine(sourceRoot, "second.txt"), "456");
        var materializer = new StorageItemMaterializer(
            stagingRoot,
            maximumFileCount: 10,
            maximumFileLength: 100,
            maximumTotalLength: 5);

        await Assert.ThrowsAsync<IOException>(() =>
            materializer.MaterializeFoldersAsync(
                [CreateStorageFolder(sourceRoot)]));

        Assert.Empty(Directory.EnumerateFileSystemEntries(stagingRoot));
    }

    [Fact]
    public void DeleteOperationsOlderThan_MissingRootReturnsZeroWithoutCreatingIt()
    {
        using var scope = new TemporaryDirectory();
        var stagingRoot = Path.Combine(scope.Path, "missing-staging");
        var materializer = new StorageItemMaterializer(stagingRoot);

        var deleted = materializer.DeleteOperationsOlderThan(TimeSpan.Zero);

        Assert.Equal(0, deleted);
        Assert.False(Directory.Exists(stagingRoot));
    }

    [Fact]
    public void DeleteOperationsOlderThan_RejectsNegativeRetention()
    {
        using var scope = new TemporaryDirectory();
        var materializer = new StorageItemMaterializer(
            Path.Combine(scope.Path, "missing-staging"));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            materializer.DeleteOperationsOlderThan(TimeSpan.FromTicks(-1)));
    }

    [Fact]
    public void DeleteOperationsOlderThan_DeletesExpiredAndKeepsRecentOperations()
    {
        using var scope = new TemporaryDirectory();
        var stagingRoot = scope.CreateDirectory("staging");
        var expiredDirectory = Directory.CreateDirectory(
            Path.Combine(stagingRoot, "expired")).FullName;
        var recentDirectory = Directory.CreateDirectory(
            Path.Combine(stagingRoot, "recent")).FullName;
        File.WriteAllText(Path.Combine(expiredDirectory, "expired.txt"), "expired");
        File.WriteAllText(Path.Combine(recentDirectory, "recent.txt"), "recent");
        Directory.SetLastWriteTimeUtc(
            expiredDirectory,
            DateTime.UtcNow - TimeSpan.FromDays(30));
        var materializer = new StorageItemMaterializer(stagingRoot);

        var deleted = materializer.DeleteOperationsOlderThan(
            TimeSpan.FromDays(7));

        Assert.Equal(1, deleted);
        Assert.False(Directory.Exists(expiredDirectory));
        Assert.True(Directory.Exists(recentDirectory));
        Assert.True(File.Exists(Path.Combine(recentDirectory, "recent.txt")));
    }

    [Fact]
    public void TryDeleteFile_DoesNotReplaceTheOriginalFailure()
    {
        var exception = Record.Exception(
            () => StorageItemMaterializer.TryDeleteFile("\0"));

        Assert.Null(exception);
    }

    private static IStorageFile CreateStorageFile(string path) =>
        (IStorageFile)CreateBclStorageItem(
            "Avalonia.Platform.Storage.FileIO.BclStorageFile",
            new FileInfo(path));

    private static IStorageFolder CreateStorageFolder(string path) =>
        (IStorageFolder)CreateBclStorageItem(
            "Avalonia.Platform.Storage.FileIO.BclStorageFolder",
            new DirectoryInfo(path));

    private static IStorageFile CreateCallbackStorageFile(
        string name,
        Func<Task<Stream>> openRead,
        Action onDisposed)
    {
        var file = DispatchProxy.Create<IStorageFile, CallbackDispatchProxy>();
        ((CallbackDispatchProxy)(object)file).Handler = method => method.Name switch
        {
            "get_Name" => name,
            "OpenReadAsync" => openRead(),
            "Dispose" => Invoke(onDisposed),
            _ => throw new NotSupportedException(
                $"Unexpected storage file call: {method.Name}")
        };
        return file;
    }

    private static object? Invoke(Action action)
    {
        action();
        return null;
    }

    private static object CreateBclStorageItem(string typeName, object fileSystemInfo)
    {
        var type = typeof(IStorageItem).Assembly.GetType(typeName, throwOnError: true)!;
        var constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [fileSystemInfo.GetType()],
            modifiers: null)
            ?? throw new InvalidOperationException(
                $"Avalonia storage constructor is unavailable: {typeName}.");
        return constructor.Invoke([fileSystemInfo]);
    }

    public class CallbackDispatchProxy : DispatchProxy
    {
        public Func<MethodInfo, object?> Handler { get; set; } = null!;

        protected override object? Invoke(
            MethodInfo? targetMethod,
            object?[]? args) =>
            Handler(targetMethod ?? throw new InvalidOperationException(
                "Proxy method is unavailable."));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } =
            Directory.CreateTempSubdirectory("classisland-storage-materializer-").FullName;

        public string CreateDirectory(string name) =>
            Directory.CreateDirectory(System.IO.Path.Combine(Path, name)).FullName;

        public void Dispose() => Directory.Delete(Path, true);
    }
}
