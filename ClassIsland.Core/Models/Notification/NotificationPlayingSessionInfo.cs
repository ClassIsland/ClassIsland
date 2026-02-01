using System.Diagnostics;

namespace ClassIsland.Core.Models.Notification;

/// <summary>
/// 代表提醒播放会话信息
/// </summary>
public class NotificationPlayingSessionInfo
{
    /// <summary>
    /// 此会话已播放的时间
    /// </summary>
    public TimeSpan SessionPlayedTime { get; internal set; } = TimeSpan.Zero;

    /// <summary>
    /// 此会话的开始时间
    /// </summary>
    public DateTime SessionStartTime { get; internal set; } = DateTime.MinValue;

    /// <summary>
    /// 当前票据的播放的开始时间
    /// </summary>
    public DateTime CurrentTicketStartTime { get; internal set; } = DateTime.MinValue;

    /// <summary>
    /// 计时用秒表
    /// </summary>
    internal Stopwatch TimingStopwatch { get; } = new();

    /// <summary>
    /// 各种音效是否已经播放
    /// </summary>
    public bool HasSoundsPlayed { get; internal set; } = false;

    /// <summary>
    /// 提醒是否显式指定了结束时间
    /// </summary>
    public bool IsExplicitEndTime { get; internal set; } = false;

    /// <summary>
    /// 此部分的会话是否已经完成
    /// </summary>
    public bool IsCompleted { get; internal set; } = false;

}