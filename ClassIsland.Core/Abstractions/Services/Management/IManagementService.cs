using ClassIsland.Shared.Abstraction.Services;
using ClassIsland.Shared.Models.Management;

namespace ClassIsland.Core.Abstractions.Services.Management;

/// <summary>
/// 集控服务。
/// </summary>
public interface IManagementService
{
    /// <summary>
    /// 是否启用集控
    /// </summary>
    bool IsManagementEnabled { get; set; }
    
    /// <summary>
    /// 集控配置版本
    /// </summary>
    ManagementVersions Versions { get; set; }
    
    /// <summary>
    /// 集控清单
    /// </summary>
    ManagementManifest Manifest { get; set; }
    
    /// <summary>
    /// 集控服务器配置
    /// </summary>
    ManagementSettings Settings { get; }
    
    /// <summary>
    /// 集控策略
    /// </summary>
    ManagementPolicy Policy { get; set; }
    
    /// <summary>
    /// 集控持久配置
    /// </summary>
    ManagementClientPersistConfig Persist { get; }
    
    /// <summary>
    /// 集控服务器连接
    /// </summary>
    IManagementServerConnection? Connection { get; }
    internal Task SetupManagement();
    internal void SaveSettings();
    
    /// <summary>
    /// 加入集控服务器。
    /// </summary>
    /// <param name="settings">集控服务器配置</param>
    Task JoinManagementAsync(ManagementSettings settings);
    
    /// <summary>
    /// 退出集控服务器。
    /// </summary>
    Task ExitManagementAsync();
}