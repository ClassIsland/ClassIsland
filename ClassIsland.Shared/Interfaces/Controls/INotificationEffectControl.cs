namespace ClassIsland.Shared.Interfaces.Controls;

/// <summary>
/// 提醒特效接口
/// </summary>
public interface INotificationEffectControl
{
    /// <summary>
    /// 播放提醒特效
    /// </summary>
    public void Play();

    /// <summary>
    /// 提醒特效播放结束事件
    /// </summary>
    public event EventHandler? EffectCompleted;
}