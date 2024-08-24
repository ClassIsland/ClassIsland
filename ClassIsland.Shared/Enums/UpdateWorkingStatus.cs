namespace ClassIsland.Shared.Enums;

/// <summary>
/// 更新工作状态
/// </summary>
public enum UpdateWorkingStatus
{
    /// <summary>
    /// 空闲
    /// </summary>
    Idle,
    /// <summary>
    /// 正在检查更新
    /// </summary>
    CheckingUpdates,
    /// <summary>
    /// 正在下载更新
    /// </summary>
    DownloadingUpdates,
    /// <summary>
    /// 网络错误
    /// </summary>
    [Obsolete]
    NetworkError,
    /// <summary>
    /// 正在解包更新
    /// </summary>
    ExtractingUpdates
}