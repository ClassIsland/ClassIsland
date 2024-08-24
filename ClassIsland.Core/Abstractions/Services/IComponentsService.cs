using System.Collections.ObjectModel;
using System.ComponentModel;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Components;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 组件服务。
/// </summary>
public interface IComponentsService : INotifyPropertyChanged
{

    /// <summary>
    /// 当前显示的所有组件
    /// </summary>
    public ObservableCollection<ComponentSettings> CurrentComponents { get; set; }

    /// <summary>
    /// 获取组件实例
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="isSettings"></param>
    /// <returns></returns>
    public ComponentBase? GetComponent(ComponentSettings settings, bool isSettings);

    /// <summary>
    /// 保存组件配置
    /// </summary>
    public void SaveConfig();

    /// <summary>
    /// 可用的组件配置文件
    /// </summary>
    public IReadOnlyList<string> ComponentConfigs { get; set; }

    /// <summary>
    /// 重新加载组件配置文件列表。
    /// </summary>
    public void RefreshConfigs();
}