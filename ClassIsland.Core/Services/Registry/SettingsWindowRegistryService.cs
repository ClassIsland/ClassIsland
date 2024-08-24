using System.Collections.ObjectModel;
using ClassIsland.Core.Attributes;

namespace ClassIsland.Core.Services.Registry;

/// <summary>
/// 设置界面注册服务。
/// </summary>
public class SettingsWindowRegistryService
{
    /// <summary>
    /// 已注册的设置页面信息
    /// </summary>
    public static ObservableCollection<SettingsPageInfo> Registered { get; } = new();
}