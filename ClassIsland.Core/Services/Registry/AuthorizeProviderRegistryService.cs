using System.Collections.ObjectModel;
using ClassIsland.Core.Attributes;

namespace ClassIsland.Core.Services.Registry;

/// <summary>
/// 认证提供方注册服务
/// </summary>
public static class AuthorizeProviderRegistryService
{
    /// <summary>
    /// 已注册的认证提供方
    /// </summary>
    public static ObservableCollection<AuthorizeProviderInfo> RegisteredAuthorizeProviders { get; } = [];
}