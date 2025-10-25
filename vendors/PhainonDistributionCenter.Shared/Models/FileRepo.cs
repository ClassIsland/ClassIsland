using System.Collections.Generic;

namespace PhainonDistributionCenter.Shared.Models;

/// <summary>
/// 代表一个文件仓库
/// </summary>
public class FileRepo
{
    /// <summary>
    /// 文件仓库的内容，键为文件哈希（base64）
    /// </summary>
    public Dictionary<string, FileRepoItem> Items { get; set; } = [];
}