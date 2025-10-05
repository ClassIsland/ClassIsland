using System.Collections.Generic;

namespace PhainonDistributionCenter.Shared.Models.FileMap;

/// <summary>
/// 代表一个文件图组件。
/// </summary>
public class FileMapComponent
{
    /// <summary>
    /// 文件组件内包含的文件
    /// </summary>
    public Dictionary<string, FileMapFile> Files { get; set; } = new();

    /// <summary>
    /// 此组件允许差分更新
    /// </summary>
    public bool AllowDiffUpdate { get; set; } = false;

    /// <summary>
    /// 组件部署根路径
    /// </summary>
    public string Root { get; set; } = "";
}