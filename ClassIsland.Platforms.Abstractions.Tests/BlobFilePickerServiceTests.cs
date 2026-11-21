using System.Reflection;
using System.Security.Cryptography;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Stubs.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

[CollectionDefinition("Blob file picker", DisableParallelization = true)]
public sealed class BlobFilePickerCollection
{
}

[Collection("Blob file picker")]
public sealed class BlobFilePickerServiceTests
{
    static BlobFilePickerServiceTests()
    {
        // 纯单元测试不启动 AppBuilder，需注册与应用运行时相同的资源 URI 解析器。
        StandardAssetLoader.RegisterResUriParsers();
    }

    [Fact]
    public async Task OpenBlobFiles_UsesPlatformPickerAndStoresContentBySha256()
    {
        using var scope = new TemporaryBlobDirectory();
        var sourcePath = Path.Combine(scope.Path, "background.png");
        await File.WriteAllTextAsync(sourcePath, "abc");
        var service = new TestFilePickerService([CreateStorageFile(sourcePath)]);
        var options = new BlobFilePickerOpenOptions();
        const string hash = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

        var result = Assert.Single(await service.OpenBlobFilesPickerAsync(options, null!));

        Assert.Same(options, service.SelectedOptions);
        Assert.Equal(1, service.PickerCallCount);
        Assert.Equal(new Uri($"avares://*blobs/sha256/{hash}"), result);
        var blobPath = Assert.Single(Directory.EnumerateFiles(
            scope.BlobRootPath, "*", SearchOption.AllDirectories));
        Assert.Equal(Path.Combine(scope.BlobRootPath, hash[..2], hash), blobPath);
        Assert.Equal("abc", await File.ReadAllTextAsync(blobPath));
    }

    [Fact]
    public async Task OpenBlobFiles_DeduplicatesContentAcrossSelections()
    {
        using var scope = new TemporaryBlobDirectory();
        var firstPath = Path.Combine(scope.Path, "first.png");
        var secondPath = Path.Combine(scope.Path, "second.png");
        await File.WriteAllTextAsync(firstPath, "same content");
        await File.WriteAllTextAsync(secondPath, "same content");
        var service = new TestFilePickerService(
            [CreateStorageFile(firstPath), CreateStorageFile(secondPath)]);

        var results = await service.OpenBlobFilesPickerAsync(new BlobFilePickerOpenOptions(), null!);
        var repeated = new TestFilePickerService([CreateStorageFile(firstPath)]);
        var repeatedResult = Assert.Single(await repeated.OpenBlobFilesPickerAsync(
            new BlobFilePickerOpenOptions(), null!));

        Assert.Equal(2, results.Count);
        Assert.Equal(results[0], results[1]);
        Assert.Equal(results[0], repeatedResult);
        var blobPath = Assert.Single(Directory.EnumerateFiles(
            scope.BlobRootPath, "*", SearchOption.AllDirectories));
        Assert.Equal("same content", await File.ReadAllTextAsync(blobPath));
        Assert.Empty(Directory.EnumerateFiles(scope.BlobRootPath, "*.tmp"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task OpenBlobFiles_SkipsOversizedFilesAndAcceptsExactLimit(int maximumSize)
    {
        using var scope = new TemporaryBlobDirectory();
        var oversizedPath = Path.Combine(scope.Path, "oversized.png");
        var acceptedPath = Path.Combine(scope.Path, "accepted.png");
        var content = new byte[maximumSize];
        await File.WriteAllBytesAsync(oversizedPath, new byte[maximumSize + 1]);
        await File.WriteAllBytesAsync(acceptedPath, content);
        var service = new TestFilePickerService(
            [CreateStorageFile(oversizedPath), CreateStorageFile(acceptedPath)]);
        var hash = Convert.ToHexStringLower(SHA256.HashData(content));

        var result = Assert.Single(await service.OpenBlobFilesPickerAsync(
            new BlobFilePickerOpenOptions { MaxFileSize = maximumSize }, null!));

        Assert.Equal(new Uri($"avares://*blobs/sha256/{hash}"), result);
        var blobPath = Assert.Single(Directory.EnumerateFiles(
            scope.BlobRootPath, "*", SearchOption.AllDirectories));
        Assert.Equal(content, await File.ReadAllBytesAsync(blobPath));
        Assert.Empty(Directory.EnumerateFiles(scope.BlobRootPath, "*.tmp"));
    }

    [Fact]
    public async Task OpenBlobFiles_CancelDoesNotCreateBlobDirectory()
    {
        using var scope = new TemporaryBlobDirectory();
        var service = new TestFilePickerService([]);

        Assert.Empty(await service.OpenBlobFilesPickerAsync(new BlobFilePickerOpenOptions(), null!));

        Assert.Equal(1, service.PickerCallCount);
        Assert.False(Directory.Exists(scope.BlobRootPath));
    }

    [Fact]
    public async Task OpenBlobFiles_RejectsInvalidOptionsBeforeOpeningPicker()
    {
        using var scope = new TemporaryBlobDirectory();
        var service = new TestFilePickerService([]);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.OpenBlobFilesPickerAsync(null!, null!));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.OpenBlobFilesPickerAsync(new BlobFilePickerOpenOptions { MaxFileSize = -1 }, null!));

        Assert.Equal(0, service.PickerCallCount);
        Assert.False(Directory.Exists(scope.BlobRootPath));
    }

    [Fact]
    public async Task OpenBlobFiles_DisposesEverySelectionWhenOpeningStreamFails()
    {
        using var scope = new TemporaryBlobDirectory();
        var disposeCounts = new int[2];
        var service = new TestFilePickerService(
        [
            CreateCallbackStorageFile(
                () => Task.FromException<Stream>(new IOException("open failed")),
                () => disposeCounts[0]++),
            CreateCallbackStorageFile(
                () => throw new InvalidOperationException("Later files must not be opened."),
                () => disposeCounts[1]++)
        ]);

        await Assert.ThrowsAsync<IOException>(() =>
            service.OpenBlobFilesPickerAsync(new BlobFilePickerOpenOptions(), null!));

        Assert.Equal([1, 1], disposeCounts);
        Assert.False(Directory.Exists(scope.BlobRootPath));
    }

    [Fact]
    public async Task OpenBlobFiles_CleansPartialBlobAndDisposesResourcesWhenReadingFails()
    {
        using var scope = new TemporaryBlobDirectory();
        var stream = new FailingReadStream();
        var disposeCounts = new int[2];
        var service = new TestFilePickerService(
        [
            CreateCallbackStorageFile(
                () => Task.FromResult<Stream>(stream),
                () => disposeCounts[0]++),
            CreateCallbackStorageFile(
                () => throw new InvalidOperationException("Later files must not be opened."),
                () => disposeCounts[1]++)
        ]);

        await Assert.ThrowsAsync<IOException>(() =>
            service.OpenBlobFilesPickerAsync(new BlobFilePickerOpenOptions(), null!));

        Assert.True(stream.IsDisposed);
        Assert.Equal([1, 1], disposeCounts);
        Assert.Empty(Directory.EnumerateFileSystemEntries(scope.BlobRootPath));
    }

    private static IStorageFile CreateStorageFile(string path)
    {
        var type = typeof(IStorageFile).Assembly.GetType(
            "Avalonia.Platform.Storage.FileIO.BclStorageFile", throwOnError: true)!;
        var constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [typeof(FileInfo)],
            modifiers: null)
            ?? throw new InvalidOperationException("Avalonia BclStorageFile constructor is unavailable.");
        return (IStorageFile)constructor.Invoke([new FileInfo(path)]);
    }

    private static IStorageFile CreateCallbackStorageFile(Func<Task<Stream>> openRead, Action onDisposed)
    {
        var file = DispatchProxy.Create<IStorageFile, CallbackDispatchProxy>();
        ((CallbackDispatchProxy)(object)file).Handler = method => method.Name switch
        {
            "OpenReadAsync" => openRead(),
            "Dispose" => Invoke(onDisposed),
            _ => throw new NotSupportedException($"Unexpected storage file call: {method.Name}")
        };
        return file;
    }

    private static object? Invoke(Action action)
    {
        action();
        return null;
    }

    public class CallbackDispatchProxy : DispatchProxy
    {
        public Func<MethodInfo, object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(targetMethod ?? throw new InvalidOperationException("Proxy method is unavailable."));
    }

    private sealed class TestFilePickerService(IReadOnlyList<IStorageFile> files)
        : AvaloniaDefaultPlatformFilePickerService
    {
        public int PickerCallCount { get; private set; }
        public BlobFilePickerOpenOptions? SelectedOptions { get; private set; }

        protected override Task<IReadOnlyList<IStorageFile>> OpenBlobStorageFilesPickerAsync(
            BlobFilePickerOpenOptions options,
            TopLevel root)
        {
            PickerCallCount++;
            SelectedOptions = options;
            return Task.FromResult(files);
        }
    }

    private sealed class FailingReadStream() : MemoryStream([1])
    {
        public bool IsDisposed { get; private set; }
        private bool _hasRead;

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_hasRead)
            {
                return ValueTask.FromException<int>(new IOException("read failed"));
            }

            _hasRead = true;
            return base.ReadAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class TemporaryBlobDirectory : IDisposable
    {
        private readonly string _previousBlobRoot = PlatformServices.BlobRootFolderPath;

        public string Path { get; } = Directory.CreateTempSubdirectory("classisland-blob-picker-").FullName;
        public string BlobRootPath { get; }

        public TemporaryBlobDirectory()
        {
            BlobRootPath = System.IO.Path.Combine(Path, "Blobs");
            PlatformServices.BlobRootFolderPath = BlobRootPath;
        }

        public void Dispose()
        {
            PlatformServices.BlobRootFolderPath = _previousBlobRoot;
            Directory.Delete(Path, true);
        }
    }
}
