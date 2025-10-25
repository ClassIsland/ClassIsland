using System.Collections.Generic;

namespace PhainonDistributionCenter.Shared.Models.FileMap;

/// <summary>
/// 代表一个文件图
/// </summary>
public class FileMap
{
    /// <summary>
    /// 文件图中包含的组件
    /// </summary>
    public Dictionary<string, FileMapComponent> Components { get; set; } = new();

    /// <summary>
    /// 文件图变量列表
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = [];

    /// <summary>
    /// 文件图包含的内容整体下载链接
    /// </summary>
    public string ArchiveUrl { get; set; } = "";

    /// <summary>
    /// 文件图包含的内容整体的压缩包的校验和
    /// </summary>
    public byte[] ArchiveSha512 { get; set; } = [];
}