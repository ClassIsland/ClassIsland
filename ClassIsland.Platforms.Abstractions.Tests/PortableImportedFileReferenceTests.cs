using ClassIsland.Platforms.Abstraction.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class PortableImportedFileReferenceTests
{
    private const string ItemDirectory = "0123456789abcdef0123456789abcdef";

    [Fact]
    public void PortableReference_ResolvesOnEveryPlatform()
    {
        using var scope = new TemporaryDirectory();
        var reference =
            $"{PortableImportedFileReference.Prefix}{ItemDirectory}/custom%20sound.wav";

        Assert.True(PortableImportedFileReference.TryResolve(
            reference,
            scope.Path,
            migrateLegacyAppleAbsolutePath: false,
            out var path));
        Assert.Equal(
            Path.Combine(scope.Path, ItemDirectory, "custom sound.wav"),
            path);
    }

    [Theory]
    [InlineData("/private/var/mobile/Containers/Data/Application/00000000-0000-0000-0000-000000000000/Documents/ClassIsland/ImportedFiles/0123456789abcdef0123456789abcdef/logo.png")]
    [InlineData("/var/mobile/Containers/Data/Application/00000000-0000-0000-0000-000000000000/Documents/ClassIsland/Data/ImportedFiles/0123456789abcdef0123456789abcdef/logo.png")]
    public void LegacyApplePath_ResolvesOnlyWhenMigrationIsEnabled(
        string legacyPath)
    {
        using var scope = new TemporaryDirectory();

        Assert.False(PortableImportedFileReference.TryResolve(
            legacyPath,
            scope.Path,
            migrateLegacyAppleAbsolutePath: false,
            out var unchangedPath));
        Assert.Equal(legacyPath, unchangedPath);

        Assert.True(PortableImportedFileReference.TryResolve(
            legacyPath,
            scope.Path,
            migrateLegacyAppleAbsolutePath: true,
            out var migratedPath));
        Assert.Equal(
            Path.Combine(scope.Path, ItemDirectory, "logo.png"),
            migratedPath);
    }

    [Theory]
    [InlineData("/home/user/ClassIsland/ImportedFiles/item/sound.wav")]
    [InlineData("C:/Users/user/ClassIsland/ImportedFiles/item/sound.wav")]
    [InlineData("C:/Users/user/Documents/ClassIsland/ImportedFiles/item/sound.wav")]
    [InlineData("/home/user/Documents/ClassIsland/ImportedFiles/item/sound.wav")]
    [InlineData("/home/user/Documents/Other/ClassIsland/ImportedFiles/item/sound.wav")]
    [InlineData("/private/var/mobile/OLD/Documents/ClassIsland/ImportedFiles/item/sound.wav")]
    [InlineData("/private/var/mobile/Containers/Data/Application/00000000-0000-0000-0000-000000000000/Library/Documents/ClassIsland/ImportedFiles/item/sound.wav")]
    [InlineData(@"\private\var\mobile\Containers\Data\Application\00000000-0000-0000-0000-000000000000\Documents\ClassIsland\ImportedFiles\item\sound.wav")]
    public void UnrelatedDesktopPath_IsNeverMigrated(string path)
    {
        using var scope = new TemporaryDirectory();

        Assert.False(PortableImportedFileReference.TryResolve(
            path,
            scope.Path,
            migrateLegacyAppleAbsolutePath: true,
            out var unchangedPath));
        Assert.Equal(path, unchangedPath);
    }

    [Theory]
    [InlineData("_classisland-imported:item/../outside.wav")]
    [InlineData("_classisland-imported:item/%2E%2E/outside.wav")]
    [InlineData("_classisland-imported:item/%2Foutside.wav")]
    public void PortableReference_RejectsTraversal(string reference)
    {
        using var scope = new TemporaryDirectory();

        Assert.Throws<FormatException>(() =>
            PortableImportedFileReference.TryResolve(
                reference,
                scope.Path,
                migrateLegacyAppleAbsolutePath: false,
                out _));
    }

    [Fact]
    public void Create_RejectsPathOutsideImportedRoot()
    {
        using var importedRoot = new TemporaryDirectory();
        using var outsideRoot = new TemporaryDirectory();
        var path = Path.Combine(outsideRoot.Path, "outside.wav");

        Assert.Throws<ArgumentException>(() =>
            PortableImportedFileReference.Create(path, importedRoot.Path));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } =
            Directory.CreateTempSubdirectory(
                "classisland-imported-reference-").FullName;

        public void Dispose() => Directory.Delete(Path, true);
    }
}
