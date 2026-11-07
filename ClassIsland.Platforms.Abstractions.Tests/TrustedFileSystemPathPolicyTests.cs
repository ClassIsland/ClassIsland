using ClassIsland.Platforms.Abstraction.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class TrustedFileSystemPathPolicyTests
{
    [Fact]
    public void GetControlledComponents_ExcludesSystemAncestorsOfTrustedRoot()
    {
        var systemRoot = Path.GetPathRoot(Environment.CurrentDirectory)!;
        var trustedRoot = Path.Combine(
            systemRoot,
            "var",
            "mobile",
            "Containers",
            "Data",
            "Application",
            "00000000-0000-0000-0000-000000000000",
            "Documents",
            "ClassIsland");
        var path = Path.Combine(trustedRoot, "Data", "ImportedFiles");

        var components = TrustedFileSystemPathPolicy.GetControlledComponents(
            path,
            trustedRoot);

        Assert.Equal(
            [
                Path.GetFullPath(trustedRoot),
                Path.Combine(Path.GetFullPath(trustedRoot), "Data"),
                Path.Combine(
                    Path.GetFullPath(trustedRoot),
                    "Data",
                    "ImportedFiles")
            ],
            components);
        Assert.DoesNotContain(
            Path.Combine(systemRoot, "var"),
            components);
    }

    [Fact]
    public void GetControlledComponents_SupportsSiblingIosRollbackRoot()
    {
        var containerRoot = Path.Combine(
            Path.GetPathRoot(Environment.CurrentDirectory)!,
            "private",
            "var",
            "mobile",
            "Containers",
            "Data",
            "Application",
            "00000000-0000-0000-0000-000000000000");
        var rollbackRoot = Path.Combine(containerRoot, "tmp", "rollback");

        var components = TrustedFileSystemPathPolicy.GetControlledComponents(
            Path.Combine(rollbackRoot, "Config", "settings.json"),
            rollbackRoot);

        Assert.Equal(Path.GetFullPath(rollbackRoot), components[0]);
        Assert.Equal(3, components.Count);
    }

    [Fact]
    public void GetControlledComponents_RejectsSiblingPath()
    {
        using var scope = new TemporaryDirectory();
        var trustedRoot = Path.Combine(scope.Path, "live");
        var sibling = Path.Combine(scope.Path, "live-shadow", "settings.json");

        Assert.Throws<InvalidDataException>(() =>
            TrustedFileSystemPathPolicy.GetControlledComponents(
                sibling,
                trustedRoot));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } =
            Directory.CreateTempSubdirectory(
                "classisland-trusted-path-policy-").FullName;

        public void Dispose() => Directory.Delete(Path, true);
    }
}
