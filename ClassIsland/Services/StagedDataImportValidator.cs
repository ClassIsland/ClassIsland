using System.Text.Json;

namespace ClassIsland.Services;

/// <summary>
/// 在替换在线数据前验证暂存区中的配置文件。
/// </summary>
internal static class StagedDataImportValidator
{
    public static void ValidateJsonFile(string path, bool requireObject = false)
    {
        ValidateJsonFile(
            path,
            requireObject ? JsonValueKind.Object : null);
    }

    public static void ValidateJsonFile(
        string path,
        JsonValueKind expectedRootKind)
    {
        ValidateJsonFile(path, (JsonValueKind?)expectedRootKind);
    }

    public static IReadOnlyList<string> ValidateProfileDirectory(string path)
    {
        return ValidateProfileDirectory(
            path,
            candidate => ValidateJsonFile(candidate, true));
    }

    public static IReadOnlyList<string> ValidateProfileDirectory(
        string path,
        Action<string> validateCandidate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(validateCandidate);
        if (!Directory.Exists(path))
        {
            throw new InvalidDataException("数据文件中不包含档案目录。");
        }

        var primaryProfiles = EnumeratePrimaryJsonFiles(path);
        if (primaryProfiles.Length == 0)
        {
            throw new InvalidDataException(
                "数据文件中不包含有效的档案 JSON 文件。");
        }

        foreach (var file in primaryProfiles)
        {
            _ = LoadPrimaryOrBackup(
                file,
                candidate =>
                {
                    validateCandidate(candidate);
                    return true;
                });
        }

        return primaryProfiles;
    }

    public static string[] EnumeratePrimaryJsonFiles(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Directory.Exists(path))
        {
            return [];
        }

        return Directory.EnumerateFiles(path)
            .Select(ToPrimaryJsonPath)
            .Where(path => path != null)
            .Select(path => path!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    public static T LoadPrimaryOrBackup<T>(
        string primaryPath,
        Func<string, T> loadAndValidate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryPath);
        ArgumentNullException.ThrowIfNull(loadAndValidate);

        var primaryResult = TryLoad(primaryPath, loadAndValidate);
        if (primaryResult.Success)
        {
            return primaryResult.Value!;
        }

        var backupPath = primaryPath + ".bak";
        var backupResult = TryLoad(backupPath, loadAndValidate);
        if (!backupResult.Success)
        {
            throw new InvalidDataException(
                $"配置主文件及其备份均无效：{primaryPath}",
                new AggregateException(
                    primaryResult.Error!,
                    backupResult.Error!));
        }

        PromoteBackup(backupPath, primaryPath);
        return backupResult.Value!;
    }

    private static void ValidateJsonFile(
        string path,
        JsonValueKind? expectedRootKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            using var input = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            using var document = JsonDocument.Parse(input);
            if (expectedRootKind is { } expected &&
                document.RootElement.ValueKind != expected)
            {
                throw new InvalidDataException(
                    $"JSON 配置根节点类型无效，期望 {expected}：{path}");
            }
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is JsonException or IOException)
        {
            throw new InvalidDataException(
                $"无法读取 JSON 配置：{path}",
                exception);
        }
    }

    private static (bool Success, T? Value, InvalidDataException? Error)
        TryLoad<T>(
            string path,
            Func<string, T> loadAndValidate)
    {
        if (!File.Exists(path))
        {
            return (
                false,
                default,
                new InvalidDataException($"找不到配置文件：{path}"));
        }

        try
        {
            return (true, loadAndValidate(path), null);
        }
        catch (InvalidDataException exception)
        {
            return (false, default, exception);
        }
    }

    private static string? ToPrimaryJsonPath(string path)
    {
        if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        return path.EndsWith(".json.bak", StringComparison.OrdinalIgnoreCase)
            ? path[..^4]
            : null;
    }

    private static void PromoteBackup(string backupPath, string primaryPath)
    {
        var directory = Path.GetDirectoryName(primaryPath);
        if (string.IsNullOrEmpty(directory))
        {
            directory = Directory.GetCurrentDirectory();
        }

        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(primaryPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var source = new FileStream(
                       backupPath,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.Read))
            using (var destination = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            {
                source.CopyTo(destination);
                destination.Flush(true);
            }

            File.Move(temporaryPath, primaryPath, true);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidDataException(
                $"无法在导入暂存区中用备份恢复配置主文件：{primaryPath}",
                exception);
        }
        finally
        {
            FileSystemDataTransaction.TryDeleteFile(temporaryPath);
        }
    }
}
