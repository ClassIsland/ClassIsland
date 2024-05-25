using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using ClassIsland.Core;
using ClassIsland.Core.Interfaces;
using ClassIsland.Models;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class MiniInfoProviderHostService : IHostedService
{
    private SettingsService SettingsService { get; }
    private ILogger<MiniInfoProviderHostService> Logger { get; }

    private Settings Settings => SettingsService.Settings;
    public ObservableDictionary<string, IMiniInfoProvider> Providers { get; } = new();

    public MiniInfoProviderHostService(SettingsService settingsService, ILogger<MiniInfoProviderHostService> logger)
    {
        Logger = logger;
        SettingsService = settingsService;
    }

    public void RegisterMiniInfoProvider(IMiniInfoProvider provider)
    {
        var guid = provider.ProviderGuid.ToString();
        Logger.LogInformation("注册快速信息提供方：{}（{}）", guid, provider.Name);
        if (!Settings.MiniInfoProviderSettings.Keys.Contains(guid))
        {
            Settings.MiniInfoProviderSettings[guid] = null;
        }
        Providers.Add(guid, provider);
    }

    public T? GetMiniInfoProviderSettings<T>(Guid id)
    {
        Logger.LogInformation("获取快速信息提供方设置：{}", id);
        var o = Settings.MiniInfoProviderSettings[id.ToString()];
        if (o is JsonElement)
        {
            var o1 = (JsonElement)o;
            return o1.Deserialize<T>();
        }
        return (T?)Settings.MiniInfoProviderSettings[id.ToString()];
    }

    public void WriteMiniInfoProviderSettings<T>(Guid id, T settings)
    {
        Logger.LogInformation("写入快速信息提供方设置：{}", id);
        Settings.MiniInfoProviderSettings[id.ToString()] = settings;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}