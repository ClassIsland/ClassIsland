using System.ComponentModel;

namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 平台级悬浮主界面服务。iOS TrollStore 发行版使用系统级常驻悬浮窗，iOS 侧载发行版使用画中画窗口，
/// 其它平台使用桩实现并返回 <see cref="IsSupported"/> = <see langword="false"/>。
/// </summary>
public interface IMainWindowOverlayService : INotifyPropertyChanged
{
    /// <summary>
    /// 当前平台与发行版是否支持悬浮主界面。
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// 是否已启用悬浮主界面。设置该属性会同步启停平台宿主。
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// 悬浮主界面是否由独立进程渲染。为 <see langword="true"/> 时，提醒预览需要转发给该进程显示，
    /// 而不是在当前进程中显示。
    /// </summary>
    bool IsRenderedOutOfProcess => false;
}
