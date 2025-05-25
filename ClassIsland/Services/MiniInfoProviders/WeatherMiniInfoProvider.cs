using System;
using System.Threading;
using System.Threading.Tasks;

using ClassIsland.Controls.MiniInfoProvider;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Models;

using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services.MiniInfoProviders;

public class WeatherMiniInfoProvider : IMiniInfoProvider, IHostedService
{
    public string Name { get; set; } = "天气简报";
    public string Description { get; set; } = "显示当前的天气信息。";
    public Guid ProviderGuid { get; set; } = new Guid("EA336289-5A60-49EF-AD36-858109F37644");
    public object? SettingsElement { get; set; }
    public object InfoElement { get; set; }

    private SettingsService SettingsService { get; }

    private Settings Settings => SettingsService.Settings;

    private WeatherMiniInfoProviderSettings WeatherMiniInfoProviderSettings { get; set; }

    public WeatherMiniInfoProvider(SettingsService settingsService, MiniInfoProviderHostService miniInfoProviderHostService)
    {
        SettingsService = settingsService;
        miniInfoProviderHostService.RegisterMiniInfoProvider(this);
        WeatherMiniInfoProviderSettings =
            miniInfoProviderHostService.GetMiniInfoProviderSettings<WeatherMiniInfoProviderSettings>(ProviderGuid)
            ?? new();
        InfoElement = new WeatherMiniInfoProviderControl(SettingsService, WeatherMiniInfoProviderSettings);
        SettingsElement = new WeatherMiniInfoProviderSettingsControl(WeatherMiniInfoProviderSettings);
        miniInfoProviderHostService.WriteMiniInfoProviderSettings(ProviderGuid, WeatherMiniInfoProviderSettings);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}