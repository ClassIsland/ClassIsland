using System.Collections.ObjectModel;
using ClassIsland.Core.Attributes;

namespace ClassIsland.Core.Services.Registry;

/// <summary>
/// 提醒提供方 V2 注册服务。
/// </summary>
public static class NotificationProviderRegistryService
{
    /// <summary>
    /// 已注册的提醒提供方列表。
    /// </summary>
    public static ObservableCollection<NotificationProviderInfo> RegisteredProviders { get; } = [];
}