namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 将现有基于文件路径的导出器适配到平台授权的输出流。
/// </summary>
internal static class StreamExportHelper
{
    public static async Task WritePathBasedExportAsync(
        Stream destination,
        string extension,
        Func<string, Task> exporter)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(exporter);

        var temporaryDirectory = Directory.CreateTempSubdirectory("ClassIslandExport-");
        var normalizedExtension = string.IsNullOrEmpty(extension)
            ? string.Empty
            : extension.StartsWith('.') ? extension : $".{extension}";
        var temporaryPath = Path.Combine(
            temporaryDirectory.FullName,
            $"Export{normalizedExtension}");
        try
        {
            await exporter(temporaryPath);
            await using var input = new FileStream(
                temporaryPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            await input.CopyToAsync(destination);
        }
        finally
        {
            TryDeleteTemporaryDirectory(temporaryDirectory.FullName);
        }
    }

    internal static void TryDeleteTemporaryDirectory(string path)
    {
        try
        {
            Directory.Delete(path, true);
        }
        catch
        {
            // 临时目录残留不应覆盖导出阶段的成功结果或原始异常。
        }
    }
}
