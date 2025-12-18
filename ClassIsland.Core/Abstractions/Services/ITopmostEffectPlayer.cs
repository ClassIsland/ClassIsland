using ClassIsland.Shared.Interfaces.Controls;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 顶层效果播放方
/// </summary>
public interface ITopmostEffectPlayer
{
    /// <summary>
    /// 播放提醒特效。
    /// </summary>
    /// <param name="effect">要播放的特效控件</param>
    public void PlayEffect(INotificationEffectControl effect);
}