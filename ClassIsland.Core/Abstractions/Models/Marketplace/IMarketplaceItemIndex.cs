namespace ClassIsland.Core.Abstractions.Models.Marketplace;

/// <summary>
/// 市场内容索引
/// </summary>
public interface IMarketplaceItemIndex
{
    /// <summary>
    /// 索引集合
    /// </summary>
    public ICollection<IMarketplaceItemInfo> Index { get; set; }
}