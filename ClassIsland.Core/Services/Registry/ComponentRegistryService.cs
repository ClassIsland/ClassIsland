using System.Collections.ObjectModel;
using System.Reflection.Metadata;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Components;
using ClassIsland.Core.Models.Plugin;

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

    public static Dictionary<Guid, Guid> MigrationPairs { get; } = new();

    /// <summary>
    /// 当前正在注册组件的插件上下文。在插件 Initialize 期间由 PluginService 设置，
    /// 供 ComponentRegistryExtensions.Register 读取以标记组件来源。
    /// 内置组件注册时此值为 null。
    /// </summary>
    public static AsyncLocal<PluginInfo?> CurrentRegisteringPlugin { get; } = new();

    /// <summary>
    /// 获取按来源分组的组件列表，内置组在最前
    /// 已加载但未注册任何组件的插件不会产生空分组。
    /// </summary>
    public static IReadOnlyList<ComponentGroup> GetGroupedSortedComponents()
    {
        var groups = Registered
            .GroupBy(c => c.SourcePlugin?.Manifest.Id ?? Guid.Empty.ToString())
            .Select(g =>
            {
                var first = g.First();
                return new ComponentGroup(
                    GroupName: first.SourceName,
                    Plugin: first.SourcePlugin,
                    Components: g.ToList());
            })
            .ToList();

        // 内置组排最前，其余按 GroupName 升序（即插件名升序）；同为插件组时按 Plugin Id 二次排序
        var ordered = groups
            .OrderBy(g => g.Plugin is null ? 0 : 1)
            .ThenBy(g => g.GroupName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(g => g.Plugin?.Manifest.Id ?? "")
            .ToList();
        return ordered;
    }
}