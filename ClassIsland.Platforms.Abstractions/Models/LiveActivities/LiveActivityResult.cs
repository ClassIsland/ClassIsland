namespace ClassIsland.Platforms.Abstraction.Models.LiveActivities;

/// <summary>
/// 实时活动操作的结果代码。
/// </summary>
public enum LiveActivityResultCode
{
    /// <summary>操作成功。</summary>
    Succeeded = 0,

    /// <summary>当前平台或系统版本不支持实时活动。</summary>
    Unsupported = 1,

    /// <summary>用户已在系统设置中关闭实时活动。</summary>
    Disabled = 2,

    /// <summary>传入内容不符合 ActivityKit 约束。</summary>
    InvalidContent = 3,

    /// <summary>原生 ActivityKit 操作失败。</summary>
    NativeFailure = 4,

    /// <summary>操作被调用方取消。</summary>
    Cancelled = 5
}

/// <summary>
/// 实时活动操作结果。
/// </summary>
/// <param name="Code">操作结果代码。</param>
/// <param name="ActivityId">ActivityKit 返回的活动标识。</param>
/// <param name="ErrorMessage">可安全显示或记录的错误说明。</param>
public sealed record LiveActivityResult(
    LiveActivityResultCode Code,
    string? ActivityId = null,
    string? ErrorMessage = null)
{
    /// <summary>
    /// 操作是否成功。
    /// </summary>
    public bool IsSuccess => Code == LiveActivityResultCode.Succeeded;
}
