using ClassIsland.Shared.IPC.Abstractions.Services;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 档案服务，用于管理ClassIsland档案信息。
/// </summary>
public interface IProfileService : IPublicProfileService
{
    internal Task LoadProfileAsync();

    /// <summary>
    /// 清空过期的临时层课表
    /// </summary>
    void CleanExpiredTempClassPlan();

    /// <summary>
    /// 清除过期的临时课表群。
    /// </summary>
    void ClearExpiredTempClassPlanGroup();

    /// <summary>
    /// 将当前档案标记为信任。
    /// </summary>
    void TrustCurrentProfile();
}