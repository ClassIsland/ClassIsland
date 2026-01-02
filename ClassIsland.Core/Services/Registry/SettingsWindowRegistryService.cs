using System.Collections.ObjectModel;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.SettingsWindow;

namespace ClassIsland.Core.Services.Registry;

/// <summary>
/// 设置界面注册服务。
/// </summary>
public static class SettingsWindowRegistryService
{
    /// <summary>
    /// 已注册的设置页面信息
    /// </summary>
    public static ObservableCollection<SettingsPageInfo> Registered { get; } = [];

    /// <summary>
    /// 已注册的设置页面分组信息
    /// </summary>
    public static Dictionary<string, SettingsPageGroupInfo> Groups { get; } = [];
}