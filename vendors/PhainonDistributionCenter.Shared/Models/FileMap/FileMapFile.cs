namespace PhainonDistributionCenter.Shared.Models.FileMap;

/// <summary>
/// 代表文件图中的一个文件
/// </summary>
public class FileMapFile
{
    /// <summary>
    /// 文件归档下载路径
    /// </summary>
    public string ArchiveDownloadUrl { get; set; } = "";

    /// <summary>
    /// 文件本体的 SHA512 校验和
    /// </summary>
    public byte[] FileSha512 { get; set; } = [];

    /// <summary>
    /// 文件归档的 SHA512 校验和
    /// </summary>
    public byte[] ArchiveSha512 { get; set; } = [];
}