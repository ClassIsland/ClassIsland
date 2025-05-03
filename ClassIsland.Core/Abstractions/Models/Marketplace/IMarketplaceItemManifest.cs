using System.ComponentModel;

namespace ClassIsland.Core.Abstractions.Models.Marketplace;

/// <summary>
/// 代表市场内容的清单信息
/// </summary>
public interface IMarketplaceItemManifest : INotifyPropertyChanged
{
    /// <summary>
    /// 显示名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 版本
    /// </summary>
    public string Version { get; set; }
}