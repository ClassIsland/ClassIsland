using System.Collections.ObjectModel;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Components;

namespace ClassIsland.Core.Services.Registry;

/// <summary>
/// 组件注册服务
/// </summary>
public class ComponentRegistryService
{
    /// <summary>
    /// 已注册的组件
    /// </summary>
    public static ObservableCollection<ComponentInfo> Registered { get; } = new();

    public static ObservableCollection<ComponentSettings> RegisteredSettings { get; } = new();
}