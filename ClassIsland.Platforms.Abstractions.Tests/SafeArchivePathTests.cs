using ClassIsland.Platforms.Abstraction.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class SafeArchivePathTests
{
    [Theory]
    [InlineData("sound:name.", "sound_name")]
    [InlineData("CON.txt", "_CON.txt")]
    [InlineData("notes ", "notes")]
    [InlineData("bad?name.json", "bad_name.json")]
    public void SanitizeFileNameSegment_ProducesPortableArchiveName(
        string name,
        string expected)
    {
        var sanitized = SafeArchivePath.SanitizeFileNameSegment(name, "File");

        Assert.Equal(expected, sanitized);
        Assert.Equal(
            sanitized,
            SafeArchivePath.NormalizeFileSystemRelativePath(sanitized));
    }

    [Theory]
    [InlineData("Config/file:stream")]
    [InlineData("Config/file.")]
    [InlineData("Config/folder /file.json")]
    [InlineData("Config/folder./file.json")]
    [InlineData("Config/CON.txt")]
    [InlineData("Config/file?.json")]
    public void NormalizeRelativePath_RejectsWindowsCanonicalizationHazards(
        string path)
    {
        Assert.Throws<InvalidDataException>(() =>
            SafeArchivePath.NormalizeRelativePath(path));
    }

    [Theory]
    [InlineData("Config/settings.json", "Config/settings.json")]
    [InlineData("Profiles/main/", "Profiles/main/")]
    [InlineData(@"Config\settings.json", "Config/settings.json")]
    public void NormalizeRelativePath_NormalizesSafePortablePath(
        string path,
        string expected)
    {
        Assert.Equal(expected, SafeArchivePath.NormalizeRelativePath(path));
    }
}
