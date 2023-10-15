using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Interfaces;
using ClassIsland.Models;
using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services;

public class MiniInfoProviderHostService : IHostedService
{
    private SettingsService SettingsService { get; }

    private Settings Settings => SettingsService.Settings;
    public ObservableDictionary<string, IMiniInfoProvider> Providers { get; } = new();

    public MiniInfoProviderHostService(SettingsService settingsService)
    {
        SettingsService = settingsService;
    }

    public void RegisterMiniInfoProvider(IMiniInfoProvider provider)
    {
        var guid = provider.ProviderGuid.ToString();
        if (!Settings.MiniInfoProviderSettings.Keys.Contains(guid))
        {
            Settings.MiniInfoProviderSettings[guid] = null;
        }
        Providers.Add(guid, provider);
    }

    public T? GetMiniInfoProviderSettings<T>(Guid id)
    {
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
        Settings.MiniInfoProviderSettings[id.ToString()] = settings;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}