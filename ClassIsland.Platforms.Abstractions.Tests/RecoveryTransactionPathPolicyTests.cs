using ClassIsland.Platforms.Abstraction.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class RecoveryTransactionPathPolicyTests
{
    private static readonly string[] RecoverablePaths =
    [
        "Settings.json",
        "Settings.json.bak",
        "Config",
        "Profiles"
    ];

    [Fact]
    public void SelectTransactionPaths_FullRecoveryIncludesMissingBackupPaths()
    {
        var paths = RecoveryTransactionPathPolicy.SelectTransactionPaths(
            true,
            RecoverablePaths,
            ["Settings.json"]);

        Assert.Equal(RecoverablePaths, paths);
    }

    [Fact]
    public void SelectTransactionPaths_IncrementalRecoveryOnlyIncludesPresentPaths()
    {
        var paths = RecoveryTransactionPathPolicy.SelectTransactionPaths(
            false,
            RecoverablePaths,
            ["Profiles", "unknown", "Profiles", "Settings.json"]);

        Assert.Equal(["Profiles", "Settings.json"], paths);
    }

    [Fact]
    public void SelectTransactionPaths_FullRecoveryDoesNotRequirePresentPaths()
    {
        var paths = RecoveryTransactionPathPolicy.SelectTransactionPaths(
            true,
            RecoverablePaths,
            []);

        Assert.Equal(RecoverablePaths, paths);
    }

    [Fact]
    public void SelectTransactionPaths_ValidatesArguments()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RecoveryTransactionPathPolicy.SelectTransactionPaths(
                false,
                null!,
                []));
        Assert.Throws<ArgumentNullException>(() =>
            RecoveryTransactionPathPolicy.SelectTransactionPaths(
                false,
                [],
                null!));
        Assert.Throws<ArgumentException>(() =>
            RecoveryTransactionPathPolicy.SelectTransactionPaths(
                true,
                ["Config", ""],
                []));
    }
}
