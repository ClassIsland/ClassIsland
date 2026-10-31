namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 平台触觉反馈服务。
/// </summary>
public interface IHapticFeedbackService
{
    /// <summary>
    /// 播放一次刻度轻震，不支持时忽略。
    /// </summary>
    void PlayTick();
}
