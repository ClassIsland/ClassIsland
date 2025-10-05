using System.IO;

namespace PhainonDistributionCenter.Shared.Models.Client;

/// <summary>
/// 代表客户端侧获取的发行信息
/// </summary>
public class DistributionInfoClient
{
    /// <summary>
    /// 友好版本号，如 2.0-Khaslana Release 1
    /// </summary>
    public string FriendlyVersion { get; set; } = "";
    
    /// <summary>
    /// 友好短版本号，如 2.0-Khaslana R1
    /// </summary>
    public string FriendlyVersionShort { get; set; } = "";
    
    /// <summary>
    /// 具体版本，如 2.0.0.0
    /// </summary>
    public string Version { get; set; } = "";

    /// <summary>
    /// 更新日志
    /// </summary>
    public string ChangeLog { get; set; } = "";

    /// <summary>
    /// 子频道 Id
    /// </summary>
    public string SubChannel { get; set; } = "unknown_unknown_full_folderClassic";

    /// <summary>
    /// 对应发行子频道的文件图
    /// </summary>
    public string FileMapJson { get; set; } = "";
    
    /// <summary>
    /// 对应发行子频道的文件图签名
    /// </summary>
    public string FileMapSignature { get; set; } = "";
}