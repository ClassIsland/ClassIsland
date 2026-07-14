using System.IO.Compression;

namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 在解压不受信任的 Zip 包前验证路径和资源用量上限。
/// </summary>
internal static class ZipArchiveSafety
{
    internal const int DefaultMaximumEntryCount = 4096;
    internal const long DefaultMaximumEntryLength = 64L * 1024 * 1024;
    internal const long DefaultMaximumTotalLength = 256L * 1024 * 1024;
    internal const double DefaultMaximumCompressionRatio = 200;
    private const long CompressionRatioCheckThreshold = 1024 * 1024;

    public static void ValidateForExtraction(ZipArchive archive)
    {
        ValidateForExtraction(
            archive,
            DefaultMaximumEntryCount,
            DefaultMaximumEntryLength,
            DefaultMaximumTotalLength,
            DefaultMaximumCompressionRatio);
    }

    internal static void ValidateForExtraction(
        ZipArchive archive,
        int maximumEntryCount,
        long maximumEntryLength,
        long maximumTotalLength,
        double maximumCompressionRatio)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumEntryCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumEntryLength);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumTotalLength);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumCompressionRatio);
        if (archive.Entries.Count > maximumEntryCount)
        {
            throw new InvalidDataException(
                $"压缩包条目数超过上限 {maximumEntryCount}。");
        }

        var comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var normalizedPaths = new HashSet<string>(comparer);
        long totalLength = 0;
        foreach (var entry in archive.Entries)
        {
            var normalizedPath = SafeRelativePath.Normalize(entry.FullName);
            if (!normalizedPaths.Add(normalizedPath))
            {
                throw new InvalidDataException(
                    $"压缩包包含重复路径：{entry.FullName}");
            }

            if (entry.Length < 0 || entry.CompressedLength < 0 ||
                entry.Length > maximumEntryLength)
            {
                throw new InvalidDataException(
                    $"压缩包条目大小无效或超过上限：{entry.FullName}");
            }

            try
            {
                totalLength = checked(totalLength + entry.Length);
            }
            catch (OverflowException exception)
            {
                throw new InvalidDataException("压缩包解压总大小溢出。", exception);
            }

            if (totalLength > maximumTotalLength)
            {
                throw new InvalidDataException(
                    $"压缩包解压总大小超过上限 {maximumTotalLength} 字节。");
            }

            if (entry.Length >= CompressionRatioCheckThreshold &&
                (entry.CompressedLength == 0 ||
                 entry.Length / (double)entry.CompressedLength > maximumCompressionRatio))
            {
                throw new InvalidDataException(
                    $"压缩包条目压缩比异常：{entry.FullName}");
            }
        }
    }
}
