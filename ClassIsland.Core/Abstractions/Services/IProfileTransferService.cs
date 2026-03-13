using System.Collections.ObjectModel;
using ClassIsland.Core.Models.Profile;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 档案数据迁移服务
/// </summary>
public interface IProfileTransferService
{
    /// <summary>
    /// 已注册的档案数据迁移提供方
    /// </summary>
    public static ObservableCollection<ProfileTransferProviderInfo> Providers { get; } = [];
}