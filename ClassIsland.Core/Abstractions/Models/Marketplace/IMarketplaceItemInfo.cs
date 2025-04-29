using System.ComponentModel;
using Newtonsoft.Json;

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
    public bool IsAvailableOnMarket { get; set; }
}