using System.ComponentModel;

namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 平台级悬浮主界面服务。当前仅 TrollStore 发行版的 iOS 端实现系统级常驻悬浮窗，
/// 其它平台使用桩实现并返回 <see cref="IsSupported"/> = <see langword="false"/>。
/// </summary>
public interface IMainWindowOverlayService : INotifyPropertyChanged
{
    /// <summary>
    /// 当前平台与发行版是否支持悬浮主界面。
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// 是否已启用悬浮主界面。设置该属性会同步启停平台宿主进程。
    /// </summary>
    bool IsEnabled { get; set; }
}
