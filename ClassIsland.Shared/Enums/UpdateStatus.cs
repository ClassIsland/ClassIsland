namespace ClassIsland.Shared.Enums;

/// <summary>
/// 当前更新状态
/// </summary>
public enum UpdateStatus
{
    /// <summary>
    /// 已是最新
    /// </summary>
    UpToDate,
    /// <summary>
    /// 更新可用
    /// </summary>
    UpdateAvailable,
    /// <summary>
    /// 已下载更新
    /// </summary>
    UpdateDownloaded
}