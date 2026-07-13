using ClassIsland.Platforms.Abstraction.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class PendingLaunchArgumentsStoreTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyPath(string path)
    {
        Assert.Throws<ArgumentException>(() => new PendingLaunchArgumentsStore(path));
    }

    [Fact]
    public void SaveAndConsume_ReturnsArgumentsOnlyOnce()
    {
        using var scope = new TemporaryDirectory();
        var path = Path.Combine(scope.Path, ".pending-launch.json");
        var store = new PendingLaunchArgumentsStore(path);

        store.Save(["-m", "-r"]);

        Assert.Equal(new[] { "-m", "-r" }, store.Consume());
        Assert.Empty(store.Consume());
    }

    [Fact]
    public void Consume_DeletesCorruptPayloadWithoutThrowing()
    {
        using var scope = new TemporaryDirectory();
        var path = Path.Combine(scope.Path, ".pending-launch.json");
        File.WriteAllText(path, "{not-json");
        var store = new PendingLaunchArgumentsStore(path);

        Assert.Empty(store.Consume());
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Consume_DoesNotFailColdStartWhenCleanupFails()
    {
        using var scope = new TemporaryDirectory();
        var path = Path.Combine(scope.Path, ".pending-launch.json");
        File.WriteAllText(path, "[\"--mobile\"]");
        var store = new PendingLaunchArgumentsStore(
            path,
            _ => throw new IOException("read-only file system"));

        Assert.Equal(new[] { "--mobile" }, store.Consume());
    }

    [Fact]
    public void Save_RejectsNullArguments()
    {
        using var scope = new TemporaryDirectory();
        var store = new PendingLaunchArgumentsStore(
            Path.Combine(scope.Path, ".pending-launch.json"));

        Assert.Throws<ArgumentNullException>(() => store.Save(null!));
    }

    [Fact]
    public void Save_CleansTemporaryPayloadWhenAtomicMoveFails()
    {
        using var scope = new TemporaryDirectory();
        var store = new PendingLaunchArgumentsStore(scope.Path);

        Assert.ThrowsAny<IOException>(() => store.Save(["--mobile"]));
        Assert.False(File.Exists(scope.Path + ".tmp"));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } =
            Directory.CreateTempSubdirectory("classisland-pending-launch-").FullName;

        public void Dispose() => Directory.Delete(Path, true);
    }
}
