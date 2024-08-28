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
    /// 检查课表是否符合当前的启用规则。
    /// </summary>
    /// <param name="plan">要检查的课表</param>
    /// <returns>是否满足启用规则</returns>
    //[Obsolete]
    //bool CheckClassPlan(ClassPlan plan);

    /// <summary>
    /// 清除过期的临时课表群。
    /// </summary>
    void ClearExpiredTempClassPlanGroup();

}