using ClassIsland.Platforms.Abstraction.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class StreamExportHelperTests
{
    [Fact]
    public async Task WritePathBasedExportAsync_CopiesExporterOutputToDestination()
    {
        await using var destination = new MemoryStream();

        await StreamExportHelper.WritePathBasedExportAsync(
            destination,
            "cidata",
            path => File.WriteAllTextAsync(path, "archive-content"));

        Assert.Equal("archive-content", System.Text.Encoding.UTF8.GetString(destination.ToArray()));
    }

    [Fact]
    public async Task WritePathBasedExportAsync_CleansTemporaryFileWhenExporterFails()
    {
        await using var destination = new MemoryStream();
        string? temporaryPath = null;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            StreamExportHelper.WritePathBasedExportAsync(
                destination,
                ".zip",
                path =>
                {
                    temporaryPath = path;
                    throw new InvalidOperationException("export failed");
                }));

        Assert.NotNull(temporaryPath);
        Assert.False(Directory.Exists(Path.GetDirectoryName(temporaryPath)));
    }

    [Fact]
    public void TryDeleteTemporaryDirectory_IgnoresCleanupFailure()
    {
        var file = Path.GetTempFileName();
        try
        {
            var exception = Record.Exception(
                () => StreamExportHelper.TryDeleteTemporaryDirectory(file));

            Assert.Null(exception);
            Assert.True(File.Exists(file));
        }
        finally
        {
            File.Delete(file);
        }
    }
}
