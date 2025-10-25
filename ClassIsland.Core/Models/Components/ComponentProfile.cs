using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Components;

/// <summary>
/// 代表组件配置方案
/// </summary>
public partial class ComponentProfile : ObservableObject
{
    /// <summary>
    /// 组件包含的主界面行
    /// </summary>
    [ObservableProperty] private ObservableCollection<MainWindowLineSettings> _lines = [];
}