using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;

namespace ClassIsland.Core.Models.Notification;

/// <summary>
/// 播放时的 <see cref="NotificationContent"/> 不可变快照
/// 计时将基于此快照
/// </summary>
public record NotificationContentSnapshot
{
    /// <summary>
    /// 计算后的显示时长
    /// 当 <see cref="EndTime"/> 存在时使用当前时间计算
    /// </summary>
    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 显示的结束时间
    /// </summary>
    public DateTime? EndTime { get; init; }

    /// <summary>
    /// 语音内容
    /// </summary>
    public string SpeechContent { get; init; } = "";

    /// <summary>
    /// 是否启用语音
    /// </summary>
    public bool IsSpeechEnabled { get; init; } = true;

    /// <summary>
    /// 涟漪特效颜色
    /// </summary>
    public IBrush? Color { get; init; }

    /// <summary>
    /// 内容模板
    /// </summary>
    public DataTemplate? ContentTemplate { get; init; }

    /// <summary>
    /// 从 <see cref="NotificationContent"/> 建不可变快照
    /// 如果指定了 <see cref="NotificationContent.EndTime"/>, 将使用 <paramref name="now"/> 计算持续时间
    /// </summary>
    /// <param name="content">原始内容</param>
    /// <param name="now">当前时间</param>
    /// <returns>不可变快照</returns>
    public static NotificationContentSnapshot From(NotificationContent content, DateTime now)
    {
        var duration = content.Duration;
        if (content.EndTime != null)
        {
            var raw = content.EndTime.Value - now;
            duration = raw > TimeSpan.Zero ? raw : TimeSpan.Zero;
        }
        return new NotificationContentSnapshot
        {
            Duration = duration,
            EndTime = content.EndTime,
            SpeechContent = content.SpeechContent,
            IsSpeechEnabled = content.IsSpeechEnabled,
            Color = content.Color,
            ContentTemplate = content.ContentTemplate,
        };
    }
}
