namespace ClassIsland.Shared.Models.Management;

/// <summary>
/// 集控持久存储信息
/// </summary>
public class ManagementClientPersistConfig
{
    /// <summary>
    /// 客户端集控唯一id
    /// </summary>
    public Guid ClientUniqueId { get; set; } = Guid.NewGuid();
}