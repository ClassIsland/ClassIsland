using System.IO.Compression;
using ClassIsland.Platforms.Abstraction.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class ZipArchiveSafetyTests
{
    [Fact]
    public void ValidArchive_IsAccepted()
    {
        using var archive = CreateArchive(("manifest.yml", "id: example"));

        ZipArchiveSafety.ValidateForExtraction(archive);
    }

    [Fact]
    public void ClassIslandDataBudget_CoversPersistentImportBudget()
    {
        Assert.Equal(
            StorageItemMaterializer.DefaultMaximumFileLength,
            ZipArchiveSafety.ClassIslandDataMaximumEntryLength);
        Assert.True(
            ZipArchiveSafety.ClassIslandDataMaximumTotalLength >
            StorageItemMaterializer.DefaultMaximumTotalLength);
        Assert.True(
            ZipArchiveSafety.ClassIslandDataMaximumEntryCount >
            StorageItemMaterializer.DefaultMaximumFileCount);
    }

    [Fact]
    public void ClassIslandDataValidation_AcceptsValidArchive()
    {
        using var archive = CreateArchive(("ImportedFiles/item/sound.wav", "sound"));

        ZipArchiveSafety.ValidateForClassIslandDataExtraction(archive);
    }

    [Fact]
    public void ClassIslandDataValidation_UsesPortablePathRulesWithoutChangingDefaultPolicy()
    {
        using var archive = CreateArchive(("Config/file:stream", "content"));

        ZipArchiveSafety.ValidateForExtraction(archive);
        Assert.Throws<InvalidDataException>(() =>
            ZipArchiveSafety.ValidateForClassIslandDataExtraction(archive));
    }

    [Fact]
    public void TraversalEntry_IsRejected()
    {
        using var archive = CreateArchive(("Profiles/../Settings.json", "{}"));

        Assert.Throws<InvalidDataException>(() =>
            ZipArchiveSafety.ValidateForExtraction(archive));
    }

    [Fact]
    public void DuplicateCanonicalEntry_IsRejected()
    {
        using var archive = CreateArchive(
            ("Config/Settings.json", "first"),
            ("Config\\Settings.json", "second"));

        Assert.Throws<InvalidDataException>(() =>
            ZipArchiveSafety.ValidateForExtraction(archive));
    }

    [Fact]
    public void EntryCountLimit_IsEnforced()
    {
        using var archive = CreateArchive(("one", "1"), ("two", "2"));

        Assert.Throws<InvalidDataException>(() =>
            ZipArchiveSafety.ValidateForExtraction(archive, 1, 100, 100, 100));
    }

    [Fact]
    public void TotalLengthLimit_IsEnforced()
    {
        using var archive = CreateArchive(("one", "12345"), ("two", "67890"));

        Assert.Throws<InvalidDataException>(() =>
            ZipArchiveSafety.ValidateForExtraction(archive, 10, 10, 9, 100));
    }

    [Fact]
    public void EntryLengthLimit_IsEnforced()
    {
        using var archive = CreateArchive(("large", "123456"));

        Assert.Throws<InvalidDataException>(() =>
            ZipArchiveSafety.ValidateForExtraction(archive, 10, 5, 100, 100));
    }

    [Fact]
    public void CompressionRatioLimit_IsEnforced()
    {
        using var archive = CreateArchive(("large", new string('0', 2 * 1024 * 1024)));

        Assert.Throws<InvalidDataException>(() =>
            ZipArchiveSafety.ValidateForExtraction(
                archive,
                10,
                3 * 1024 * 1024,
                3 * 1024 * 1024,
                2));
    }

    private static ZipArchive CreateArchive(params (string Path, string Content)[] entries)
    {
        var stream = new MemoryStream();
        using (var writer = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            foreach (var (path, content) in entries)
            {
                var entry = writer.CreateEntry(path, CompressionLevel.SmallestSize);
                using var textWriter = new StreamWriter(entry.Open());
                textWriter.Write(content);
            }
        }

        stream.Position = 0;
        return new ZipArchive(stream, ZipArchiveMode.Read);
    }
}
