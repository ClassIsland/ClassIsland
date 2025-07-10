using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Components;

/// <summary>
/// 代表主界面行的配置
/// </summary>
public partial class MainWindowLineSettings : ObservableObject
{
    /// <summary>
    /// 主界面行包含的组件列表
    /// </summary>
    [ObservableProperty] private ObservableCollection<ComponentSettings> _children = [];

    /// <summary>
    /// 是否为主要行
    /// </summary>
    [ObservableProperty] private bool _isMainLine = false;

    /// <summary>
    /// 是否启用提醒
    /// </summary>
    [ObservableProperty] private bool _isNotificationEnabled = true;
}