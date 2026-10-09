using ClassIsland.Platforms.Abstraction.Models.LiveActivities;

namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 使用纯 C# 发布、更新和结束系统实时活动。
/// </summary>
public interface ILiveActivityService
{
    /// <summary>
    /// 获取当前设备的实时活动可用状态。
    /// </summary>
    LiveActivityAvailability Availability { get; }

    /// <summary>
    /// 发布课程实时活动；已有的 ClassIsland Activity 会平滑更新。
    /// <see cref="LessonLiveActivityContent.IntervalId"/> 仅用于业务标识和内容去重。
    /// </summary>
    /// <param name="content">要展示的课程内容。</param>
    /// <param name="cancellationToken">取消等待原生操作完成。</param>
    Task<LiveActivityResult> PublishAsync(
        LessonLiveActivityContent content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 结束当前课程实时活动。
    /// </summary>
    /// <param name="dismissalPolicy">结束后的系统移除策略。</param>
    /// <param name="cancellationToken">取消等待原生操作完成。</param>
    Task<LiveActivityResult> EndAsync(
        LiveActivityDismissalPolicy dismissalPolicy = LiveActivityDismissalPolicy.Default,
        CancellationToken cancellationToken = default);
}
