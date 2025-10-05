namespace PhainonDistributionCenter.Shared.Models;

/// <summary>
/// 代表文件仓库的一个项目。
/// </summary>
public class FileRepoItem
{
    /// <summary>
    /// 代表这个文件本身的 SHA512 校验值
    /// </summary>
    public byte[] FileSha512 { get; set; } = [];
    
    /// <summary>
    /// 代表这个文件的压缩档的 SHA512 校验值
    /// </summary>
    public byte[] ArchiveSha512 { get; set; } = [];

    /// <summary>
    /// 这个文件的文件名
    /// </summary>
    public string FileName { get; set; } = "";

    /// <summary>
    /// 这个文件的下载链接
    /// </summary>
    public string ArchiveDownloadUrl { get; set; } = "";
}