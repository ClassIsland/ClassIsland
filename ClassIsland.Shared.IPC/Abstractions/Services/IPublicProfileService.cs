using ClassIsland.Shared.Models.Profile;
using dotnetCampus.Ipc.CompilerServices.Attributes;

namespace ClassIsland.Shared.IPC.Abstractions.Services;

/// <summary>
/// 向其它进程公开的档案服务，用于管理ClassIsland档案信息。
/// </summary>
[IpcPublic(IgnoresIpcException = true)]
public interface IPublicProfileService
{
    /// <summary>
    /// 当前加载的档案文件名
    /// </summary>
    string CurrentProfilePath { get; set; }

    /// <summary>
    /// 当前加载的档案
    /// </summary>
    Profile Profile { get; set; }

    /// <summary>
    /// 保存档案到当前的档案。
    /// </summary>
    void SaveProfile();

    /// <summary>
    /// 将档案保存到指定的文件。
    /// </summary>
    /// <param name="filename">文件名</param>
    void SaveProfile(string filename);

    /// <summary>
    /// 创建临时层课表。如果已经存在临时层课表，则本方法不起作用。
    /// </summary>
    /// <param name="id">源课表ID</param>
    /// <param name="timeLayoutId">要使用的时间表ID,留null将使用源课表的时间表</param>
    /// <param name="enableDateTime">临时层的预定启用时间</param>
    /// <returns>如果创建成功，则返回临时层课表的ID，否则返回null。</returns>
    string? CreateTempClassPlan(string id, string? timeLayoutId = null, DateTime? enableDateTime = null);

    /// <summary>
    /// 清空临时层课表
    /// </summary>
    void ClearTempClassPlan();

    /// <summary>
    /// 将当前临时层课表转换为普通课表。
    /// </summary>
    void ConvertToStdClassPlan();

    /// <summary>
    /// 将指定临时层课表转换为普通课表。
    /// </summary>
    /// <param name="id">要转换的课表 GUID</param>
    void ConvertToStdClassPlan(string id);

    /// <summary>
    /// 设置临时课表组。
    /// </summary>
    /// <param name="key">要设置的临时课表组ID</param>
    /// <param name="expireTime">临时课表组过期时间，默认为完成一个周期后。</param>
    void SetupTempClassPlanGroup(string key, DateTime? expireTime = null);

    /// <summary>
    /// 清除当前的临时课表组。
    /// </summary>
    void ClearTempClassPlanGroup();

    /// <summary>
    /// 当前档案是否为信任档案。
    /// </summary>
    bool IsCurrentProfileTrusted { get; }
}