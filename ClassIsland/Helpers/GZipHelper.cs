using System.IO;
using System.IO.Compression;

namespace ClassIsland.Helpers;

public static class GZipHelper
{
    public static void CompressFileAndDelete(string path)
    {
        using var originalFileStream = File.Open(path, FileMode.Open);
        using var compressedFileStream = File.Create(path + ".gz");
        using var compressor = new GZipStream(compressedFileStream, CompressionMode.Compress);
        originalFileStream.CopyTo(compressor);
        compressor.Close();
        originalFileStream.Close();
        File.Delete(path);
    }
}