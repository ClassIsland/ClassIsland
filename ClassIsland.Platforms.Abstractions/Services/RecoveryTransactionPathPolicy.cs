namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 选择应用数据恢复事务需要快照和替换的相对路径。
/// </summary>
internal static class RecoveryTransactionPathPolicy
{
    public static IReadOnlyList<string> SelectTransactionPaths(
        bool fullRecovery,
        IEnumerable<string> recoverablePaths,
        IEnumerable<string> presentPaths)
    {
        ArgumentNullException.ThrowIfNull(recoverablePaths);
        ArgumentNullException.ThrowIfNull(presentPaths);

        var recoverable = recoverablePaths
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (recoverable.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Recoverable paths cannot contain empty values.",
                nameof(recoverablePaths));
        }

        if (fullRecovery)
        {
            return recoverable;
        }

        var recoverableSet = recoverable.ToHashSet(StringComparer.Ordinal);
        return presentPaths
            .Where(recoverableSet.Contains)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
