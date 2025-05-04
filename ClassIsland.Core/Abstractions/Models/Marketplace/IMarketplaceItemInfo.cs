using System.ComponentModel;
using System.Text.Json.Serialization;
using ClassIsland.Core.Models;

namespace ClassIsland.Core.Abstractions.Models.Marketplace;

/// <summary>
/// 代表市场内容的基本信息
/// </summary>
public interface IMarketplaceItemInfo : INotifyPropertyChanged
{
    /// <summary>
    /// 对象清单
    /// </summary>
    [JsonIgnore]
    public IMarketplaceItemManifest ManifestReadonly { get; }

    /// <summary>
    /// 是否在市场上可用
    /// </summary>
    [JsonIgnore]
    public bool IsAvailableOnMarket { get; }

    /// <summary>
    /// 是否存在于本地
    /// </summary>
    [JsonIgnore] 
    public bool IsLocal { get; }

    /// <summary>
    /// 关联的下栽进度
    /// </summary>
    [JsonIgnore]
    public DownloadProgress? DownloadProgress { get; set; }
}