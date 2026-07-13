using System.Reflection;
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
    public async Task MaterializeFiles_ReturnsLocalPathWithoutReadingFile()
    {
        using var scope = new TemporaryDirectory();
        var path = Path.Combine(scope.Path, "plugin.cipx");
        await File.WriteAllTextAsync(path, "plugin");
        using var file = CreateStorageFile(path);
        var service = new AvaloniaDefaultPlatformFilePickerService();

        var paths = await service.MaterializeFilesAsync([file]);

        Assert.Equal(Path.GetFullPath(path), Assert.Single(paths));
    }

    private static IStorageFile CreateStorageFile(string path)
    {
        var type = typeof(IStorageFile).Assembly.GetType(
            "Avalonia.Platform.Storage.FileIO.BclStorageFile",
            throwOnError: true)!;
        var constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(FileInfo)],
            modifiers: null)
            ?? throw new InvalidOperationException("Avalonia BclStorageFile constructor is unavailable.");
        return (IStorageFile)constructor.Invoke([new FileInfo(path)]);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } =
            Directory.CreateTempSubdirectory("classisland-default-picker-").FullName;

        public void Dispose() => Directory.Delete(Path, true);
    }
}
