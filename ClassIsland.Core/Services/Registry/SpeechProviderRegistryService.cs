using System.Collections.ObjectModel;
using ClassIsland.Core.Attributes;

namespace ClassIsland.Core.Services.Registry;

/// <summary>
/// 语音提供方注册服务
/// </summary>
public static class SpeechProviderRegistryService
{
    /// <summary>
    /// 已注册的语音提供方
    /// </summary>
    public static ObservableCollection<SpeechProviderInfo> RegisteredProviders { get; } = [];
}